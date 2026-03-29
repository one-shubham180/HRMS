using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Workforce.Queries;

public record GetShiftDefinitionsQuery() : IRequest<IReadOnlyCollection<ShiftDefinitionDto>>;
public record GetHolidayCalendarsQuery() : IRequest<IReadOnlyCollection<HolidayCalendarDto>>;
public record GetRosterAssignmentsQuery(
    Guid? EmployeeId = null,
    DateOnly? WorkDate = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null) : IRequest<IReadOnlyCollection<RosterAssignmentDto>>;
public record GetMyRosterAssignmentsQuery(DateOnly? StartDate = null, DateOnly? EndDate = null) : IRequest<IReadOnlyCollection<RosterAssignmentDto>>;

public class GetShiftDefinitionsQueryHandler : IRequestHandler<GetShiftDefinitionsQuery, IReadOnlyCollection<ShiftDefinitionDto>>
{
    private readonly IShiftDefinitionRepository _shiftDefinitionRepository;
    private readonly IMapper _mapper;

    public GetShiftDefinitionsQueryHandler(IShiftDefinitionRepository shiftDefinitionRepository, IMapper mapper)
    {
        _shiftDefinitionRepository = shiftDefinitionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ShiftDefinitionDto>> Handle(GetShiftDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var shifts = await _shiftDefinitionRepository.GetAllAsync(cancellationToken);
        return shifts
            .OrderBy(x => x.Name)
            .Select(x => _mapper.Map<ShiftDefinitionDto>(x))
            .ToArray();
    }
}

public class GetHolidayCalendarsQueryHandler : IRequestHandler<GetHolidayCalendarsQuery, IReadOnlyCollection<HolidayCalendarDto>>
{
    private readonly IHolidayCalendarRepository _holidayCalendarRepository;
    private readonly IMapper _mapper;

    public GetHolidayCalendarsQueryHandler(IHolidayCalendarRepository holidayCalendarRepository, IMapper mapper)
    {
        _holidayCalendarRepository = holidayCalendarRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<HolidayCalendarDto>> Handle(GetHolidayCalendarsQuery request, CancellationToken cancellationToken)
    {
        var calendars = await _holidayCalendarRepository.GetAllAsync(cancellationToken);
        return calendars.Select(x => _mapper.Map<HolidayCalendarDto>(x)).ToArray();
    }
}

public class GetRosterAssignmentsQueryHandler : IRequestHandler<GetRosterAssignmentsQuery, IReadOnlyCollection<RosterAssignmentDto>>
{
    private readonly IRosterAssignmentRepository _rosterAssignmentRepository;
    private readonly IMapper _mapper;

    public GetRosterAssignmentsQueryHandler(IRosterAssignmentRepository rosterAssignmentRepository, IMapper mapper)
    {
        _rosterAssignmentRepository = rosterAssignmentRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<RosterAssignmentDto>> Handle(GetRosterAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var rosters = await _rosterAssignmentRepository.GetFilteredAsync(
            request.EmployeeId,
            request.WorkDate,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return rosters.Select(x => _mapper.Map<RosterAssignmentDto>(x)).ToArray();
    }
}

public class GetMyRosterAssignmentsQueryHandler : IRequestHandler<GetMyRosterAssignmentsQuery, IReadOnlyCollection<RosterAssignmentDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRosterAssignmentRepository _rosterAssignmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyRosterAssignmentsQueryHandler(
        IEmployeeRepository employeeRepository,
        IRosterAssignmentRepository rosterAssignmentRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _rosterAssignmentRepository = rosterAssignmentRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<RosterAssignmentDto>> Handle(GetMyRosterAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = request.StartDate ?? today;
        var endDate = request.EndDate ?? today.AddDays(13);

        var rosters = await _rosterAssignmentRepository.GetFilteredAsync(
            employee.Id,
            null,
            startDate,
            endDate,
            cancellationToken);

        return rosters.Select(x => _mapper.Map<RosterAssignmentDto>(x)).ToArray();
    }
}
