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
        var result = await Sender.Send(new GetRecentAuditTrailQuery(take), cancellationToken);
        return Ok(result);
    }
}
