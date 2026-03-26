using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using MediatR;

namespace HRMS.Application.Features.Leaves.Commands;

public record ApplyLeaveCommand(LeaveType LeaveType, DateOnly StartDate, DateOnly EndDate, string Reason) : IRequest<LeaveRequestDto>;
public record ReviewLeaveCommand(Guid LeaveRequestId, bool Approve, string? Remarks) : IRequest<LeaveRequestDto>;

public class ApplyLeaveCommandValidator : AbstractValidator<ApplyLeaveCommand>
{
    public ApplyLeaveCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
    }
}

public class ReviewLeaveCommandValidator : AbstractValidator<ReviewLeaveCommand>
{
    public ReviewLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class ApplyLeaveCommandHandler : IRequestHandler<ApplyLeaveCommand, LeaveRequestDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ApplyLeaveCommandHandler(
        IEmployeeRepository employeeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<LeaveRequestDto> Handle(ApplyLeaveCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (totalDays <= 0)
        {
            throw new AppException("Leave duration must be at least one day.");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            Reason = request.Reason.Trim()
        };

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        leaveRequest.Employee = employee;

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }
}

public class ReviewLeaveCommandHandler : IRequestHandler<ReviewLeaveCommand, LeaveRequestDto>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewLeaveCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<LeaveRequestDto> Handle(ReviewLeaveCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken)
            ?? throw new AppException("Leave request not found.", 404);

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new AppException("Leave request has already been reviewed.");
        }

        var employee = await _employeeRepository.GetByIdAsync(leaveRequest.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee profile not found.", 404);

        leaveRequest.Status = request.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leaveRequest.ReviewRemarks = request.Remarks?.Trim();
        leaveRequest.ReviewedByUserId = _currentUserService.UserId.Value;
        leaveRequest.ReviewedUtc = DateTime.UtcNow;
        leaveRequest.Employee = employee;

        if (request.Approve)
        {
            ApplyLeaveBalance(employee, leaveRequest.LeaveType, leaveRequest.TotalDays);
            employee.ModifiedUtc = DateTime.UtcNow;
            _employeeRepository.Update(employee);
        }

        _leaveRequestRepository.Update(leaveRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    private static void ApplyLeaveBalance(Employee employee, LeaveType leaveType, decimal totalDays)
    {
        switch (leaveType)
        {
            case LeaveType.Annual:
                EnsureSufficientBalance(employee.AnnualLeaveBalance, totalDays, "annual");
                employee.AnnualLeaveBalance -= totalDays;
                break;
            case LeaveType.Sick:
                EnsureSufficientBalance(employee.SickLeaveBalance, totalDays, "sick");
                employee.SickLeaveBalance -= totalDays;
                break;
            case LeaveType.Casual:
                EnsureSufficientBalance(employee.CasualLeaveBalance, totalDays, "casual");
                employee.CasualLeaveBalance -= totalDays;
                break;
            case LeaveType.Unpaid:
                break;
        }
    }

    private static void EnsureSufficientBalance(decimal currentBalance, decimal required, string leaveType)
    {
        if (currentBalance < required)
        {
            throw new AppException($"Insufficient {leaveType} leave balance.");
        }
    }
}
