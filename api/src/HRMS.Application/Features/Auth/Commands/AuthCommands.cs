using FluentValidation;
using HRMS.Application.Common.Constants;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using MediatR;
using System.Net;

namespace HRMS.Application.Features.Auth.Commands;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid DepartmentId,
    string EmployeeCode,
    string JobTitle,
    DateOnly DateOfBirth,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    string? PhoneNumber) : IRequest<AuthResponseDto>;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
public record AssignRoleCommand(Guid UserId, string Role) : IRequest;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(128);
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).Must(ApplicationRoles.All.Contains).WithMessage("Invalid role supplied.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new AppException("Department was not found.", (int)HttpStatusCode.NotFound);

        if (await _employeeRepository.ExistsByEmployeeCodeAsync(request.EmployeeCode, cancellationToken))
        {
            throw new AppException("Employee code is already in use.");
        }

        if (await _employeeRepository.ExistsByWorkEmailAsync(request.Email, cancellationToken))
        {
            throw new AppException("Work email is already in use.");
        }

        var authResult = await _identityService.RegisterAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            ApplicationRoles.Employee,
            cancellationToken);

        var employee = new Employee
        {
            UserId = authResult.UserId,
            DepartmentId = department.Id,
            Department = department,
            EmployeeCode = request.EmployeeCode.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            WorkEmail = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth,
            JoinDate = request.JoinDate,
            JobTitle = request.JobTitle.Trim(),
            EmploymentType = request.EmploymentType
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            authResult.AccessToken,
            authResult.RefreshToken,
            authResult.ExpiresUtc,
            authResult.UserId,
            authResult.Email,
            authResult.Roles,
            employee.Id);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginAsync(request.Email, request.Password, cancellationToken);
        return new AuthResponseDto(result.AccessToken, result.RefreshToken, result.ExpiresUtc, result.UserId, result.Email, result.Roles, result.EmployeeId);
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return new AuthResponseDto(result.AccessToken, result.RefreshToken, result.ExpiresUtc, result.UserId, result.Email, result.Roles, result.EmployeeId);
    }
}

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IIdentityService _identityService;

    public AssignRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Unit> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        await _identityService.AssignRoleAsync(request.UserId, request.Role, cancellationToken);
        return Unit.Value;
    }
}
