using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HRMS.Api.Controllers;

[Authorize]
public class AiAssistantController : ApiControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAiAssistantService _aiAssistantService;

    public AiAssistantController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0)
        {
            return BadRequest(new { message = "At least one chat message is required." });
        }

        var response = await _aiAssistantService.GetResponseAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("chat-stream")]
    public async Task ChatStream([FromBody] AiChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Messages.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { message = "At least one chat message is required." }, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var chatEvent in _aiAssistantService.StreamResponseAsync(request, cancellationToken))
        {
            var payload = JsonSerializer.Serialize(chatEvent, StreamJsonOptions);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
