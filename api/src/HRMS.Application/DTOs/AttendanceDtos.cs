using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs;

public record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly WorkDate,
    DateTime CheckInUtc,
    DateTime? CheckOutUtc,
    AttendanceStatus Status,
    decimal WorkedHours,
    string? Notes);
