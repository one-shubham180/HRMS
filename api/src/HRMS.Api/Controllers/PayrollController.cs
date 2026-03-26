using HRMS.Application.Features.Payroll.Commands;
using HRMS.Application.Features.Payroll.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HRMS.Api.Controllers;

[Authorize]
public class PayrollController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPayrollRecordsQuery(employeeId, departmentId, year, month, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var records = await Sender.Send(new ExportPayrollRecordsQuery(year, month, departmentId, employeeId), cancellationToken);
        var fileName = $"payroll-{year}-{month:D2}.csv";
        var csv = new StringBuilder()
            .AppendLine("Employee Name,Year,Month,Payslip Number,Gross Salary,Total Deductions,Net Salary,Payable Days,Loss Of Pay Days,Generated Utc");

        foreach (var record in records)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(record.EmployeeName),
                record.Year,
                record.Month,
                EscapeCsv(record.PayslipNumber),
                record.GrossSalary,
                record.TotalDeductions,
                record.NetSalary,
                record.PayableDays,
                record.LossOfPayDays,
                record.GeneratedUtc.ToString("O")));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("salary-structures/{employeeId:guid}")]
    public async Task<IActionResult> GetSalaryStructure(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSalaryStructureQuery(employeeId), cancellationToken);
        return result is null ? NoContent() : Ok(result);
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

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("generate-batch")]
    public async Task<IActionResult> GenerateBatch([FromBody] GeneratePayrollBatchCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
