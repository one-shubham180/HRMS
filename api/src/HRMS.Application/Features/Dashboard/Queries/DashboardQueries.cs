using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Dashboard.Queries;

public record GetAdminDashboardQuery() : IRequest<AdminDashboardDto>;
public record GetEmployeeDashboardQuery() : IRequest<EmployeeDashboardDto>;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetAdminDashboardQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var indiaNow = DateTime.UtcNow.AddHours(5.5);
        var workDate = DateOnly.FromDateTime(indiaNow);

        return new AdminDashboardDto(
            await _dashboardRepository.GetEmployeeCountAsync(cancellationToken),
            await _dashboardRepository.GetDepartmentCountAsync(cancellationToken),
            await _dashboardRepository.GetPendingLeaveCountAsync(cancellationToken),
            await _dashboardRepository.GetEmployeesPresentTodayAsync(workDate, cancellationToken),
            await _dashboardRepository.GetMonthlyPayrollTotalAsync(indiaNow.Year, indiaNow.Month, cancellationToken));
    }
}

public class GetEmployeeDashboardQueryHandler : IRequestHandler<GetEmployeeDashboardQuery, EmployeeDashboardDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IMapper _mapper;

    public GetEmployeeDashboardQueryHandler(
        ICurrentUserService currentUserService,
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IMapper mapper)
    {
        _currentUserService = currentUserService;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _mapper = mapper;
    }

    public async Task<EmployeeDashboardDto> Handle(GetEmployeeDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        var attendance = await _attendanceRepository.GetRecentByEmployeeAsync(employee.Id, 5, cancellationToken);
        var leaves = await _leaveRequestRepository.GetRecentByEmployeeAsync(employee.Id, 5, cancellationToken);

        return new EmployeeDashboardDto(
            _mapper.Map<EmployeeDto>(employee),
            employee.AnnualLeaveBalance,
            employee.SickLeaveBalance,
            employee.CasualLeaveBalance,
            _mapper.Map<IReadOnlyCollection<AttendanceRecordDto>>(attendance),
            _mapper.Map<IReadOnlyCollection<LeaveRequestDto>>(leaves));
    }
}
