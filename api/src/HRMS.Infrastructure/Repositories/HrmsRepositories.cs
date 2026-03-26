using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Services;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HrmsDbContext _context;

    public EmployeeRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Employee employee, CancellationToken cancellationToken) =>
        _context.Employees.AddAsync(employee, cancellationToken).AsTask();

    public Task<Employee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        _context.Employees
            .Include(x => x.Department)
            .Include(x => x.SalaryStructure)
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken);

    public Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.Employees
            .Include(x => x.Department)
            .Include(x => x.SalaryStructure)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<PagedResult<Employee>> GetPagedAsync(EmployeeListFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.SalaryStructure)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(search) ||
                x.LastName.ToLower().Contains(search) ||
                x.EmployeeCode.ToLower().Contains(search) ||
                x.WorkEmail.ToLower().Contains(search));
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        query = (filter.SortBy?.ToLowerInvariant(), filter.Descending) switch
        {
            ("joindate", true) => query.OrderByDescending(x => x.JoinDate),
            ("joindate", false) => query.OrderBy(x => x.JoinDate),
            ("department", true) => query.OrderByDescending(x => x.Department!.Name),
            ("department", false) => query.OrderBy(x => x.Department!.Name),
            ("name", true) => query.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName),
            _ => query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken) =>
        _context.Employees.AnyAsync(x => x.EmployeeCode == employeeCode.Trim(), cancellationToken);

    public Task<bool> ExistsByWorkEmailAsync(string workEmail, CancellationToken cancellationToken) =>
        _context.Employees.AnyAsync(x => x.WorkEmail == workEmail.Trim().ToLower(), cancellationToken);

    public void Update(Employee employee) => _context.Employees.Update(employee);

    public void Remove(Employee employee) => _context.Employees.Remove(employee);
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly HrmsDbContext _context;

    public DepartmentRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Department department, CancellationToken cancellationToken) =>
        _context.Departments.AddAsync(department, cancellationToken).AsTask();

    public Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken) =>
        _context.Departments.FirstOrDefaultAsync(x => x.Id == departmentId, cancellationToken);

    public async Task<IReadOnlyCollection<Department>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.Departments.AnyAsync(x => x.Code == code.Trim().ToUpper(), cancellationToken);

    public void Update(Department department) => _context.Departments.Update(department);

    public void Remove(Department department) => _context.Departments.Remove(department);
}

public class AttendanceRepository : IAttendanceRepository
{
    private readonly HrmsDbContext _context;

    public AttendanceRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(AttendanceRecord attendanceRecord, CancellationToken cancellationToken) =>
        _context.AttendanceRecords.AddAsync(attendanceRecord, cancellationToken).AsTask();

    public Task<AttendanceRecord?> GetOpenAttendanceAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken) =>
        _context.AttendanceRecords.FirstOrDefaultAsync(
            x => x.EmployeeId == employeeId && x.WorkDate == workDate && x.CheckOutUtc == null,
            cancellationToken);

    public Task<bool> ExistsForDateAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken) =>
        _context.AttendanceRecords.AnyAsync(x => x.EmployeeId == employeeId && x.WorkDate == workDate, cancellationToken);

    public async Task<PagedResult<AttendanceRecord>> GetPagedAsync(AttendanceLogFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.AttendanceRecords.AsNoTracking().AsQueryable();

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.WorkDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.WorkDate <= filter.EndDate.Value);
        }

        query = query.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.CheckInUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AttendanceRecord>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyCollection<AttendanceRecord>> GetRecentByEmployeeAsync(Guid employeeId, int take, CancellationToken cancellationToken) =>
        await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.WorkDate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetLossOfPayDaysAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.WorkDate.Year == year && x.WorkDate.Month == month)
            .ToListAsync(cancellationToken);

        return records.Sum(x => AttendancePolicy.CalculateLossOfPayFactor(x.Status, x.WorkedHours));
    }
}

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly HrmsDbContext _context;

    public LeaveRequestRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken) =>
        _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken).AsTask();

    public Task<LeaveRequest?> GetByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken) =>
        _context.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == leaveRequestId, cancellationToken);

    public async Task<PagedResult<LeaveRequest>> GetPagedAsync(LeaveListFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaveRequest>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyCollection<LeaveRequest>> GetRecentByEmployeeAsync(Guid employeeId, int take, CancellationToken cancellationToken) =>
        await _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<int> GetApprovedUnpaidLeaveDaysAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        var leaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.Status == LeaveStatus.Approved && x.LeaveType == LeaveType.Unpaid)
            .ToListAsync(cancellationToken);

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var days = 0;

        foreach (var leave in leaves)
        {
            var start = leave.StartDate > monthStart ? leave.StartDate : monthStart;
            var end = leave.EndDate < monthEnd ? leave.EndDate : monthEnd;

            if (end >= start)
            {
                days += end.DayNumber - start.DayNumber + 1;
            }
        }

        return days;
    }

    public void Update(LeaveRequest leaveRequest) => _context.LeaveRequests.Update(leaveRequest);
}

public class SalaryStructureRepository : ISalaryStructureRepository
{
    private readonly HrmsDbContext _context;

    public SalaryStructureRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task<SalaryStructure?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken) =>
        _context.SalaryStructures
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, cancellationToken);

    public Task AddAsync(SalaryStructure salaryStructure, CancellationToken cancellationToken) =>
        _context.SalaryStructures.AddAsync(salaryStructure, cancellationToken).AsTask();

    public void Update(SalaryStructure salaryStructure) => _context.SalaryStructures.Update(salaryStructure);
}

public class PayrollRepository : IPayrollRepository
{
    private readonly HrmsDbContext _context;

    public PayrollRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken) =>
        _context.PayrollRecords.AddAsync(payrollRecord, cancellationToken).AsTask();

    public Task<PayrollRecord?> GetByEmployeeAndPeriodAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken) =>
        _context.PayrollRecords
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Month == month, cancellationToken);

    public async Task<PagedResult<PayrollRecord>> GetPagedAsync(PayrollListFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.PayrollRecords
            .AsNoTracking()
            .Include(x => x.Employee)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(x => x.Year == filter.Year.Value);
        }

        query = query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PayrollRecord>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public void Update(PayrollRecord payrollRecord) => _context.PayrollRecords.Update(payrollRecord);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly HrmsDbContext _context;

    public RefreshTokenRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken) =>
        _context.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public void Update(RefreshToken refreshToken) => _context.RefreshTokens.Update(refreshToken);
}

public class DashboardRepository : IDashboardRepository
{
    private readonly HrmsDbContext _context;

    public DashboardRepository(HrmsDbContext context)
    {
        _context = context;
    }

    public Task<int> GetEmployeeCountAsync(CancellationToken cancellationToken) =>
        _context.Employees.CountAsync(cancellationToken);

    public Task<int> GetDepartmentCountAsync(CancellationToken cancellationToken) =>
        _context.Departments.CountAsync(cancellationToken);

    public Task<int> GetPendingLeaveCountAsync(CancellationToken cancellationToken) =>
        _context.LeaveRequests.CountAsync(x => x.Status == LeaveStatus.Pending, cancellationToken);

    public Task<int> GetEmployeesPresentTodayAsync(DateOnly workDate, CancellationToken cancellationToken) =>
        _context.AttendanceRecords.CountAsync(x => x.WorkDate == workDate, cancellationToken);

    public async Task<decimal> GetMonthlyPayrollTotalAsync(int year, int month, CancellationToken cancellationToken) =>
        await _context.PayrollRecords
            .Where(x => x.Year == year && x.Month == month)
            .Select(x => x.NetSalary)
            .DefaultIfEmpty(0)
            .SumAsync(cancellationToken);
}
