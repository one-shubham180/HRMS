using System.Text;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

public class OvertimeService : IOvertimeService
{
    private readonly IRosterAssignmentRepository _rosterAssignmentRepository;
    private readonly IHolidayCalendarRepository _holidayCalendarRepository;

    public OvertimeService(
        IRosterAssignmentRepository rosterAssignmentRepository,
        IHolidayCalendarRepository holidayCalendarRepository)
    {
        _rosterAssignmentRepository = rosterAssignmentRepository;
        _holidayCalendarRepository = holidayCalendarRepository;
    }

    public async Task<OvertimeComputationResult> CalculateAsync(
        Employee employee,
        DateOnly workDate,
        DateTime checkInUtc,
        DateTime checkOutUtc,
        CancellationToken cancellationToken)
    {
        var rosterAssignment = await _rosterAssignmentRepository.GetByEmployeeAndDateAsync(employee.Id, workDate, cancellationToken);
        var holidayCalendar = employee.HolidayCalendarId.HasValue
            ? await _holidayCalendarRepository.GetByIdAsync(employee.HolidayCalendarId.Value, cancellationToken)
            : await _holidayCalendarRepository.GetDefaultAsync(cancellationToken);

        var holiday = holidayCalendar?.Holidays.FirstOrDefault(x => x.Date == workDate);
        var shift = rosterAssignment?.ShiftDefinition;
        var workedHours = AttendancePolicy.CalculateWorkedHours(checkInUtc, checkOutUtc);

        if (shift is null)
        {
            return new OvertimeComputationResult(
                rosterAssignment?.Id,
                null,
                null,
                null,
                0m,
                holiday is not null || rosterAssignment?.IsRestDay == true ? workedHours : 0m,
                holiday is not null,
                holiday?.Name,
                rosterAssignment?.IsRestDay == true);
        }

        var overtimeHours = 0m;
        if (holiday is not null || rosterAssignment?.IsRestDay == true)
        {
            overtimeHours = workedHours;
        }
        else
        {
            var extraHours = Math.Max(workedHours - shift.StandardHours, 0m);
            overtimeHours = extraHours * 60m >= shift.MinimumOvertimeMinutes ? extraHours : 0m;
        }

        return new OvertimeComputationResult(
            rosterAssignment?.Id,
            shift.Name,
            shift.StartTimeLocal,
            shift.EndTimeLocal,
            shift.StandardHours,
            overtimeHours,
            holiday is not null,
            holiday?.Name,
            rosterAssignment?.IsRestDay == true);
    }
}

public class AuditTrailService : IAuditTrailService
{
    private readonly IAuditTrailRepository _auditTrailRepository;

    public AuditTrailService(IAuditTrailRepository auditTrailRepository)
    {
        _auditTrailRepository = auditTrailRepository;
    }

    public Task WriteAsync(
        Guid? actorUserId,
        string entityType,
        Guid? entityId,
        string action,
        string? oldState,
        string? newState,
        string? metadata,
        Guid? notificationItemId,
        CancellationToken cancellationToken) =>
        _auditTrailRepository.AddAsync(new AuditTrailEntry
        {
            ActorUserId = actorUserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldState = oldState,
            NewState = newState,
            Metadata = metadata,
            NotificationItemId = notificationItemId,
            OccurredUtc = DateTime.UtcNow
        }, cancellationToken);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditTrailService _auditTrailService;

    public NotificationService(
        INotificationRepository notificationRepository,
        IAuditTrailService auditTrailService)
    {
        _notificationRepository = notificationRepository;
        _auditTrailService = auditTrailService;
    }

    public async Task<NotificationItem> CreateAsync(
        Guid recipientUserId,
        Guid? triggeredByUserId,
        NotificationType type,
        string title,
        string message,
        string relatedEntityType,
        Guid? relatedEntityId,
        CancellationToken cancellationToken)
    {
        var notification = new NotificationItem
        {
            RecipientUserId = recipientUserId,
            TriggeredByUserId = triggeredByUserId,
            Type = type,
            Title = title.Trim(),
            Message = message.Trim(),
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            Status = NotificationStatus.Delivered,
            DeliveredUtc = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _auditTrailService.WriteAsync(
            triggeredByUserId,
            nameof(NotificationItem),
            notification.Id,
            "NotificationCreated",
            null,
            notification.Status.ToString(),
            $"{type}:{title}",
            notification.Id,
            cancellationToken);

        return notification;
    }

    public async Task MarkAsDeliveredAsync(NotificationItem notification, CancellationToken cancellationToken)
    {
        var previousStatus = notification.Status;
        notification.Status = NotificationStatus.Delivered;
        notification.DeliveredUtc = DateTime.UtcNow;
        notification.ModifiedUtc = DateTime.UtcNow;
        _notificationRepository.Update(notification);

        await _auditTrailService.WriteAsync(
            notification.TriggeredByUserId,
            nameof(NotificationItem),
            notification.Id,
            "NotificationDelivered",
            previousStatus.ToString(),
            notification.Status.ToString(),
            notification.Title,
            notification.Id,
            cancellationToken);
    }
}

public class OnboardingService : IOnboardingService
{
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IIdentityService identityService,
        INotificationService notificationService,
        IAuditTrailService auditTrailService,
        ICurrentUserService currentUserService,
        ILogger<OnboardingService> logger)
    {
        _identityService = identityService;
        _notificationService = notificationService;
        _auditTrailService = auditTrailService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PasswordSetupResult> SendWelcomeEmailAsync(Employee employee, CancellationToken cancellationToken)
    {
        var setup = await _identityService.GeneratePasswordSetupAsync(employee.UserId, cancellationToken);

        await _notificationService.CreateAsync(
            employee.UserId,
            _currentUserService.UserId,
            NotificationType.Onboarding,
            "Welcome to HRMS",
            $"Your account is ready. Use the password setup link to activate access: {setup.ResetLink}",
            nameof(Employee),
            employee.Id,
            cancellationToken);

        await _auditTrailService.WriteAsync(
            _currentUserService.UserId,
            nameof(Employee),
            employee.Id,
            "WelcomeEmailQueued",
            null,
            "PasswordResetRequired",
            employee.WorkEmail,
            null,
            cancellationToken);

        _logger.LogInformation(
            "Welcome email queued for {Email} with password setup link {ResetLink}",
            employee.WorkEmail,
            setup.ResetLink);

        return setup;
    }
}

public class DocumentVaultService : IDocumentVaultService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmployeeDocumentRepository _employeeDocumentRepository;
    private readonly IAuditTrailService _auditTrailService;

    public DocumentVaultService(
        IFileStorageService fileStorageService,
        IEmployeeDocumentRepository employeeDocumentRepository,
        IAuditTrailService auditTrailService)
    {
        _fileStorageService = fileStorageService;
        _employeeDocumentRepository = employeeDocumentRepository;
        _auditTrailService = auditTrailService;
    }

    public async Task<EmployeeDocument> PublishPayslipAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken)
    {
        var fileName = $"{payrollRecord.PayslipNumber}.pdf";
        var bytes = BuildSimplePdf(payrollRecord);
        await using var stream = new MemoryStream(bytes);
        var storagePath = await _fileStorageService.SaveEmployeeDocumentAsync(stream, fileName, "application/pdf", cancellationToken);

        var document = new EmployeeDocument
        {
            EmployeeId = payrollRecord.EmployeeId,
            PayrollRecordId = payrollRecord.Id,
            Category = DocumentCategory.Payslip,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = "application/pdf",
            FileSize = bytes.Length,
            IsSystemGenerated = true
        };

        await _employeeDocumentRepository.AddAsync(document, cancellationToken);
        await _auditTrailService.WriteAsync(
            null,
            nameof(PayrollRecord),
            payrollRecord.Id,
            "PayslipPublished",
            null,
            storagePath,
            payrollRecord.PayslipNumber,
            null,
            cancellationToken);

        return document;
    }

    private static byte[] BuildSimplePdf(PayrollRecord payrollRecord)
    {
        var employeeName = payrollRecord.Employee?.FullName ?? $"Employee {payrollRecord.EmployeeId}";
        var lines = new[]
        {
            $"Payslip {payrollRecord.PayslipNumber}",
            $"Employee: {employeeName}",
            $"Period: {payrollRecord.Year}-{payrollRecord.Month:D2}",
            $"Gross Salary: {payrollRecord.GrossSalary:0.00}",
            $"Deductions: {payrollRecord.TotalDeductions:0.00}",
            $"Net Salary: {payrollRecord.NetSalary:0.00}",
            $"Generated UTC: {payrollRecord.GeneratedUtc:O}"
        };

        var escapedText = string.Join("\\n", lines.Select(EscapePdfText));
        var content = $"BT /F1 12 Tf 50 760 Td ({escapedText}) Tj ET";
        var contentBytes = Encoding.ASCII.GetBytes(content);

        var objects = new[]
        {
            "%PDF-1.4\n",
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {contentBytes.Length} >> stream\n{content}\nendstream endobj\n"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true);
        var offsets = new List<long> { 0 };

        foreach (var obj in objects)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.Write(obj);
        }

        writer.Flush();
        var xrefPosition = stream.Position;
        writer.Write($"xref\n0 {offsets.Count}\n");
        writer.Write("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            writer.Write($"{offset:0000000000} 00000 n \n");
        }

        writer.Write($"trailer << /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
        writer.Flush();
        return stream.ToArray();
    }

    private static string EscapePdfText(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
