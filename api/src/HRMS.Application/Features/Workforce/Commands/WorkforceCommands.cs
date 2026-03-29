using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Services;
using MediatR;

namespace HRMS.Application.Features.Workforce.Commands;

public record CreateHolidayCalendarCommand(string Name, string Code, bool IsDefault) : IRequest<Guid>;
public record AddHolidayDateCommand(Guid? HolidayCalendarId, DateOnly Date, string Name, bool IsOptional) : IRequest<Guid>;
public record CreateShiftDefinitionCommand(
    string Name,
    string Code,
    string StartTimeLocal,
    string EndTimeLocal,
    decimal StandardHours,
    int BreakMinutes,
    int MinimumOvertimeMinutes) : IRequest<ShiftDefinitionDto>;
public record AssignRosterCommand(Guid EmployeeId, Guid ShiftDefinitionId, DateOnly WorkDate, bool IsRestDay, string? Notes) : IRequest<RosterAssignmentDto>;

public class CreateHolidayCalendarCommandValidator : AbstractValidator<CreateHolidayCalendarCommand>
{
    public CreateHolidayCalendarCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
    }
}

public class AddHolidayDateCommandValidator : AbstractValidator<AddHolidayDateCommand>
{
    public AddHolidayDateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public class CreateShiftDefinitionCommandValidator : AbstractValidator<CreateShiftDefinitionCommand>
{
    public CreateShiftDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.StartTimeLocal)
            .NotEmpty()
            .Must(BeValidTime)
            .WithMessage("Start time must be in HH:mm format.");
        RuleFor(x => x.EndTimeLocal)
            .NotEmpty()
            .Must(BeValidTime)
            .WithMessage("End time must be in HH:mm format.");
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumOvertimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(HaveValidShiftWindow)
            .WithMessage("Shift hours must be greater than zero after subtracting the break.");
    }

    private static bool BeValidTime(string? value) =>
        !string.IsNullOrWhiteSpace(value) && TimeOnly.TryParseExact(value.Trim(), "HH:mm", out _);

    private static bool HaveValidShiftWindow(CreateShiftDefinitionCommand command)
    {
        if (!BeValidTime(command.StartTimeLocal) || !BeValidTime(command.EndTimeLocal))
        {
            return false;
        }

        var start = TimeOnly.ParseExact(command.StartTimeLocal.Trim(), "HH:mm");
        var end = TimeOnly.ParseExact(command.EndTimeLocal.Trim(), "HH:mm");
        return AttendancePolicy.CalculateScheduledHours(start, end, command.BreakMinutes) > 0m;
    }
}

public class AssignRosterCommandValidator : AbstractValidator<AssignRosterCommand>
{
    public AssignRosterCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ShiftDefinitionId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(400);
    }
}

public class CreateHolidayCalendarCommandHandler : IRequestHandler<CreateHolidayCalendarCommand, Guid>
{
    private readonly IHolidayCalendarRepository _holidayCalendarRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHolidayCalendarCommandHandler(IHolidayCalendarRepository holidayCalendarRepository, IUnitOfWork unitOfWork)
    {
        _holidayCalendarRepository = holidayCalendarRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateHolidayCalendarCommand request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
        {
            var existingDefault = await _holidayCalendarRepository.GetDefaultAsync(cancellationToken);
            if (existingDefault is not null)
            {
                existingDefault.IsDefault = false;
                existingDefault.ModifiedUtc = DateTime.UtcNow;
                _holidayCalendarRepository.Update(existingDefault);
            }
        }

        var calendar = new HolidayCalendar
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            IsDefault = request.IsDefault
        };

        await _holidayCalendarRepository.AddAsync(calendar, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return calendar.Id;
    }
}

public class AddHolidayDateCommandHandler : IRequestHandler<AddHolidayDateCommand, Guid>
{
    private readonly IHolidayCalendarRepository _holidayCalendarRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddHolidayDateCommandHandler(IHolidayCalendarRepository holidayCalendarRepository, IUnitOfWork unitOfWork)
    {
        _holidayCalendarRepository = holidayCalendarRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddHolidayDateCommand request, CancellationToken cancellationToken)
    {
        HolidayCalendar? calendar = null;
        if (request.HolidayCalendarId.HasValue)
        {
            calendar = await _holidayCalendarRepository.GetByIdAsync(request.HolidayCalendarId.Value, cancellationToken);
        }
        else
        {
            calendar = await _holidayCalendarRepository.GetDefaultAsync(cancellationToken);
        }

        if (calendar is null)
        {
            throw new AppException("Holiday calendar not found.", 404);
        }

        if (await _holidayCalendarRepository.HolidayExistsAsync(calendar.Id, request.Date, cancellationToken))
        {
            throw new AppException("A holiday already exists for the selected date.");
        }

        var holiday = new HolidayDate
        {
            HolidayCalendarId = calendar.Id,
            Date = request.Date,
            Name = request.Name.Trim(),
            IsOptional = request.IsOptional
        };

        calendar.ModifiedUtc = DateTime.UtcNow;

        try
        {
            await _holidayCalendarRepository.AddHolidayAsync(holiday, cancellationToken);
            _holidayCalendarRepository.Update(calendar);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            throw new AppException("Saving failed. Please try again. If the problem continues, verify that this date is not already present in the selected holiday calendar.");
        }

        return holiday.Id;
    }
}

public class CreateShiftDefinitionCommandHandler : IRequestHandler<CreateShiftDefinitionCommand, ShiftDefinitionDto>
{
    private readonly IShiftDefinitionRepository _shiftDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateShiftDefinitionCommandHandler(IShiftDefinitionRepository shiftDefinitionRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _shiftDefinitionRepository = shiftDefinitionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ShiftDefinitionDto> Handle(CreateShiftDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (!TimeOnly.TryParseExact(request.StartTimeLocal.Trim(), "HH:mm", out var startTimeLocal))
        {
            throw new AppException("Start time must be in HH:mm format.", 400);
        }

        if (!TimeOnly.TryParseExact(request.EndTimeLocal.Trim(), "HH:mm", out var endTimeLocal))
        {
            throw new AppException("End time must be in HH:mm format.", 400);
        }

        var calculatedStandardHours = AttendancePolicy.CalculateScheduledHours(
            startTimeLocal,
            endTimeLocal,
            request.BreakMinutes);

        if (calculatedStandardHours <= 0m)
        {
            throw new AppException("Shift hours must be greater than zero after subtracting the break.", 400);
        }

        var shift = new ShiftDefinition
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            StartTimeLocal = startTimeLocal,
            EndTimeLocal = endTimeLocal,
            StandardHours = calculatedStandardHours,
            BreakMinutes = request.BreakMinutes,
            MinimumOvertimeMinutes = request.MinimumOvertimeMinutes
        };

        await _shiftDefinitionRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ShiftDefinitionDto>(shift);
    }
}

public class AssignRosterCommandHandler : IRequestHandler<AssignRosterCommand, RosterAssignmentDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IShiftDefinitionRepository _shiftDefinitionRepository;
    private readonly IRosterAssignmentRepository _rosterAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AssignRosterCommandHandler(
        IEmployeeRepository employeeRepository,
        IShiftDefinitionRepository shiftDefinitionRepository,
        IRosterAssignmentRepository rosterAssignmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _shiftDefinitionRepository = shiftDefinitionRepository;
        _rosterAssignmentRepository = rosterAssignmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<RosterAssignmentDto> Handle(AssignRosterCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", 404);
        var shift = await _shiftDefinitionRepository.GetByIdAsync(request.ShiftDefinitionId, cancellationToken)
            ?? throw new AppException("Shift definition not found.", 404);

        var roster = await _rosterAssignmentRepository.GetByEmployeeAndDateAsync(request.EmployeeId, request.WorkDate, cancellationToken);
        var isNew = roster is null;

        roster ??= new RosterAssignment
        {
            EmployeeId = employee.Id,
            WorkDate = request.WorkDate
        };

        roster.ShiftDefinitionId = shift.Id;
        roster.Employee = employee;
        roster.ShiftDefinition = shift;
        roster.IsRestDay = request.IsRestDay;
        roster.Notes = request.Notes?.Trim();
        roster.ModifiedUtc = DateTime.UtcNow;

        if (isNew)
        {
            await _rosterAssignmentRepository.AddAsync(roster, cancellationToken);
        }
        else
        {
            _rosterAssignmentRepository.Update(roster);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<RosterAssignmentDto>(roster);
    }
}
