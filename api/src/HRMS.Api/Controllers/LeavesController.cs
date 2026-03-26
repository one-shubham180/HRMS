using HRMS.Application.Features.Leaves.Commands;
using HRMS.Application.Features.Leaves.Queries;
using HRMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class LeavesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] LeaveStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetLeaveRequestsQuery(employeeId, status, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("{leaveRequestId:guid}/review")]
    public async Task<IActionResult> Review(Guid leaveRequestId, [FromBody] ReviewLeaveCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { LeaveRequestId = leaveRequestId }, cancellationToken);
        return Ok(result);
    }
}
