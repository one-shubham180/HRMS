using HRMS.Application.Features.Workforce.Commands;
using HRMS.Application.Features.Workforce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class WorkforceController : ApiControllerBase
{
    [Authorize(Roles = "Admin,HR")]
    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShiftDefinitionsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("holiday-calendars")]
    public async Task<IActionResult> GetHolidayCalendars(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetHolidayCalendarsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("rosters")]
    public async Task<IActionResult> GetRosters(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? workDate = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetRosterAssignmentsQuery(employeeId, workDate, startDate, endDate), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("my-rosters")]
    public async Task<IActionResult> GetMyRosters(
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyRosterAssignmentsQuery(startDate, endDate), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("holiday-calendars")]
    public async Task<IActionResult> CreateHolidayCalendar([FromBody] CreateHolidayCalendarCommand command, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return Ok(new { holidayCalendarId = id });
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("holiday-dates")]
    public async Task<IActionResult> AddHolidayDate([FromBody] AddHolidayDateCommand command, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return Ok(new { holidayDateId = id });
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftDefinitionCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("rosters")]
    public async Task<IActionResult> AssignRoster([FromBody] AssignRosterCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
