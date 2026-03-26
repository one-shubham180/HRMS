using HRMS.Application.Features.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class DashboardController : ApiControllerBase
{
    [Authorize(Roles = "Admin,HR")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminDashboardQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("employee")]
    public async Task<IActionResult> GetEmployeeDashboard(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEmployeeDashboardQuery(), cancellationToken);
        return Ok(result);
    }
}
