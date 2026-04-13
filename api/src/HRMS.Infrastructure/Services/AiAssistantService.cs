using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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
        new("leaves", "/leaves", "Leaves", "Apply for leave or review pending requests.", [], "leave", "vacation", "time off", "approve leave"),
        new("payroll", "/payroll", "Payroll", "Review salary structures, generate payroll, and inspect payslip history.", [], "payroll", "salary", "payslip", "compensation"),
        new("documents", "/documents", "Documents", "Open the document vault for uploaded files and generated records.", [], "document", "vault", "file", "contract"),
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
        var latestUserMessage = GetLatestUserMessage(request);
        var configurationIssue = GetConfigurationIssue(availableRoutes, latestUserMessage, request.CurrentPath);
        if (configurationIssue is not null)
        {
            return configurationIssue;
        }

        using var httpRequest = BuildProviderRequest(request, availableRoutes, stream: false);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AI assistant request failed with status {StatusCode}: {ResponseBody}", response.StatusCode, rawResponse);
            return BuildUnavailableResponse(
                BuildProviderErrorMessage(response.StatusCode, rawResponse),
                availableRoutes,
                latestUserMessage,
                request.CurrentPath);
        }

        var completion = JsonSerializer.Deserialize<NvidiaChatCompletionResponse>(rawResponse, JsonOptions);
        var assistantMessage = SanitizeMessage(ExtractAssistantContent(completion));

        return BuildResponsePayload(assistantMessage, availableRoutes, latestUserMessage, request.CurrentPath);
    }

    public async IAsyncEnumerable<AiChatStreamEventDto> StreamResponseAsync(
        AiChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var availableRoutes = GetAvailableRoutes();
        var latestUserMessage = GetLatestUserMessage(request);
        var configurationIssue = GetConfigurationIssue(availableRoutes, latestUserMessage, request.CurrentPath);
        if (configurationIssue is not null)
        {
            yield return new AiChatStreamEventDto
            {
                Type = "complete",
                Message = configurationIssue.Message,
                Actions = configurationIssue.Actions,
                AutoNavigatePath = configurationIssue.AutoNavigatePath
            };
            yield break;
        }

        using var httpRequest = BuildProviderRequest(request, availableRoutes, stream: true);
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI assistant streaming request failed with status {StatusCode}: {ResponseBody}", response.StatusCode, rawError);
            var fallback = BuildUnavailableResponse(
                BuildProviderErrorMessage(response.StatusCode, rawError),
                availableRoutes,
                latestUserMessage,
                request.CurrentPath);

            yield return new AiChatStreamEventDto
            {
                Type = "complete",
                Message = fallback.Message,
                Actions = fallback.Actions,
                AutoNavigatePath = fallback.AutoNavigatePath
            };
            yield break;
        }

        var responseBuffer = new StringBuilder();
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var delta = ExtractAssistantDelta(payload);
            if (string.IsNullOrEmpty(delta))
            {
                continue;
            }

            responseBuffer.Append(delta);
            yield return new AiChatStreamEventDto
            {
                Type = "delta",
                Delta = delta
            };
        }

        var finalResponse = BuildResponsePayload(
            SanitizeMessage(responseBuffer.ToString()),
            availableRoutes,
            latestUserMessage,
            request.CurrentPath);

        yield return new AiChatStreamEventDto
        {
            Type = "complete",
            Message = finalResponse.Message,
            Actions = finalResponse.Actions,
            AutoNavigatePath = finalResponse.AutoNavigatePath
        };
    }

    private AiChatResponseDto? GetConfigurationIssue(
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage,
        string? currentPath)
    {
        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            return BuildUnavailableResponse(
                "The AI assistant is almost ready, but the NVIDIA model id is missing in the API configuration.",
                availableRoutes,
                latestUserMessage,
                currentPath);
        }

        if (string.IsNullOrWhiteSpace(ResolveApiKey()))
        {
            return BuildUnavailableResponse(
                "The AI assistant is not configured with an NVIDIA API key yet. Add it on the server to enable live chat.",
                availableRoutes,
                latestUserMessage,
                currentPath);
        }

        return null;
    }

    private HttpRequestMessage BuildProviderRequest(
        AiChatRequestDto request,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        bool stream)
    {
        var payload = new
        {
            model = _options.Model,
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens,
            stream,
            messages = BuildPromptMessages(request, availableRoutes)
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ResolveApiKey());
        return httpRequest;
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
                     .TakeLast(12))
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
            $"- {route.Label} ({route.Path}): {route.Description}"));

        return $$"""
You are HRMS Compass, a warm and practical in-portal assistant for this HRMS product.
Act like a real conversational copilot, not a JSON API.
Use prior messages naturally so the conversation feels continuous.
Stay focused on helping with this portal: features, workflows, permissions, navigation, and what the user can do on the current page.
Be concise but conversational. Usually answer in 2 to 5 short sentences.
If a question is unrelated to the HRMS portal, politely say you specialize in this portal and steer the user back to product help.
Do not invent pages, permissions, data, or workflows that are not listed.
If navigation would help, mention the exact route label so the UI can suggest it.

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

    private string GetLatestUserMessage(AiChatRequestDto request) =>
        request.Messages.LastOrDefault(message =>
            string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content?.Trim() ?? string.Empty;

    private IReadOnlyCollection<PortalRoute> GetAvailableRoutes() =>
        PortalRoutes
            .Where(route => route.AllowedRoles.Count == 0 || route.AllowedRoles.Intersect(_currentUserService.Roles, StringComparer.OrdinalIgnoreCase).Any())
            .ToArray();

    private AiChatResponseDto BuildUnavailableResponse(
        string message,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage,
        string? currentPath)
    {
        var fallback = BuildResponsePayload(message, availableRoutes, latestUserMessage, currentPath);
        return new AiChatResponseDto
        {
            Message = fallback.Message,
            Actions = fallback.Actions,
            AutoNavigatePath = fallback.AutoNavigatePath
        };
    }

    private AiChatResponseDto BuildResponsePayload(
        string message,
        IReadOnlyCollection<PortalRoute> availableRoutes,
        string latestUserMessage,
        string? currentPath)
    {
        var sanitizedMessage = SanitizeMessage(message);
        var suggestedRoutes = SuggestRoutes(latestUserMessage, sanitizedMessage, currentPath, availableRoutes);
        var shouldAutoNavigate = ShouldAutoNavigate(latestUserMessage);
        var autoRoute = shouldAutoNavigate ? suggestedRoutes.FirstOrDefault() : null;

        return new AiChatResponseDto
        {
            Message = sanitizedMessage,
            Actions = suggestedRoutes
                .Take(3)
                .Select(route => new AiAssistantActionDto
                {
                    Label = route.Label,
                    Path = route.Path,
                    Description = route.Description,
                    AutoNavigate = string.Equals(route.Path, autoRoute?.Path, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AutoNavigatePath = autoRoute?.Path
        };
    }

    private IReadOnlyCollection<PortalRoute> SuggestRoutes(
        string latestUserMessage,
        string assistantMessage,
        string? currentPath,
        IReadOnlyCollection<PortalRoute> availableRoutes)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void AddScore(PortalRoute route, int amount)
        {
            scores[route.Id] = scores.TryGetValue(route.Id, out var current) ? current + amount : amount;
        }

        if (TryGetCurrentRoute(currentPath, availableRoutes) is { } currentRoute)
        {
            if (IsCurrentPageQuestion(latestUserMessage))
            {
                AddScore(currentRoute, 8);
            }

            if (string.Equals(currentRoute.Path, "/dashboard", StringComparison.OrdinalIgnoreCase) &&
                Regex.IsMatch(latestUserMessage, @"\b(start|overview|summary)\b", RegexOptions.IgnoreCase))
            {
                AddScore(currentRoute, 4);
            }
        }

        var combinedContext = $"{latestUserMessage} {assistantMessage}";
        foreach (var route in availableRoutes)
        {
            var score = GetRouteScore(combinedContext, route);
            if (score > 0)
            {
                AddScore(route, score);
            }
        }

        if (Regex.IsMatch(latestUserMessage, @"\b(main|portal|features|modules|help)\b", RegexOptions.IgnoreCase))
        {
            foreach (var route in GetDefaultRecommendedRoutes(availableRoutes))
            {
                AddScore(route, 3);
            }
        }

        return availableRoutes
            .Where(route => scores.ContainsKey(route.Id))
            .OrderByDescending(route => scores[route.Id])
            .ThenBy(route => route.Label)
            .ToArray();
    }

    private static bool IsCurrentPageQuestion(string message) =>
        Regex.IsMatch(message, @"\b(this page|here|current page|what can i do)\b", RegexOptions.IgnoreCase);

    private static PortalRoute? TryGetCurrentRoute(string? currentPath, IReadOnlyCollection<PortalRoute> availableRoutes) =>
        availableRoutes.FirstOrDefault(route =>
            string.Equals(route.Path, currentPath, StringComparison.OrdinalIgnoreCase));

    private IReadOnlyCollection<PortalRoute> GetDefaultRecommendedRoutes(IReadOnlyCollection<PortalRoute> availableRoutes)
    {
        var preferredOrder = new[] { "/dashboard", "/attendance", "/leaves", "/payroll", "/employees", "/documents" };
        return preferredOrder
            .Select(path => availableRoutes.FirstOrDefault(route => string.Equals(route.Path, path, StringComparison.OrdinalIgnoreCase)))
            .Where(route => route is not null)
            .Cast<PortalRoute>()
            .Take(3)
            .ToArray();
    }

    private static int GetRouteScore(string content, PortalRoute route)
    {
        var score = 0;
        var normalized = content.Trim().ToLowerInvariant();

        if (normalized.Contains(route.Label.ToLowerInvariant()))
        {
            score += 5;
        }

        if (normalized.Contains(route.Path.Trim('/').ToLowerInvariant()))
        {
            score += 4;
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
        Regex.IsMatch(userMessage, @"\b(go|open|navigate|redirect|take me|show me|bring me)\b", RegexOptions.IgnoreCase);

    private static string ExtractAssistantContent(NvidiaChatCompletionResponse? completion)
    {
        var content = completion?.Choices.FirstOrDefault()?.Message?.Content;
        if (content is null || content.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return string.Empty;
        }

        return ExtractContentValue(content.Value);
    }

    private static string ExtractAssistantDelta(string rawChunk)
    {
        try
        {
            var chunk = JsonSerializer.Deserialize<NvidiaChatCompletionChunk>(rawChunk, JsonOptions);
            var content = chunk?.Choices.FirstOrDefault()?.Delta?.Content;
            if (content is null || content.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return string.Empty;
            }

            return ExtractContentValue(content.Value);
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string ExtractContentValue(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in content.EnumerateArray())
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

            return string.Join(string.Empty, parts);
        }

        return string.Empty;
    }

    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "I can help with HRMS features, workflows, and navigation.";
        }

        var cleaned = message
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleaned.Length <= 900 ? cleaned : cleaned[..900].TrimEnd();
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

    private sealed class NvidiaChatCompletionChunk
    {
        public IReadOnlyCollection<NvidiaChunkChoice> Choices { get; init; } = Array.Empty<NvidiaChunkChoice>();
    }

    private sealed class NvidiaChunkChoice
    {
        public NvidiaDelta? Delta { get; init; }
    }

    private sealed class NvidiaDelta
    {
        public JsonElement? Content { get; init; }
    }
}
