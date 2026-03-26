using HRMS.Application.Common.Models;
using HRMS.Domain.Entities;

namespace HRMS.Application.Common.Interfaces;

public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResult<Employee>> GetPagedAsync(EmployeeListFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Employee>> GetActiveForPayrollAsync(Guid? departmentId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken cancellationToken);
    Task<bool> ExistsByWorkEmailAsync(string workEmail, CancellationToken cancellationToken);
    void Update(Employee employee);
    void Remove(Employee employee);
}

public interface IDepartmentRepository
{
    Task AddAsync(Department department, CancellationToken cancellationToken);
    Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Department>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);
    void Update(Department department);
    void Remove(Department department);
}

public interface IAttendanceRepository
{
    Task AddAsync(AttendanceRecord attendanceRecord, CancellationToken cancellationToken);
    Task<AttendanceRecord?> GetOpenAttendanceAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken);
    Task<bool> ExistsForDateAsync(Guid employeeId, DateOnly workDate, CancellationToken cancellationToken);
    Task<PagedResult<AttendanceRecord>> GetPagedAsync(AttendanceLogFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AttendanceRecord>> GetRecentByEmployeeAsync(Guid employeeId, int take, CancellationToken cancellationToken);
    Task<decimal> GetLossOfPayDaysAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken);
}

public interface IAttendanceSettingsRepository
{
    Task<AttendanceSettings?> GetCurrentAsync(CancellationToken cancellationToken);
    Task AddAsync(AttendanceSettings settings, CancellationToken cancellationToken);
    void Update(AttendanceSettings settings);
}

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken);
    Task<LeaveRequest?> GetByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken);
    Task<PagedResult<LeaveRequest>> GetPagedAsync(LeaveListFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LeaveRequest>> GetRecentByEmployeeAsync(Guid employeeId, int take, CancellationToken cancellationToken);
    Task<int> GetApprovedUnpaidLeaveDaysAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken);
    void Update(LeaveRequest leaveRequest);
}

public interface ISalaryStructureRepository
{
    Task<SalaryStructure?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task AddAsync(SalaryStructure salaryStructure, CancellationToken cancellationToken);
    void Update(SalaryStructure salaryStructure);
}

public interface IPayrollRepository
{
    Task AddAsync(PayrollRecord payrollRecord, CancellationToken cancellationToken);
    Task<PayrollRecord?> GetByEmployeeAndPeriodAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken);
    Task<PagedResult<PayrollRecord>> GetPagedAsync(PayrollListFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PayrollRecord>> GetFilteredAsync(PayrollListFilter filter, CancellationToken cancellationToken);
    void Update(PayrollRecord payrollRecord);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    void Update(RefreshToken refreshToken);
}

public interface IDashboardRepository
{
    Task<int> GetEmployeeCountAsync(CancellationToken cancellationToken);
    Task<int> GetDepartmentCountAsync(CancellationToken cancellationToken);
    Task<int> GetPendingLeaveCountAsync(CancellationToken cancellationToken);
    Task<int> GetEmployeesPresentTodayAsync(DateOnly workDate, CancellationToken cancellationToken);
    Task<decimal> GetMonthlyPayrollTotalAsync(int year, int month, CancellationToken cancellationToken);
}
