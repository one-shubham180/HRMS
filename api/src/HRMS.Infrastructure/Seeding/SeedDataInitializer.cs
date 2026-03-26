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
    }
}
