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
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceSettings> AttendanceSettings => Set<AttendanceSettings>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
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
        });

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("Attendance");
            entity.HasIndex(x => new { x.EmployeeId, x.WorkDate }).IsUnique();
            entity.Property(x => x.WorkedHours).HasPrecision(8, 2);
            entity.Property(x => x.Notes).HasMaxLength(400);
            entity.Property(x => x.CheckInPhotoUrl).HasMaxLength(512);
            entity.Property(x => x.CheckOutPhotoUrl).HasMaxLength(512);
            entity.Property(x => x.CheckInLatitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckInLongitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckOutLatitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckOutLongitude).HasPrecision(9, 6);
            entity.Property(x => x.CheckInLocationLabel).HasMaxLength(200);
            entity.Property(x => x.CheckOutLocationLabel).HasMaxLength(200);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.AttendanceRecords)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
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

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(200).IsRequired();
        });
    }
}
