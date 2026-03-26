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

public record CheckInCommand(
    string? Notes,
    Stream? PhotoStream,
    string? FileName,
    string? ContentType,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationLabel,
    DateTime? CapturedPhotoUtc) : IRequest<AttendanceRecordDto>;

public record CheckOutCommand(
    string? Notes,
    Stream? PhotoStream,
    string? FileName,
    string? ContentType,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationLabel,
    DateTime? CapturedPhotoUtc) : IRequest<AttendanceRecordDto>;

public record UpdateAttendanceSettingsCommand(bool RequireGeoTaggedPhotoForAttendance) : IRequest<AttendanceSettingsDto>;

public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(400);
        RuleFor(x => x.LocationLabel).MaximumLength(200);
    }
}

public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(400);
        RuleFor(x => x.LocationLabel).MaximumLength(200);
    }
}

public class UpdateAttendanceSettingsCommandValidator : AbstractValidator<UpdateAttendanceSettingsCommand>
{
}

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, AttendanceRecordDto>
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceSettingsRepository _attendanceSettingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CheckInCommandHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IAttendanceSettingsRepository attendanceSettingsRepository,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _attendanceSettingsRepository = attendanceSettingsRepository;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AttendanceRecordDto> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var employee = await GetCurrentEmployeeAsync(cancellationToken);
        await EnsurePhotoProofAsync(request, cancellationToken);

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
            Notes = request.Notes?.Trim(),
            CheckInCapturedPhotoUtc = request.CapturedPhotoUtc,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInLocationLabel = request.LocationLabel?.Trim()
        };

        if (request.PhotoStream is not null && !string.IsNullOrWhiteSpace(request.FileName))
        {
            attendance.CheckInPhotoUrl = await _fileStorageService.SaveAttendanceProofImageAsync(
                request.PhotoStream,
                request.FileName,
                request.ContentType,
                cancellationToken);
        }

        await _attendanceRepository.AddAsync(attendance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        attendance.Employee = employee;
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

    private async Task EnsurePhotoProofAsync(CheckInCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsInRole("Employee"))
        {
            return;
        }

        var settings = await _attendanceSettingsRepository.GetCurrentAsync(cancellationToken);
        if (settings?.RequireGeoTaggedPhotoForAttendance != true)
        {
            return;
        }

        if (request.PhotoStream is null || string.IsNullOrWhiteSpace(request.FileName) || request.Latitude is null || request.Longitude is null || request.CapturedPhotoUtc is null)
        {
            throw new AppException("A live geo-tagged photo with location is required to mark attendance.");
        }
    }
}

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, AttendanceRecordDto>
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceSettingsRepository _attendanceSettingsRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CheckOutCommandHandler(
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IAttendanceSettingsRepository attendanceSettingsRepository,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _attendanceSettingsRepository = attendanceSettingsRepository;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AttendanceRecordDto> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        await EnsurePhotoProofAsync(request, cancellationToken);

        var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        var attendance = await _attendanceRepository.GetOpenAttendanceAsync(employee.Id, DateOnly.FromDateTime(localNow), cancellationToken)
            ?? throw new AppException("No active attendance record found for today.");

        attendance.CheckOutUtc = DateTime.UtcNow;
        attendance.WorkedHours = AttendancePolicy.CalculateWorkedHours(attendance.CheckInUtc, attendance.CheckOutUtc.Value);
        attendance.Notes = request.Notes?.Trim() ?? attendance.Notes;
        attendance.ModifiedUtc = DateTime.UtcNow;
        attendance.CheckOutCapturedPhotoUtc = request.CapturedPhotoUtc;
        attendance.CheckOutLatitude = request.Latitude;
        attendance.CheckOutLongitude = request.Longitude;
        attendance.CheckOutLocationLabel = request.LocationLabel?.Trim();

        if (request.PhotoStream is not null && !string.IsNullOrWhiteSpace(request.FileName))
        {
            attendance.CheckOutPhotoUrl = await _fileStorageService.SaveAttendanceProofImageAsync(
                request.PhotoStream,
                request.FileName,
                request.ContentType,
                cancellationToken);
        }

        if (attendance.Status == AttendanceStatus.Present && attendance.WorkedHours < 4)
        {
            attendance.Status = AttendanceStatus.HalfDay;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        attendance.Employee = employee;
        return _mapper.Map<AttendanceRecordDto>(attendance);
    }

    private async Task EnsurePhotoProofAsync(CheckOutCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsInRole("Employee"))
        {
            return;
        }

        var settings = await _attendanceSettingsRepository.GetCurrentAsync(cancellationToken);
        if (settings?.RequireGeoTaggedPhotoForAttendance != true)
        {
            return;
        }

        if (request.PhotoStream is null || string.IsNullOrWhiteSpace(request.FileName) || request.Latitude is null || request.Longitude is null || request.CapturedPhotoUtc is null)
        {
            throw new AppException("A live geo-tagged photo with location is required to mark attendance.");
        }
    }
}

public class UpdateAttendanceSettingsCommandHandler : IRequestHandler<UpdateAttendanceSettingsCommand, AttendanceSettingsDto>
{
    private readonly IAttendanceSettingsRepository _attendanceSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAttendanceSettingsCommandHandler(
        IAttendanceSettingsRepository attendanceSettingsRepository,
        IUnitOfWork unitOfWork)
    {
        _attendanceSettingsRepository = attendanceSettingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AttendanceSettingsDto> Handle(UpdateAttendanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _attendanceSettingsRepository.GetCurrentAsync(cancellationToken);
        var isNew = settings is null;

        settings ??= new AttendanceSettings();
        settings.RequireGeoTaggedPhotoForAttendance = request.RequireGeoTaggedPhotoForAttendance;
        settings.ModifiedUtc = DateTime.UtcNow;

        if (isNew)
        {
            await _attendanceSettingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            _attendanceSettingsRepository.Update(settings);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AttendanceSettingsDto(settings.RequireGeoTaggedPhotoForAttendance);
    }
}
