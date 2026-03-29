using HRMS.Application.Features.Workforce.Commands;
using HRMS.Application.Features.Workforce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize(Roles = "Admin,HR")]
public class WorkforceController : ApiControllerBase
{
    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetShiftDefinitionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("holiday-calendars")]
    public async Task<IActionResult> GetHolidayCalendars(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetHolidayCalendarsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("rosters")]
    public async Task<IActionResult> GetRosters([FromQuery] Guid? employeeId = null, [FromQuery] DateOnly? workDate = null, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetRosterAssignmentsQuery(employeeId, workDate), cancellationToken);
        return Ok(result);
    }

    [HttpPost("holiday-calendars")]
    public async Task<IActionResult> CreateHolidayCalendar([FromBody] CreateHolidayCalendarCommand command, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return Ok(new { holidayCalendarId = id });
    }

    [HttpPost("holiday-dates")]
    public async Task<IActionResult> AddHolidayDate([FromBody] AddHolidayDateCommand command, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return Ok(new { holidayDateId = id });
    }

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftDefinitionCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("rosters")]
    public async Task<IActionResult> AssignRoster([FromBody] AssignRosterCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
