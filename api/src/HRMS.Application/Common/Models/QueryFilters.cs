using HRMS.Domain.Enums;

namespace HRMS.Application.Common.Models;

public record EmployeeListFilter(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? DepartmentId = null,
    string? SortBy = null,
    bool Descending = false);

public record AttendanceLogFilter(
    Guid? EmployeeId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int PageNumber = 1,
    int PageSize = 10);

public record LeaveListFilter(
    Guid? EmployeeId = null,
    LeaveStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 10);

public record PayrollListFilter(
    Guid? EmployeeId = null,
    int? Year = null,
    int PageNumber = 1,
    int PageSize = 10);
