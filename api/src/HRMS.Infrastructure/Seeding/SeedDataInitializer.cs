using HRMS.Application.Common.Constants;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Infrastructure.Identity;
using HRMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Seeding;

public class SeedDataInitializer
{
    private readonly HrmsDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public SeedDataInitializer(
        HrmsDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureSchemaUpgradesAsync(cancellationToken);

        foreach (var role in ApplicationRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });
            }
        }

        if (!await _context.Departments.AnyAsync(cancellationToken))
        {
            _context.Departments.AddRange(
                new Department { Name = "Human Resources", Code = "HR", Description = "People operations and employee success" },
                new Department { Name = "Engineering", Code = "ENG", Description = "Software development and delivery" },
                new Department { Name = "Finance", Code = "FIN", Description = "Payroll and financial operations" });

            await _context.SaveChangesAsync(cancellationToken);
        }

        var departments = await _context.Departments.ToListAsync(cancellationToken);
        var hrDepartment = departments.First(x => x.Code == "HR");
        var engDepartment = departments.First(x => x.Code == "ENG");
        var holidayCalendar = await EnsureDefaultHolidayCalendarAsync(cancellationToken);
        await EnsureDefaultShiftDefinitionAsync(cancellationToken);

        await EnsureSeedUserAsync(
            "admin@hrms.local",
            "Admin@123",
            "System",
            "Administrator",
            ApplicationRoles.Admin,
            hrDepartment.Id,
            "EMP001",
            "HRMS Administrator",
            EmploymentType.FullTime,
            65000,
            12000,
            3500,
            2500,
            1500,
            2500,
            4200,
            holidayCalendar.Id,
            cancellationToken);

        await EnsureSeedUserAsync(
            "hr@hrms.local",
            "Hr@12345",
            "Helen",
            "Roberts",
            ApplicationRoles.HR,
            hrDepartment.Id,
            "EMP002",
            "HR Manager",
            EmploymentType.FullTime,
            50000,
            10000,
            3000,
            2000,
            1200,
            1800,
            3200,
            holidayCalendar.Id,
            cancellationToken);

        await EnsureSeedUserAsync(
            "employee@hrms.local",
            "Emp@12345",
            "Ethan",
            "Walker",
            ApplicationRoles.Employee,
            engDepartment.Id,
            "EMP003",
            "Software Engineer",
            EmploymentType.FullTime,
            42000,
            8500,
            2500,
            1800,
            1000,
            1500,
            2600,
            holidayCalendar.Id,
            cancellationToken);

        if (!await _context.AttendanceRecords.AnyAsync(cancellationToken))
        {
            var employee = await _context.Employees.FirstAsync(x => x.EmployeeCode == "EMP003", cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));

            _context.AttendanceRecords.AddRange(
                new AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    WorkDate = today.AddDays(-2),
                    CheckInUtc = DateTime.UtcNow.AddDays(-2).Date.AddHours(3.8),
                    CheckOutUtc = DateTime.UtcNow.AddDays(-2).Date.AddHours(12.2),
                    Status = AttendanceStatus.Present,
                    WorkedHours = 8.4m
                },
                new AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    WorkDate = today.AddDays(-1),
                    CheckInUtc = DateTime.UtcNow.AddDays(-1).Date.AddHours(4.3),
                    CheckOutUtc = DateTime.UtcNow.AddDays(-1).Date.AddHours(11.7),
                    Status = AttendanceStatus.Late,
                    WorkedHours = 7.4m
                });

            _context.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Annual,
                Status = LeaveStatus.Pending,
                StartDate = today.AddDays(4),
                EndDate = today.AddDays(5),
                TotalDays = 2,
                Reason = "Family event"
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSchemaUpgradesAsync(CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[dbo].[AttendanceSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AttendanceSettings] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [RequireGeoTaggedPhotoForAttendance] bit NOT NULL CONSTRAINT [DF_AttendanceSettings_RequireGeoTaggedPhotoForAttendance] DEFAULT(0)
    );
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[AttendanceSettings])
BEGIN
    INSERT INTO [dbo].[AttendanceSettings] ([Id], [CreatedUtc], [ModifiedUtc], [RequireGeoTaggedPhotoForAttendance])
    VALUES (NEWID(), SYSUTCDATETIME(), NULL, 0);
END;

IF COL_LENGTH('dbo.Attendance', 'CheckInCapturedPhotoUtc') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckInCapturedPhotoUtc] datetime2 NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckInPhotoUrl') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckInPhotoUrl] nvarchar(512) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckInLatitude') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckInLatitude] decimal(9,6) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckInLongitude') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckInLongitude] decimal(9,6) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckInLocationLabel') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckInLocationLabel] nvarchar(200) NULL;

IF COL_LENGTH('dbo.Attendance', 'CheckOutCapturedPhotoUtc') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckOutCapturedPhotoUtc] datetime2 NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckOutPhotoUrl') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckOutPhotoUrl] nvarchar(512) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckOutLatitude') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckOutLatitude] decimal(9,6) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckOutLongitude') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckOutLongitude] decimal(9,6) NULL;
IF COL_LENGTH('dbo.Attendance', 'CheckOutLocationLabel') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [CheckOutLocationLabel] nvarchar(200) NULL;

IF COL_LENGTH('dbo.AspNetUsers', 'MustChangePassword') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [MustChangePassword] bit NOT NULL CONSTRAINT [DF_AspNetUsers_MustChangePassword] DEFAULT(0);
IF COL_LENGTH('dbo.AspNetUsers', 'DeactivatedUtc') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [DeactivatedUtc] datetime2 NULL;
IF COL_LENGTH('dbo.AspNetUsers', 'IsActive') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT(1);

IF COL_LENGTH('dbo.Employees', 'HolidayCalendarId') IS NULL
    ALTER TABLE [dbo].[Employees] ADD [HolidayCalendarId] uniqueidentifier NULL;
IF COL_LENGTH('dbo.Employees', 'SourceCandidateId') IS NULL
    ALTER TABLE [dbo].[Employees] ADD [SourceCandidateId] uniqueidentifier NULL;
IF COL_LENGTH('dbo.Employees', 'DeactivatedUtc') IS NULL
    ALTER TABLE [dbo].[Employees] ADD [DeactivatedUtc] datetime2 NULL;
IF COL_LENGTH('dbo.Employees', 'DeactivatedByUserId') IS NULL
    ALTER TABLE [dbo].[Employees] ADD [DeactivatedByUserId] uniqueidentifier NULL;

IF COL_LENGTH('dbo.Attendance', 'RosterAssignmentId') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [RosterAssignmentId] uniqueidentifier NULL;
IF COL_LENGTH('dbo.Attendance', 'ScheduledShiftName') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [ScheduledShiftName] nvarchar(120) NULL;
IF COL_LENGTH('dbo.Attendance', 'ScheduledStartTimeLocal') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [ScheduledStartTimeLocal] time NULL;
IF COL_LENGTH('dbo.Attendance', 'ScheduledEndTimeLocal') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [ScheduledEndTimeLocal] time NULL;
IF COL_LENGTH('dbo.Attendance', 'ScheduledHours') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [ScheduledHours] decimal(8,2) NOT NULL CONSTRAINT [DF_Attendance_ScheduledHours] DEFAULT(0);
IF COL_LENGTH('dbo.Attendance', 'OvertimeHours') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [OvertimeHours] decimal(8,2) NOT NULL CONSTRAINT [DF_Attendance_OvertimeHours] DEFAULT(0);
IF COL_LENGTH('dbo.Attendance', 'IsHoliday') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [IsHoliday] bit NOT NULL CONSTRAINT [DF_Attendance_IsHoliday] DEFAULT(0);
IF COL_LENGTH('dbo.Attendance', 'HolidayName') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [HolidayName] nvarchar(160) NULL;
IF COL_LENGTH('dbo.Attendance', 'IsRestDay') IS NULL
    ALTER TABLE [dbo].[Attendance] ADD [IsRestDay] bit NOT NULL CONSTRAINT [DF_Attendance_IsRestDay] DEFAULT(0);

IF OBJECT_ID(N'[dbo].[HolidayCalendars]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HolidayCalendars] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [Name] nvarchar(120) NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [IsDefault] bit NOT NULL
    );
    CREATE UNIQUE INDEX [IX_HolidayCalendars_Code] ON [dbo].[HolidayCalendars]([Code]);
END;

IF OBJECT_ID(N'[dbo].[HolidayDates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HolidayDates] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [HolidayCalendarId] uniqueidentifier NOT NULL,
        [Date] date NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [IsOptional] bit NOT NULL,
        CONSTRAINT [FK_HolidayDates_HolidayCalendars_HolidayCalendarId] FOREIGN KEY ([HolidayCalendarId]) REFERENCES [dbo].[HolidayCalendars]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_HolidayDates_Calendar_Date] ON [dbo].[HolidayDates]([HolidayCalendarId], [Date]);
END;

IF OBJECT_ID(N'[dbo].[ShiftDefinitions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ShiftDefinitions] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [Name] nvarchar(120) NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [StartTimeLocal] time NOT NULL,
        [EndTimeLocal] time NOT NULL,
        [StandardHours] decimal(8,2) NOT NULL,
        [BreakMinutes] int NOT NULL,
        [MinimumOvertimeMinutes] int NOT NULL
    );
    CREATE UNIQUE INDEX [IX_ShiftDefinitions_Code] ON [dbo].[ShiftDefinitions]([Code]);
END;

IF OBJECT_ID(N'[dbo].[RosterAssignments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RosterAssignments] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [ShiftDefinitionId] uniqueidentifier NOT NULL,
        [WorkDate] date NOT NULL,
        [IsRestDay] bit NOT NULL,
        [Notes] nvarchar(400) NULL,
        CONSTRAINT [FK_RosterAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RosterAssignments_ShiftDefinitions_ShiftDefinitionId] FOREIGN KEY ([ShiftDefinitionId]) REFERENCES [dbo].[ShiftDefinitions]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_RosterAssignments_EmployeeId_WorkDate] ON [dbo].[RosterAssignments]([EmployeeId], [WorkDate]);
END;

IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [TriggeredByUserId] uniqueidentifier NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [Title] nvarchar(160) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [RelatedEntityType] nvarchar(80) NOT NULL,
        [RelatedEntityId] uniqueidentifier NULL,
        [DeliveredUtc] datetime2 NULL,
        [ReadUtc] datetime2 NULL
    );
    CREATE INDEX [IX_Notifications_RecipientUserId_Status] ON [dbo].[Notifications]([RecipientUserId], [Status]);
END;

IF OBJECT_ID(N'[dbo].[AuditTrail]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditTrail] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [ActorUserId] uniqueidentifier NULL,
        [NotificationItemId] uniqueidentifier NULL,
        [EntityType] nvarchar(80) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [Action] nvarchar(120) NOT NULL,
        [OldState] nvarchar(120) NULL,
        [NewState] nvarchar(120) NULL,
        [Metadata] nvarchar(4000) NULL,
        [OccurredUtc] datetime2 NOT NULL,
        CONSTRAINT [FK_AuditTrail_Notifications_NotificationItemId] FOREIGN KEY ([NotificationItemId]) REFERENCES [dbo].[Notifications]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_AuditTrail_EntityType_EntityId_OccurredUtc] ON [dbo].[AuditTrail]([EntityType], [EntityId], [OccurredUtc]);
END;

IF OBJECT_ID(N'[dbo].[EmployeeDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmployeeDocuments] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [PayrollRecordId] uniqueidentifier NULL,
        [Category] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [StoragePath] nvarchar(512) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSize] bigint NOT NULL,
        [IsSystemGenerated] bit NOT NULL,
        [UploadedByUserId] uniqueidentifier NULL,
        CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeDocuments_Payroll_PayrollRecordId] FOREIGN KEY ([PayrollRecordId]) REFERENCES [dbo].[Payroll]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_EmployeeDocuments_EmployeeId_Category_CreatedUtc] ON [dbo].[EmployeeDocuments]([EmployeeId], [Category], [CreatedUtc]);
END;

IF OBJECT_ID(N'[dbo].[EmployeeDocuments]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeDocuments_Payroll_PayrollRecordId')
    BEGIN
        ALTER TABLE [dbo].[EmployeeDocuments] DROP CONSTRAINT [FK_EmployeeDocuments_Payroll_PayrollRecordId];
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeDocuments_Payroll_PayrollRecordId')
    BEGIN
        ALTER TABLE [dbo].[EmployeeDocuments]
        ADD CONSTRAINT [FK_EmployeeDocuments_Payroll_PayrollRecordId]
        FOREIGN KEY ([PayrollRecordId]) REFERENCES [dbo].[Payroll]([Id]) ON DELETE NO ACTION;
    END;
END;

IF OBJECT_ID(N'[dbo].[Candidates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Candidates] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [ConvertedEmployeeId] uniqueidentifier NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [JobTitle] nvarchar(128) NOT NULL,
        [Status] int NOT NULL,
        [HiredDate] date NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [FK_Candidates_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_Candidates_Email] ON [dbo].[Candidates]([Email]);
END;

IF OBJECT_ID(N'[dbo].[PerformanceAppraisals]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PerformanceAppraisals] (
        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [InitializedFromCandidateId] uniqueidentifier NULL,
        [CycleName] nvarchar(160) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] int NOT NULL,
        [GoalsSummary] nvarchar(2000) NULL,
        CONSTRAINT [FK_PerformanceAppraisals_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PerformanceAppraisals_Candidates_InitializedFromCandidateId] FOREIGN KEY ([InitializedFromCandidateId]) REFERENCES [dbo].[Candidates]([Id]) ON DELETE SET NULL
    );
END;
""", cancellationToken);
    }

    private async Task<HolidayCalendar> EnsureDefaultHolidayCalendarAsync(CancellationToken cancellationToken)
    {
        var calendar = await _context.HolidayCalendars
            .Include(x => x.Holidays)
            .FirstOrDefaultAsync(x => x.Code == "IND-DEFAULT", cancellationToken);

        if (calendar is null)
        {
            calendar = new HolidayCalendar
            {
                Name = "India Default Calendar",
                Code = "IND-DEFAULT",
                IsDefault = true,
                Holidays =
                {
                    new HolidayDate { Date = new DateOnly(DateTime.UtcNow.Year, 1, 26), Name = "Republic Day" },
                    new HolidayDate { Date = new DateOnly(DateTime.UtcNow.Year, 8, 15), Name = "Independence Day" },
                    new HolidayDate { Date = new DateOnly(DateTime.UtcNow.Year, 10, 2), Name = "Gandhi Jayanti" }
                }
            };

            _context.HolidayCalendars.Add(calendar);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return calendar;
    }

    private async Task EnsureDefaultShiftDefinitionAsync(CancellationToken cancellationToken)
    {
        if (await _context.ShiftDefinitions.AnyAsync(x => x.Code == "GENERAL", cancellationToken))
        {
            return;
        }

        _context.ShiftDefinitions.Add(new ShiftDefinition
        {
            Name = "General Shift",
            Code = "GENERAL",
            StartTimeLocal = new TimeOnly(9, 0),
            EndTimeLocal = new TimeOnly(18, 0),
            StandardHours = 8m,
            BreakMinutes = 60,
            MinimumOvertimeMinutes = 30
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSeedUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid departmentId,
        string employeeCode,
        string jobTitle,
        EmploymentType employmentType,
        decimal basicSalary,
        decimal hra,
        decimal conveyance,
        decimal medical,
        decimal other,
        decimal providentFund,
        decimal tax,
        Guid holidayCalendarId,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existingUser is null)
        {
            existingUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(existingUser, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }

            await _userManager.AddToRoleAsync(existingUser, role);
        }

        if (!await _context.Employees.AnyAsync(x => x.UserId == existingUser.Id, cancellationToken))
        {
            var employee = new Employee
            {
                UserId = existingUser.Id,
                DepartmentId = departmentId,
                HolidayCalendarId = holidayCalendarId,
                EmployeeCode = employeeCode,
                FirstName = firstName,
                LastName = lastName,
                WorkEmail = email,
                DateOfBirth = new DateOnly(1995, 1, 10),
                JoinDate = new DateOnly(2024, 1, 2),
                JobTitle = jobTitle,
                EmploymentType = employmentType
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);

            _context.SalaryStructures.Add(new SalaryStructure
            {
                EmployeeId = employee.Id,
                BasicSalary = basicSalary,
                HouseRentAllowance = hra,
                ConveyanceAllowance = conveyance,
                MedicalAllowance = medical,
                OtherAllowance = other,
                ProvidentFundDeduction = providentFund,
                TaxDeduction = tax
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var employee = await _context.Employees.FirstAsync(x => x.UserId == existingUser.Id, cancellationToken);
            if (!employee.HolidayCalendarId.HasValue)
            {
                employee.HolidayCalendarId = holidayCalendarId;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
