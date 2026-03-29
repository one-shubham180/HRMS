using System.Text;
using System.Globalization;
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

        var scheduledHours = AttendancePolicy.CalculateScheduledHours(
            shift.StartTimeLocal,
            shift.EndTimeLocal,
            shift.BreakMinutes);
        var overtimeHours = 0m;
        if (holiday is not null || rosterAssignment?.IsRestDay == true)
        {
            overtimeHours = workedHours;
        }
        else
        {
            var extraHours = Math.Max(workedHours - scheduledHours, 0m);
            overtimeHours = extraHours * 60m >= shift.MinimumOvertimeMinutes ? extraHours : 0m;
        }

        return new OvertimeComputationResult(
            rosterAssignment?.Id,
            shift.Name,
            shift.StartTimeLocal,
            shift.EndTimeLocal,
            scheduledHours,
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
        var bytes = BuildStyledPayslipPdf(payrollRecord);
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

    private static byte[] BuildStyledPayslipPdf(PayrollRecord payrollRecord)
    {
        var employeeName = payrollRecord.Employee?.FullName ?? $"Employee {payrollRecord.EmployeeId}";
        var salary = payrollRecord.Employee?.SalaryStructure;
        var periodLabel = new DateTime(payrollRecord.Year, payrollRecord.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var generatedLabel = payrollRecord.GeneratedUtc.ToString("dd MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        var details = new[]
        {
            ("Employee", employeeName),
            ("Employee Code", payrollRecord.Employee?.EmployeeCode ?? "-"),
            ("Department", payrollRecord.Employee?.Department?.Name ?? "-"),
            ("Designation", payrollRecord.Employee?.JobTitle ?? "-"),
            ("Pay Period", periodLabel),
            ("Join Date", payrollRecord.Employee?.JoinDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "-"),
            ("Payable Days", payrollRecord.PayableDays.ToString("0.##", CultureInfo.InvariantCulture)),
            ("Loss Of Pay", payrollRecord.LossOfPayDays.ToString("0.##", CultureInfo.InvariantCulture))
        };

        var earnings = salary is not null
            ? new List<(string Label, decimal Amount)>
            {
                ("Basic Salary", salary.BasicSalary),
                ("House Rent Allowance", salary.HouseRentAllowance),
                ("Conveyance Allowance", salary.ConveyanceAllowance),
                ("Medical Allowance", salary.MedicalAllowance),
                ("Other Allowance", salary.OtherAllowance),
            }
            : new List<(string Label, decimal Amount)>
            {
                ("Gross Salary Snapshot", payrollRecord.GrossSalary)
            };

        var deductions = salary is not null
            ? new List<(string Label, decimal Amount)>
            {
                ("Provident Fund", salary.ProvidentFundDeduction),
                ("Tax Deduction", salary.TaxDeduction),
            }
            : new List<(string Label, decimal Amount)>
            {
                ("Total Deductions", payrollRecord.TotalDeductions)
            };

        var content = BuildPayslipContent(
            payrollRecord,
            employeeName,
            periodLabel,
            generatedLabel,
            details,
            earnings,
            deductions);
        var contentBytes = Encoding.ASCII.GetBytes(content);

        var objects = new[]
        {
            "%PDF-1.4\n",
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 6 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {contentBytes.Length} >> stream\n{content}\nendstream endobj\n",
            "6 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj\n"
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

    private static string BuildPayslipContent(
        PayrollRecord payrollRecord,
        string employeeName,
        string periodLabel,
        string generatedLabel,
        IReadOnlyList<(string Label, string Value)> details,
        IReadOnlyList<(string Label, decimal Amount)> earnings,
        IReadOnlyList<(string Label, decimal Amount)> deductions)
    {
        var builder = new StringBuilder();

        DrawFilledRectangle(builder, 0, 0, 612, 792, 0.99m, 0.99m, 0.99m);
        DrawFilledRectangle(builder, 40, 676, 532, 82, 0.07m, 0.15m, 0.18m);
        DrawText(builder, "F2", 24, 58, 726, "PAYSLIP", 1m, 1m, 1m);
        DrawText(builder, "F1", 11, 58, 704, $"Payslip No: {payrollRecord.PayslipNumber}", 0.90m, 0.95m, 0.96m);
        DrawText(builder, "F1", 10, 58, 688, $"Generated: {generatedLabel}", 0.78m, 0.87m, 0.89m);
        DrawRightAlignedText(builder, "F2", 14, 552, 723, $"NET PAY INR {FormatAmount(payrollRecord.NetSalary)}", 1m, 1m, 1m);
        DrawRightAlignedText(builder, "F1", 10, 552, 703, employeeName, 0.88m, 0.94m, 0.95m);
        DrawRightAlignedText(builder, "F1", 10, 552, 687, periodLabel, 0.78m, 0.87m, 0.89m);

        DrawSummaryCard(builder, 40, 600, 164, 58, "Gross Salary", payrollRecord.GrossSalary, 0.94m, 0.97m, 0.95m);
        DrawSummaryCard(builder, 224, 600, 164, 58, "Deductions", payrollRecord.TotalDeductions, 0.99m, 0.95m, 0.91m);
        DrawSummaryCard(builder, 408, 600, 164, 58, "Net Salary", payrollRecord.NetSalary, 0.93m, 0.96m, 0.99m);

        DrawSectionBox(builder, 40, 426, 532, 152, "Employee Details");
        var detailStartY = 520m;
        for (var index = 0; index < details.Count; index += 1)
        {
            var column = index % 2;
            var row = index / 2;
            var x = column == 0 ? 58m : 320m;
            var y = detailStartY - (row * 24m);
            DrawText(builder, "F1", 9, x, y + 10, details[index].Label.ToUpperInvariant(), 0.45m, 0.52m, 0.60m);
            DrawText(builder, "F2", 11, x, y - 4, details[index].Value, 0.11m, 0.16m, 0.20m);
        }

        DrawTable(builder, 40, 188, 252, 222, "Earnings", earnings, "Gross Salary", payrollRecord.GrossSalary);
        DrawTable(builder, 320, 188, 252, 222, "Deductions", deductions, "Total Deductions", payrollRecord.TotalDeductions);

        DrawSectionBox(builder, 40, 116, 532, 48, "Notes");
        DrawText(builder, "F1", 10, 58, 136, "This is a system-generated payslip and does not require a physical signature.", 0.26m, 0.33m, 0.39m);

        return builder.ToString();
    }

    private static void DrawSummaryCard(StringBuilder builder, decimal x, decimal y, decimal width, decimal height, string label, decimal amount, decimal r, decimal g, decimal b)
    {
        DrawFilledRectangle(builder, x, y, width, height, r, g, b);
        DrawStrokedRectangle(builder, x, y, width, height, 0.90m, 0.92m, 0.94m, 0.8m);
        DrawText(builder, "F1", 9, x + 16, y + height - 18, label.ToUpperInvariant(), 0.38m, 0.45m, 0.52m);
        DrawText(builder, "F2", 18, x + 16, y + 18, $"INR {FormatAmount(amount)}", 0.11m, 0.16m, 0.20m);
    }

    private static void DrawSectionBox(StringBuilder builder, decimal x, decimal y, decimal width, decimal height, string title)
    {
        DrawFilledRectangle(builder, x, y, width, height, 1m, 1m, 1m);
        DrawStrokedRectangle(builder, x, y, width, height, 0.89m, 0.91m, 0.93m, 0.8m);
        DrawFilledRectangle(builder, x, y + height - 30, width, 30, 0.95m, 0.97m, 0.99m);
        DrawText(builder, "F2", 11, x + 18, y + height - 19, title, 0.11m, 0.16m, 0.20m);
    }

    private static void DrawTable(
        StringBuilder builder,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string title,
        IReadOnlyList<(string Label, decimal Amount)> rows,
        string totalLabel,
        decimal totalAmount)
    {
        DrawSectionBox(builder, x, y, width, height, title);
        var top = y + height - 52;
        DrawText(builder, "F1", 9, x + 18, top, "Description", 0.45m, 0.52m, 0.60m);
        DrawRightAlignedText(builder, "F1", 9, x + width - 18, top, "Amount (INR)", 0.45m, 0.52m, 0.60m);
        DrawLine(builder, x + 16, top - 8, x + width - 16, top - 8, 0.89m, 0.91m, 0.93m, 0.8m);

        var currentY = top - 28;
        foreach (var row in rows)
        {
            DrawText(builder, "F1", 10, x + 18, currentY, row.Label, 0.17m, 0.22m, 0.27m);
            DrawRightAlignedText(builder, "F1", 10, x + width - 18, currentY, FormatAmount(row.Amount), 0.17m, 0.22m, 0.27m);
            currentY -= 24;
        }

        DrawLine(builder, x + 16, y + 42, x + width - 16, y + 42, 0.82m, 0.85m, 0.89m, 1m);
        DrawText(builder, "F2", 10, x + 18, y + 20, totalLabel, 0.11m, 0.16m, 0.20m);
        DrawRightAlignedText(builder, "F2", 11, x + width - 18, y + 20, FormatAmount(totalAmount), 0.11m, 0.16m, 0.20m);
    }

    private static void DrawFilledRectangle(StringBuilder builder, decimal x, decimal y, decimal width, decimal height, decimal r, decimal g, decimal b)
    {
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} rg {3:0.##} {4:0.##} {5:0.##} {6:0.##} re f\n", r, g, b, x, y, width, height);
    }

    private static void DrawStrokedRectangle(StringBuilder builder, decimal x, decimal y, decimal width, decimal height, decimal r, decimal g, decimal b, decimal lineWidth)
    {
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} RG {3:0.##} w {4:0.##} {5:0.##} {6:0.##} {7:0.##} re S\n", r, g, b, lineWidth, x, y, width, height);
    }

    private static void DrawLine(StringBuilder builder, decimal x1, decimal y1, decimal x2, decimal y2, decimal r, decimal g, decimal b, decimal lineWidth)
    {
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} RG {3:0.##} w {4:0.##} {5:0.##} m {6:0.##} {7:0.##} l S\n", r, g, b, lineWidth, x1, y1, x2, y2);
    }

    private static void DrawText(StringBuilder builder, string fontName, decimal fontSize, decimal x, decimal y, string text, decimal r, decimal g, decimal b)
    {
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "BT /{0} {1:0.##} Tf {2:0.###} {3:0.###} {4:0.###} rg 1 0 0 1 {5:0.##} {6:0.##} Tm ({7}) Tj ET\n",
            fontName,
            fontSize,
            r,
            g,
            b,
            x,
            y,
            EscapePdfText(text));
    }

    private static void DrawRightAlignedText(StringBuilder builder, string fontName, decimal fontSize, decimal rightX, decimal y, string text, decimal r, decimal g, decimal b)
    {
        var estimatedWidth = EstimateTextWidth(text, fontSize);
        DrawText(builder, fontName, fontSize, rightX - estimatedWidth, y, text, r, g, b);
    }

    private static decimal EstimateTextWidth(string text, decimal fontSize) =>
        Math.Max(text.Length * fontSize * 0.48m, 0m);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string EscapePdfText(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
