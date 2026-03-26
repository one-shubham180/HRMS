using HRMS.Application.Features.Attendance.Commands;
using HRMS.Application.Features.Attendance.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class AttendanceController : ApiControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAttendanceSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAttendanceSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-in")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> CheckIn([FromForm] AttendanceActionRequest request, CancellationToken cancellationToken)
    {
        await using var memoryStream = request.Photo is null ? null : new MemoryStream();
        if (request.Photo is not null && memoryStream is not null)
        {
            await request.Photo.CopyToAsync(memoryStream, cancellationToken);
        }

        var result = await Sender.Send(
            new CheckInCommand(
                request.Notes,
                memoryStream,
                request.Photo?.FileName,
                request.Photo?.ContentType,
                request.Latitude,
                request.Longitude,
                request.LocationLabel,
                request.CapturedPhotoUtc),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-out")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> CheckOut([FromForm] AttendanceActionRequest request, CancellationToken cancellationToken)
    {
        await using var memoryStream = request.Photo is null ? null : new MemoryStream();
        if (request.Photo is not null && memoryStream is not null)
        {
            await request.Photo.CopyToAsync(memoryStream, cancellationToken);
        }

        var result = await Sender.Send(
            new CheckOutCommand(
                request.Notes,
                memoryStream,
                request.Photo?.FileName,
                request.Photo?.ContentType,
                request.Latitude,
                request.Longitude,
                request.LocationLabel,
                request.CapturedPhotoUtc),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetAttendanceLogsQuery(employeeId, startDate, endDate, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }
}

public class AttendanceActionRequest
{
    public string? Notes { get; set; }
    public IFormFile? Photo { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationLabel { get; set; }
    public DateTime? CapturedPhotoUtc { get; set; }
}
