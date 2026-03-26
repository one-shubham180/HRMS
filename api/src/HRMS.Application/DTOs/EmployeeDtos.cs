using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs;

public record EmployeeDto(
    Guid Id,
    Guid UserId,
    Guid DepartmentId,
    string DepartmentName,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string WorkEmail,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    DateOnly JoinDate,
    string JobTitle,
    EmploymentType EmploymentType,
    decimal AnnualLeaveBalance,
    decimal SickLeaveBalance,
    decimal CasualLeaveBalance,
    string? ProfileImageUrl,
    bool IsActive,
    decimal GrossSalary);
