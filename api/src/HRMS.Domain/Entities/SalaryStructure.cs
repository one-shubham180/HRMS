using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class SalaryStructure : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HouseRentAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal ProvidentFundDeduction { get; set; }
    public decimal TaxDeduction { get; set; }

    public Employee? Employee { get; set; }

    public decimal GrossSalary => BasicSalary + HouseRentAllowance + ConveyanceAllowance + MedicalAllowance + OtherAllowance;
    public decimal TotalDeductions => ProvidentFundDeduction + TaxDeduction;
}
