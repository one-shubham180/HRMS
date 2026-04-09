using HRMS.Application.DTOs;

namespace HRMS.Application.Common.Interfaces;

public interface IAiAssistantService
{
    Task<AiChatResponseDto> GetResponseAsync(AiChatRequestDto request, CancellationToken cancellationToken);
    IAsyncEnumerable<AiChatStreamEventDto> StreamResponseAsync(AiChatRequestDto request, CancellationToken cancellationToken);
}
