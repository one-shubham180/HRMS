using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs;

public record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly WorkDate,
    DateTime CheckInUtc,
    DateTime? CheckInCapturedPhotoUtc,
    string? CheckInPhotoUrl,
    decimal? CheckInLatitude,
    decimal? CheckInLongitude,
    string? CheckInLocationLabel,
    DateTime? CheckOutUtc,
    DateTime? CheckOutCapturedPhotoUtc,
    string? CheckOutPhotoUrl,
    decimal? CheckOutLatitude,
    decimal? CheckOutLongitude,
    string? CheckOutLocationLabel,
    AttendanceStatus Status,
    decimal WorkedHours,
    string? Notes);

public record AttendanceSettingsDto(bool RequireGeoTaggedPhotoForAttendance);
