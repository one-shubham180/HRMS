using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Services;
using MediatR;

namespace HRMS.Application.Features.Attendance.Commands;

public record CheckInCommand(string? Notes) : IRequest<AttendanceRecordDto>;
public record CheckOutCommand(string? Notes) : IRequest<AttendanceRecordDto>;

public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(400);
    }
}

public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(400);
    }
}

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, AttendanceRecordDto>
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CheckInCommandHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AttendanceRecordDto> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var employee = await GetCurrentEmployeeAsync(cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, IndiaTimeZone);
        var workDate = DateOnly.FromDateTime(localNow);

        if (await _attendanceRepository.ExistsForDateAsync(employee.Id, workDate, cancellationToken))
        {
            throw new AppException("You have already checked in for today.");
        }

        var attendance = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            WorkDate = workDate,
            CheckInUtc = nowUtc,
            Status = AttendancePolicy.ResolveStatus(localNow),
            WorkedHours = 0,
            Notes = request.Notes?.Trim()
        };

        await _attendanceRepository.AddAsync(attendance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<AttendanceRecordDto>(attendance);
    }

    private async Task<Employee> GetCurrentEmployeeAsync(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        return await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);
    }
}

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, AttendanceRecordDto>
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CheckOutCommandHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AttendanceRecordDto> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        var attendance = await _attendanceRepository.GetOpenAttendanceAsync(employee.Id, DateOnly.FromDateTime(localNow), cancellationToken)
            ?? throw new AppException("No active attendance record found for today.");

        attendance.CheckOutUtc = DateTime.UtcNow;
        attendance.WorkedHours = AttendancePolicy.CalculateWorkedHours(attendance.CheckInUtc, attendance.CheckOutUtc.Value);
        attendance.Notes = request.Notes?.Trim() ?? attendance.Notes;
        attendance.ModifiedUtc = DateTime.UtcNow;

        if (attendance.Status == AttendanceStatus.Present && attendance.WorkedHours < 4)
        {
            attendance.Status = AttendanceStatus.HalfDay;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<AttendanceRecordDto>(attendance);
    }
}
