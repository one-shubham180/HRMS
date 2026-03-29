using AutoMapper;
using HRMS.Application.Common.Constants;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Attendance.Queries;

public record GetAttendanceLogsQuery(
    Guid? EmployeeId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<AttendanceRecordDto>>;
public record GetAttendanceSettingsQuery() : IRequest<AttendanceSettingsDto>;
public record GetAttendanceExportQuery(DateOnly WorkDate, string Scope) : IRequest<IReadOnlyCollection<AttendanceExportRowDto>>;

public record AttendanceExportRowDto(
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string WorkEmail,
    string JobTitle,
    string Status,
    string WorkDate,
    string? CheckInLocal,
    string? CheckOutLocal,
    decimal WorkedHours,
    decimal ScheduledHours,
    decimal OvertimeHours,
    string? ShiftName,
    string? HolidayName,
    bool IsRestDay,
    string? Notes);

public class GetAttendanceLogsQueryHandler : IRequestHandler<GetAttendanceLogsQuery, PagedResult<AttendanceRecordDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetAttendanceLogsQueryHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResult<AttendanceRecordDto>> Handle(GetAttendanceLogsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;

        if (_currentUserService.IsInRole(ApplicationRoles.Employee))
        {
            if (_currentUserService.UserId is null)
            {
                throw new AppException("User context is unavailable.", 401);
            }

            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
                ?? throw new AppException("Employee profile not found.", 404);

            employeeId = employee.Id;
        }

        var result = await _attendanceRepository.GetPagedAsync(
            new AttendanceLogFilter(employeeId, request.StartDate, request.EndDate, request.PageNumber, request.PageSize),
            cancellationToken);

        return new PagedResult<AttendanceRecordDto>
        {
            Items = _mapper.Map<IReadOnlyCollection<AttendanceRecordDto>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}

public class GetAttendanceSettingsQueryHandler : IRequestHandler<GetAttendanceSettingsQuery, AttendanceSettingsDto>
{
    private readonly IAttendanceSettingsRepository _attendanceSettingsRepository;

    public GetAttendanceSettingsQueryHandler(IAttendanceSettingsRepository attendanceSettingsRepository)
    {
        _attendanceSettingsRepository = attendanceSettingsRepository;
    }

    public async Task<AttendanceSettingsDto> Handle(GetAttendanceSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _attendanceSettingsRepository.GetCurrentAsync(cancellationToken);
        return new AttendanceSettingsDto(settings?.RequireGeoTaggedPhotoForAttendance ?? false);
    }
}

public class GetAttendanceExportQueryHandler
    : IRequestHandler<GetAttendanceExportQuery, IReadOnlyCollection<AttendanceExportRowDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public GetAttendanceExportQueryHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<IReadOnlyCollection<AttendanceExportRowDto>> Handle(
        GetAttendanceExportQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedScope = (request.Scope ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedScope is not ("present" or "absent" or "all"))
        {
            throw new AppException("Invalid attendance export scope. Use present, absent, or all.", 400);
        }

        var employees = await _employeeRepository.GetActiveForPayrollAsync(null, cancellationToken);
        var attendanceRecords = await _attendanceRepository.GetByDateAsync(request.WorkDate, cancellationToken);
        var attendanceLookup = attendanceRecords
            .GroupBy(record => record.EmployeeId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(record => record.CheckInUtc).First());

        var rows = employees
            .Select(employee =>
            {
                attendanceLookup.TryGetValue(employee.Id, out var attendanceRecord);
                var status = attendanceRecord?.Status.ToString() ?? "Absent";
                var includeRow = normalizedScope switch
                {
                    "present" => attendanceRecord is not null,
                    "absent" => attendanceRecord is null,
                    _ => true
                };

                if (!includeRow)
                {
                    return null;
                }

                return new AttendanceExportRowDto(
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Department?.Name ?? "Unassigned",
                    employee.WorkEmail,
                    employee.JobTitle,
                    status,
                    request.WorkDate.ToString("yyyy-MM-dd"),
                    attendanceRecord?.CheckInUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    attendanceRecord?.CheckOutUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    attendanceRecord?.WorkedHours ?? 0m,
                    attendanceRecord?.ScheduledHours ?? 0m,
                    attendanceRecord?.OvertimeHours ?? 0m,
                    attendanceRecord?.ScheduledShiftName,
                    attendanceRecord?.HolidayName,
                    attendanceRecord?.IsRestDay ?? false,
                    attendanceRecord?.Notes);
            })
            .Where(row => row is not null)
            .Cast<AttendanceExportRowDto>()
            .OrderBy(row => row.EmployeeName)
            .ToList();

        return rows;
    }
}
