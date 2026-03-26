using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class PayrollRecord : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal PayableDays { get; set; }
    public decimal LossOfPayDays { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    public string PayslipNumber { get; set; } = string.Empty;

    public Employee? Employee { get; set; }
}
