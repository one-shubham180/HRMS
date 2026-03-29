using HRMS.Application.Features.Notifications.Commands;
using HRMS.Application.Features.Notifications.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyNotificationsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetRecentAuditTrailQuery(Math.Clamp(take, 1, 40)), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await Sender.Send(new MarkNotificationAsReadCommand(notificationId), cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var updatedCount = await Sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return Ok(new { updatedCount });
    }
}
