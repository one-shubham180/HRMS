namespace HRMS.Application.DTOs;

public record SalaryStructureDto(
    Guid Id,
    Guid EmployeeId,
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal ConveyanceAllowance,
    decimal MedicalAllowance,
    decimal OtherAllowance,
    decimal ProvidentFundDeduction,
    decimal TaxDeduction,
    decimal GrossSalary,
    decimal TotalDeductions);

public record PayrollRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    int Year,
    int Month,
    decimal PayableDays,
    decimal LossOfPayDays,
    decimal GrossSalary,
    decimal TotalDeductions,
    decimal NetSalary,
    string PayslipNumber,
    DateTime GeneratedUtc);
