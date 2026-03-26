using HRMS.Application.Features.Employees.Commands;
using HRMS.Application.Features.Employees.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class EmployeesController : ApiControllerBase
{
    [Authorize(Roles = "Admin,HR")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetEmployeesQuery(pageNumber, pageSize, search, departmentId, sortBy, descending), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> GetById(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEmployeeByIdQuery(employeeId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { employeeId = result.Id }, result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPut("{employeeId:guid}")]
    public async Task<IActionResult> Update(Guid employeeId, [FromBody] UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { EmployeeId = employeeId }, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{employeeId:guid}")]
    public async Task<IActionResult> Delete(Guid employeeId, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteEmployeeCommand(employeeId), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,HR,Employee")]
    [HttpPost("{employeeId:guid}/profile-image")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadProfileImage(Guid employeeId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var result = await Sender.Send(
            new UploadEmployeeProfileImageCommand(employeeId, memoryStream, file.FileName, file.ContentType),
            cancellationToken);

        return Ok(new { imageUrl = result });
    }
}
