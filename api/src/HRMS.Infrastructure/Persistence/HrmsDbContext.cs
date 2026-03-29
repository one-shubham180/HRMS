using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Persistence;

public class HrmsDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<HolidayDate> HolidayDates => Set<HolidayDate>();
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<RosterAssignment> RosterAssignments => Set<RosterAssignment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceSettings> AttendanceSettings => Set<AttendanceSettings>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<NotificationItem> NotificationItems => Set<NotificationItem>();
    public DbSet<AuditTrailEntry> AuditTrailEntries => Set<AuditTrailEntry>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<PerformanceAppraisal> PerformanceAppraisals => Set<PerformanceAppraisal>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.HasIndex(x => x.WorkEmail).IsUnique();
            entity.Property(x => x.EmployeeCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.WorkEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.JobTitle).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(512);
            entity.Property(x => x.AnnualLeaveBalance).HasPrecision(8, 2);
            entity.Property(x => x.SickLeaveBalance).HasPrecision(8, 2);
            entity.Property(x => x.CasualLeaveBalance).HasPrecision(8, 2);
            entity.HasOne(x => x.Department)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.HolidayCalendar)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.HolidayCalendarId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.SourceCandidate)
                .WithOne(x => x.ConvertedEmployee)
                .HasForeignKey<Employee>(x => x.SourceCandidateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<HolidayCalendar>(entity =>
        {
            entity.ToTable("HolidayCalendars");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
        });

        builder.Entity<HolidayDate>(entity =>
        {
            entity.ToTable("HolidayDates");
            entity.HasIndex(x => new { x.HolidayCalendarId, x.Date }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasOne(x => x.HolidayCalendar)
                .WithMany(x => x.Holidays)
                .HasForeignKey(x => x.HolidayCalendarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShiftDefinition>(entity =>
        {
            entity.ToTable("ShiftDefinitions");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.StandardHours).HasPrecision(8, 2);
        });

        builder.Entity<RosterAssignment>(entity =>
        {
            entity.ToTable("RosterAssignments");
            entity.HasIndex(x => new { x.EmployeeId, x.WorkDate }).IsUnique();
            entity.Property(x => x.Notes).HasMaxLength(400);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.RosterAssignments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ShiftDefinition)
                .WithMany(x => x.RosterAssignments)
                .HasForeignKey(x => x.ShiftDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("Attendance");
            entity.HasIndex(x => new { x.EmployeeId, x.WorkDate }).IsUnique();
            entity.Property(x => x.WorkedHours).HasPrecision(8, 2);
            entity.Property(x => x.ScheduledHours).HasPrecision(8, 2);
            entity.Property(x => x.OvertimeHours).HasPrecision(8, 2);
            entity.Property(x => x.Notes).HasMaxLength(400);
            entity.Property(x => x.CheckInPhotoUrl).HasMaxLength(512);
            entity.Property(x => x.CheckOutPhotoUrl).HasMaxLength(512);
            entity.Property(x => x.CheckInLatitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckInLongitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckOutLatitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckOutLongitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckInLocationLabel).HasMaxLength(200);
            entity.Property(x => x.CheckOutLocationLabel).HasMaxLength(200);
            entity.Property(x => x.ScheduledShiftName).HasMaxLength(120);
            entity.Property(x => x.HolidayName).HasMaxLength(160);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.AttendanceRecords)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RosterAssignment)
                .WithMany(x => x.AttendanceRecords)
                .HasForeignKey(x => x.RosterAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AttendanceSettings>(entity =>
        {
            entity.ToTable("AttendanceSettings");
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("Leaves");
            entity.HasIndex(x => new { x.EmployeeId, x.Status });
            entity.Property(x => x.TotalDays).HasPrecision(8, 2);
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ReviewRemarks).HasMaxLength(500);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.LeaveRequests)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SalaryStructure>(entity =>
        {
            entity.ToTable("SalaryStructures");
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.Property(x => x.BasicSalary).HasPrecision(18, 2);
            entity.Property(x => x.HouseRentAllowance).HasPrecision(18, 2);
            entity.Property(x => x.ConveyanceAllowance).HasPrecision(18, 2);
            entity.Property(x => x.MedicalAllowance).HasPrecision(18, 2);
            entity.Property(x => x.OtherAllowance).HasPrecision(18, 2);
            entity.Property(x => x.ProvidentFundDeduction).HasPrecision(18, 2);
            entity.Property(x => x.TaxDeduction).HasPrecision(18, 2);
            entity.HasOne(x => x.Employee)
                .WithOne(x => x.SalaryStructure)
                .HasForeignKey<SalaryStructure>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PayrollRecord>(entity =>
        {
            entity.ToTable("Payroll");
            entity.HasIndex(x => new { x.EmployeeId, x.Year, x.Month }).IsUnique();
            entity.HasIndex(x => x.PayslipNumber).IsUnique();
            entity.Property(x => x.PayableDays).HasPrecision(8, 2);
            entity.Property(x => x.LossOfPayDays).HasPrecision(8, 2);
            entity.Property(x => x.GrossSalary).HasPrecision(18, 2);
            entity.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            entity.Property(x => x.NetSalary).HasPrecision(18, 2);
            entity.Property(x => x.PayslipNumber).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.PayrollRecords)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NotificationItem>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasIndex(x => new { x.RecipientUserId, x.Status });
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RelatedEntityType).HasMaxLength(80).IsRequired();
        });

        builder.Entity<AuditTrailEntry>(entity =>
        {
            entity.ToTable("AuditTrail");
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredUtc });
            entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OldState).HasMaxLength(120);
            entity.Property(x => x.NewState).HasMaxLength(120);
            entity.Property(x => x.Metadata).HasMaxLength(4000);
            entity.HasOne(x => x.NotificationItem)
                .WithMany()
                .HasForeignKey(x => x.NotificationItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<EmployeeDocument>(entity =>
        {
            entity.ToTable("EmployeeDocuments");
            entity.HasIndex(x => new { x.EmployeeId, x.Category, x.CreatedUtc });
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PayrollRecord)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PayrollRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Candidate>(entity =>
        {
            entity.ToTable("Candidates");
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.JobTitle).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PerformanceAppraisal>(entity =>
        {
            entity.ToTable("PerformanceAppraisals");
            entity.Property(x => x.CycleName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.GoalsSummary).HasMaxLength(2000);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.PerformanceAppraisals)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InitializedFromCandidate)
                .WithMany()
                .HasForeignKey(x => x.InitializedFromCandidateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(200).IsRequired();
        });
    }
}
