using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs;

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    LeaveType LeaveType,
    LeaveStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string? ReviewRemarks);
