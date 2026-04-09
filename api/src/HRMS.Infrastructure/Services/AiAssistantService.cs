using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRMS.Infrastructure.Services;

public sealed class AiAssistantService : IAiAssistantService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly PortalRoute[] PortalRoutes =
    [
        new("dashboard", "/dashboard", "Dashboard", "See overall metrics, summaries, recent payroll, and leave activity.", []),
        new("employees", "/employees", "Employees", "Browse employee records, add people, and open employee detail profiles.", ["Admin", "HR"], "employee", "directory", "profile", "staff"),
        new("departments", "/departments", "Departments", "Create and manage department records and organization structure.", ["Admin", "HR"], "department", "team", "org"),
        new("workforce", "/workforce", "Workforce", "Manage shift planning, rosters, and workforce scheduling operations.", ["Admin", "HR"], "workforce", "schedule", "shift", "planning"),
        new("attendance", "/attendance", "Attendance", "Handle check-in, check-out, geo-tagged proof, and attendance logs.", [], "attendance", "check in", "check-in", "check out", "check-out", "present"),
        new("my-roster", "/my-roster", "My Roster", "Review the logged-in employee's assigned shifts and upcoming workdays.", ["Employee"], "roster", "my shift", "my schedule"),
        new("leaves", "/leaves", "Leaves", "Apply for leave or review and approve pending requests.", [], "leave", "vacation", "time off", "approve leave"),
        new("payroll", "/payroll", "Payroll", "Review salary structures, generate payroll, and inspect payslip history.", [], "payroll", "salary", "payslip", "compensation"),
        new("documents", "/documents", "Documents", "Open the document vault for uploaded files and generated records.", [], "document", "vault", "file", "contract", "payslip file"),
        new("notifications", "/notifications", "Notifications", "Track alerts tied to leave, payroll, onboarding, recruitment, and general workflows.", [], "notification", "alert", "updates"),
        new("talent", "/talent", "Talent", "Review recruiting and talent pipeline features such as candidates and appraisals.", [], "talent", "recruitment", "candidate", "hiring", "appraisal")
    ];

    private static readonly string PortalKnowledge = """
HRMS is a human resource management portal with these major capabilities:
- Authentication with Admin, HR, and Employee roles.
- Dashboard summaries for headcount, departments, payroll, attendance, and recent activity.
- Employee management with employee records, department assignment, profile images, and detail pages.
- Department management for creating and organizing departments.
- Attendance workflows with check-in, check-out, geotagged photo proof, logs, and attendance status tracking.
- Workforce scheduling with shifts, roster planning, and employee roster visibility.
- Leave workflows for applying, approving, or rejecting leave requests.
- Payroll workflows for salary structures, payroll generation, and payroll history.
- Document vault support for employee and payroll documents.
- Notifications for leave, payroll, onboarding, recruitment, and general updates.
- Talent features covering candidates, hiring pipeline, and performance appraisals.
""";

    private readonly HttpClient _httpClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AiAssistantService> _logger;
    private readonly AiAssistantOptions _options;

    public AiAssistantService(
        HttpClient httpClient,
        ICurrentUserService currentUserService,
        IOptions<AiAssistantOptions> options,
        ILogger<AiAssistantService> logger)
    {
        _httpClient = httpClient;
        _currentUserService = currentUserService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<AiChatResponseDto> GetResponseAsync(AiChatRequestDto request, CancellationToken cancellationToken)
    {
        var availableRoutes = GetAvailableRoutes();
        var latestUserMessage = request.Messages.LastOrDefault(message =>
            string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            return BuildUnavailableResponse(
                "The AI assistant is almost ready, but the NVIDIA model id is missing in the API configuration.",
                availableRoutes,
                latestUserMessage);
        }

        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BuildUnavailableResponse(
                "The AI assistant is not configured with an NVIDIA API key yet. Add it on the server to enable live chat.",
                availableRoutes,
                latestUserMessage);
        }

        var payload = new
        {
            model = _options.Model,
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens,
            messages = BuildPromptMessages(request, availableRoutes)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AI assistant request failed with status {StatusCode}: {ResponseBody}", response.StatusCode, rawResponse);
            return BuildUnavailableResponse(
                BuildProviderErrorMessage(response.StatusCode, rawResponse),
                availableRoutes,
                latestUserMessage);
        }

        var completion = JsonSerializer.Deserialize<NvidiaChatCompletionResponse>(rawResponse, JsonOptions);
        var assistantOutput = ExtractAssistantContent(completion);
        var parsedPayload = ParseAssistantPayload(assistantOutput, availableRoutes, latestUserMessage);

        return new AiChatResponseDto
        {
            Message = parsedPayload.Message,
            Actions = parsedPayload.Actions,
            AutoNavigatePath = parsedPayload.AutoNavigatePath
        };
    }

    private string? ResolveApiKey() =>
        Environment.GetEnvironmentVariable("NVIDIA_API_KEY") ??
        _options.ApiKey;

    private IReadOnlyCollection<object> BuildPromptMessages(AiChatRequestDto request, IReadOnlyCollection<PortalRoute> availableRoutes)
    {
        var promptMessages = new List<object>
        {
            new
            {
                role = "system",
                content = BuildSystemPrompt(request.CurrentPath, availableRoutes)
            }
        };

        foreach (var message in request.Messages
                     .Where(message => !string.IsNullOrWhiteSpace(message.Content))
                     .TakeLast(8))
        {
            promptMessages.Add(new
            {
                role = NormalizeRole(message.Role),
                content = message.Content.Trim()
            });
        }

        return promptMessages;
    }

    private string BuildSystemPrompt(string? currentPath, IReadOnlyCollection<PortalRoute> availableRoutes)
    {
        var roles = _currentUserService.Roles.Count > 0
            ? string.Join(", ", _currentUserService.Roles)
            : "Authenticated User";
        var routeSummary = string.Join(Environment.NewLine, availableRoutes.Select(route =>
            $"- id: {route.Id}; label: {route.Label}; path: {route.Path}; description: {route.Description}"));

        return $$"""
You are HRMS Compass, the in-portal assistant for this HRMS application.
Stay grounded in the product context below and help the user understand features, workflows, and navigation.
Do not invent pages or permissions that are not listed.
Prefer concise answers. Keep the assistant message under 140 words when possible.
Only recommend routes from the available route list.
Set autoNavigateRouteId only when the user explicitly asks to go, open, navigate, redirect, or take them to a page now.
Respond with strict JSON only using this exact shape:
{"message":"string","actionRouteIds":["route-id"],"autoNavigateRouteId":"route-id-or-null"}

Portal context:
{{PortalKnowledge}}

Current user:
- Email: {{_currentUserService.Email ?? "Unknown"}}
- Roles: {{roles}}
- Current path: {{currentPath ?? "/dashboard"}}

Available routes:
{{routeSummary}}
""";
    }

    private static string NormalizeRole(string role) =>
        string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";

    private IReadOnlyCollection<PortalRoute> GetAvailableRoutes() =>
        PortalRoutes
            .Where(route => route.AllowedRoles.Count == 0 || route.AllowedRoles.Intersect(_currentUserService.Roles, StringComparer.OrdinalIgnoreCase).Any())
            .ToArray();

    private AiChatResponseDto BuildUnavailableResponse(
        string message,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage)
    {
        var fallback = BuildFallbackPayload(message, availableRoutes, latestUserMessage);
        return new AiChatResponseDto
        {
            Message = fallback.Message,
            Actions = fallback.Actions,
            AutoNavigatePath = fallback.AutoNavigatePath
        };
    }

    private static string BuildProviderErrorMessage(System.Net.HttpStatusCode statusCode, string rawResponse)
    {
        if (statusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return "The NVIDIA API key was rejected by the provider. Please verify the key and restart the API.";
        }

        if (statusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return $"NVIDIA rejected the assistant request. {ExtractProviderMessage(rawResponse)}";
        }

        if (statusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return "The NVIDIA free tier rate limit has been reached. Please wait a bit and try again.";
        }

        return $"I could not reach the NVIDIA assistant service right now. {ExtractProviderMessage(rawResponse)}";
    }

    private static string ExtractProviderMessage(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return "Please try again in a moment.";
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    return errorElement.GetString() ?? "Please try again in a moment.";
                }

                if (errorElement.ValueKind == JsonValueKind.Object &&
                    errorElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString() ?? "Please try again in a moment.";
                }
            }

            if (document.RootElement.TryGetProperty("message", out var rootMessage) &&
                rootMessage.ValueKind == JsonValueKind.String)
            {
                return rootMessage.GetString() ?? "Please try again in a moment.";
            }
        }
        catch (JsonException)
        {
        }

        return rawResponse.Length <= 180 ? rawResponse : $"{rawResponse[..180].TrimEnd()}...";
    }

    private AssistantResponsePayload ParseAssistantPayload(
        string rawContent,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage)
    {
        var json = ExtractJson(rawContent);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<ModelPayload>(json, JsonOptions);
                if (payload is not null && !string.IsNullOrWhiteSpace(payload.Message))
                {
                    return BuildResponsePayload(payload.Message, payload.ActionRouteIds, payload.AutoNavigateRouteId, availableRoutes, latestUserMessage);
                }
            }
            catch (JsonException exception)
            {
                _logger.LogDebug(exception, "Unable to parse assistant JSON payload. Falling back to heuristic response.");
            }
        }

        return BuildFallbackPayload(rawContent, availableRoutes, latestUserMessage);
    }

    private AssistantResponsePayload BuildFallbackPayload(
        string rawContent,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage)
    {
        var matchedRoutes = MatchRoutes(latestUserMessage, availableRoutes);
        var autoRoute = ShouldAutoNavigate(latestUserMessage) ? matchedRoutes.FirstOrDefault()?.Id : null;
        var message = string.IsNullOrWhiteSpace(rawContent)
            ? "I can help explain HRMS features and point you to the right module."
            : SanitizeMessage(rawContent);

        return BuildResponsePayload(message, matchedRoutes.Select(route => route.Id).ToArray(), autoRoute, availableRoutes, latestUserMessage);
    }

    private AssistantResponsePayload BuildResponsePayload(
        string message,
        IReadOnlyCollection<string>? actionRouteIds,
        string? autoNavigateRouteId,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage)
    {
        var routeLookup = availableRoutes.ToDictionary(route => route.Id, StringComparer.OrdinalIgnoreCase);
        var actions = new List<AiAssistantActionDto>();

        foreach (var routeId in actionRouteIds ?? Array.Empty<string>())
        {
            if (!routeLookup.TryGetValue(routeId, out var route))
            {
                continue;
            }

            if (actions.Any(existing => string.Equals(existing.Path, route.Path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            actions.Add(new AiAssistantActionDto
            {
                Label = route.Label,
                Path = route.Path,
                Description = route.Description,
                AutoNavigate = string.Equals(route.Id, autoNavigateRouteId, StringComparison.OrdinalIgnoreCase)
            });
        }

        if (actions.Count == 0)
        {
            foreach (var route in MatchRoutes(latestUserMessage, availableRoutes).Take(3))
            {
                actions.Add(new AiAssistantActionDto
                {
                    Label = route.Label,
                    Path = route.Path,
                    Description = route.Description,
                    AutoNavigate = string.Equals(route.Id, autoNavigateRouteId, StringComparison.OrdinalIgnoreCase)
                });
            }
        }

        var autoNavigatePath = routeLookup.TryGetValue(autoNavigateRouteId ?? string.Empty, out var autoRoute)
            ? autoRoute.Path
            : null;

        return new AssistantResponsePayload
        {
            Message = SanitizeMessage(message),
            Actions = actions,
            AutoNavigatePath = autoNavigatePath
        };
    }

    private static IReadOnlyCollection<PortalRoute> MatchRoutes(string userMessage, IReadOnlyCollection<PortalRoute> availableRoutes)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return availableRoutes.Take(3).ToArray();
        }

        return availableRoutes
            .Select(route => new
            {
                Route = route,
                Score = GetRouteScore(userMessage, route)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Route.Label)
            .Select(item => item.Route)
            .Take(3)
            .ToArray();
    }

    private static int GetRouteScore(string userMessage, PortalRoute route)
    {
        var score = 0;
        var normalized = userMessage.Trim().ToLowerInvariant();

        if (normalized.Contains(route.Label.ToLowerInvariant()))
        {
            score += 4;
        }

        if (normalized.Contains(route.Path.Trim('/').ToLowerInvariant()))
        {
            score += 3;
        }

        foreach (var keyword in route.Keywords)
        {
            if (normalized.Contains(keyword.ToLowerInvariant()))
            {
                score += 2;
            }
        }

        return score;
    }

    private static bool ShouldAutoNavigate(string userMessage) =>
        Regex.IsMatch(userMessage, @"\b(go|open|navigate|redirect|take me|show me)\b", RegexOptions.IgnoreCase);

    private static string ExtractAssistantContent(NvidiaChatCompletionResponse? completion)
    {
        var content = completion?.Choices.FirstOrDefault()?.Message?.Content;
        if (content is null || content.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return string.Empty;
        }

        if (content.Value.ValueKind == JsonValueKind.String)
        {
            return content.Value.GetString() ?? string.Empty;
        }

        if (content.Value.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in content.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    parts.Add(item.GetString() ?? string.Empty);
                    continue;
                }

                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    parts.Add(textElement.GetString() ?? string.Empty);
                }
            }

            return string.Join(" ", parts).Trim();
        }

        return content.Value.ToString();
    }

    private static string ExtractJson(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return string.Empty;
        }

        var trimmed = rawContent.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');

        return start >= 0 && end > start
            ? trimmed[start..(end + 1)]
            : string.Empty;
    }

    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "I can help with HRMS features and navigation.";
        }

        var cleaned = message
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleaned.Length <= 420 ? cleaned : cleaned[..420].TrimEnd();
    }

    private sealed record PortalRoute(
        string Id,
        string Path,
        string Label,
        string Description,
        IReadOnlyCollection<string> AllowedRoles,
        params string[] RouteKeywords)
    {
        public IReadOnlyCollection<string> Keywords { get; } = RouteKeywords;
    }

    private sealed class ModelPayload
    {
        public string Message { get; init; } = string.Empty;
        public IReadOnlyCollection<string>? ActionRouteIds { get; init; }
        public string? AutoNavigateRouteId { get; init; }
    }

    private sealed class AssistantResponsePayload
    {
        public string Message { get; init; } = string.Empty;
        public IReadOnlyCollection<AiAssistantActionDto> Actions { get; init; } = Array.Empty<AiAssistantActionDto>();
        public string? AutoNavigatePath { get; init; }
    }

    private sealed class NvidiaChatCompletionResponse
    {
        public IReadOnlyCollection<NvidiaChoice> Choices { get; init; } = Array.Empty<NvidiaChoice>();
    }

    private sealed class NvidiaChoice
    {
        public NvidiaMessage? Message { get; init; }
    }

    private sealed class NvidiaMessage
    {
        public JsonElement? Content { get; init; }
    }
}
