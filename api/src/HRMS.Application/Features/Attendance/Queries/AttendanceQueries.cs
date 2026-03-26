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
