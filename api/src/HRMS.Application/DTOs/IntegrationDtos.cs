using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs;

public record ShiftDefinitionDto(
    Guid Id,
    string Name,
    string Code,
    TimeOnly StartTimeLocal,
    TimeOnly EndTimeLocal,
    decimal StandardHours,
    int BreakMinutes,
    int MinimumOvertimeMinutes);

public record HolidayDateDto(
    Guid Id,
    DateOnly Date,
    string Name,
    bool IsOptional);

public record HolidayCalendarDto(
    Guid Id,
    string Name,
    string Code,
    bool IsDefault,
    IReadOnlyCollection<HolidayDateDto> Holidays);

public record RosterAssignmentDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid ShiftDefinitionId,
    string ShiftName,
    TimeOnly? ShiftStartTimeLocal,
    TimeOnly? ShiftEndTimeLocal,
    decimal ShiftHours,
    int BreakMinutes,
    DateOnly WorkDate,
    bool IsRestDay,
    string? Notes);

public record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    Guid? TriggeredByUserId,
    NotificationType Type,
    NotificationStatus Status,
    string Title,
    string Message,
    string RelatedEntityType,
    Guid? RelatedEntityId,
    DateTime? DeliveredUtc,
    DateTime? ReadUtc);

public record AuditTrailDto(
    Guid Id,
    Guid? ActorUserId,
    Guid? NotificationItemId,
    string EntityType,
    Guid? EntityId,
    string Action,
    string? OldState,
    string? NewState,
    string? Metadata,
    DateTime OccurredUtc);

public record EmployeeDocumentDto(
    Guid Id,
    Guid EmployeeId,
    Guid? PayrollRecordId,
    DocumentCategory Category,
    string FileName,
    string StoragePath,
    string ContentType,
    long FileSize,
    bool IsSystemGenerated,
    Guid? UploadedByUserId,
    DateTime CreatedUtc);

public record CandidateDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid? ConvertedEmployeeId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string JobTitle,
    CandidateStatus Status,
    DateOnly? HiredDate,
    string? Notes);

public record PerformanceAppraisalDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid? InitializedFromCandidateId,
    string CycleName,
    DateOnly StartDate,
    DateOnly EndDate,
    AppraisalStatus Status,
    string? GoalsSummary);
