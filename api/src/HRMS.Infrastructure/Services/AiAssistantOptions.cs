namespace HRMS.Infrastructure.Services;

public sealed class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    public string BaseUrl { get; init; } = "https://integrate.api.nvidia.com/v1";
    public string? ApiKey { get; init; }
    public string? Model { get; init; }
    public double Temperature { get; init; } = 0.3;
    public int MaxTokens { get; init; } = 420;
}
