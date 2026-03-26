using HRMS.Application.Features.Payroll.Commands;
using HRMS.Application.Features.Payroll.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Api.Controllers;

[Authorize]
public class PayrollController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] int? year = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPayrollRecordsQuery(employeeId, year, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("salary-structures")]
    public async Task<IActionResult> UpsertSalaryStructure([FromBody] UpsertSalaryStructureCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateMonthlyPayrollCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
