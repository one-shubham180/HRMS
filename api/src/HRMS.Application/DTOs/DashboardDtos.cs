namespace HRMS.Application.DTOs;

public record AdminDashboardDto(
    int TotalEmployees,
    int TotalDepartments,
    int PendingLeaves,
    int PresentToday,
    decimal MonthlyPayroll);

public record EmployeeDashboardDto(
    EmployeeDto Profile,
    decimal AnnualLeaveBalance,
    decimal SickLeaveBalance,
    decimal CasualLeaveBalance,
    IReadOnlyCollection<AttendanceRecordDto> RecentAttendance,
    IReadOnlyCollection<LeaveRequestDto> RecentLeaves);
