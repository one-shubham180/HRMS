namespace HRMS.Application.DTOs;

public sealed class AiChatRequestDto
{
    public IReadOnlyCollection<AiChatMessageDto> Messages { get; init; } = Array.Empty<AiChatMessageDto>();
    public string? CurrentPath { get; init; }
}

public sealed class AiChatMessageDto
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;
}

public sealed class AiChatResponseDto
{
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<AiAssistantActionDto> Actions { get; init; } = Array.Empty<AiAssistantActionDto>();
    public string? AutoNavigatePath { get; init; }
}

public sealed class AiAssistantActionDto
{
    public string Label { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool AutoNavigate { get; init; }
}
