using AutoMapper;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Workforce.Queries;

public record GetShiftDefinitionsQuery() : IRequest<IReadOnlyCollection<ShiftDefinitionDto>>;
public record GetHolidayCalendarsQuery() : IRequest<IReadOnlyCollection<HolidayCalendarDto>>;
public record GetRosterAssignmentsQuery(Guid? EmployeeId = null, DateOnly? WorkDate = null) : IRequest<IReadOnlyCollection<RosterAssignmentDto>>;

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
        return shifts.Select(x => _mapper.Map<ShiftDefinitionDto>(x)).ToArray();
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
        var rosters = await _rosterAssignmentRepository.GetFilteredAsync(request.EmployeeId, request.WorkDate, cancellationToken);
        return rosters.Select(x => _mapper.Map<RosterAssignmentDto>(x)).ToArray();
    }
}
