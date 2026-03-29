using HRMS.Domain.Entities;
using HRMS.Domain.Enums;

namespace HRMS.Application.Common.Interfaces;

public record OvertimeComputationResult(
    Guid? RosterAssignmentId,
    string? ShiftName,
    TimeOnly? ScheduledStartTimeLocal,
    TimeOnly? ScheduledEndTimeLocal,
    decimal ScheduledHours,
    decimal OvertimeHours,
    bool IsHoliday,
    string? HolidayName,
    bool IsRestDay);

public record PasswordSetupResult(string ResetToken, string ResetLink);

public interface IOvertimeService
{
    Task<OvertimeComputationResult> CalculateAsync(Employee employee, DateOnly workDate, DateTime checkInUtc, DateTime checkOutUtc, CancellationToken cancellationToken);
}

public interface INotificationService
{
    Task<NotificationItem> CreateAsync(
        Guid recipientUserId,
        Guid? triggeredByUserId,
        NotificationType type,
        string title,
        string message,
        string relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken);

    Task MarkAsDeliveredAsync(NotificationItem notification, CancellationToken cancellationToken);
}

public interface IAuditTrailService
{
    Task WriteAsync(
        Guid? actorUserId,
        string entityType,
        Guid? entityId,
        string action,
        string? oldState,
        string? newState,
        string? metadata,
        Guid? notificationItemId,
        CancellationToken cancellationToken);
}

public interface IOnboardingService
{
    Task<PasswordSetupResult> SendWelcomeEmailAsync(Employee employee, CancellationToken cancellationToken);
}

public interface IDocumentVaultService
{
    Task<EmployeeDocument> PublishPayslipAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken);
}
