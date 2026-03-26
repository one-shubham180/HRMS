using HRMS.Application.Features.Departments.Commands;
using HRMS.Application.Features.Departments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class DepartmentsController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDepartmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetById(Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDepartmentByIdQuery(departmentId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { departmentId = result.Id }, result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> Update(Guid departmentId, [FromBody] UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { DepartmentId = departmentId }, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> Delete(Guid departmentId, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteDepartmentCommand(departmentId), cancellationToken);
        return NoContent();
    }
}
