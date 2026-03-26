using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Constants;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using MediatR;
using System.Net;

namespace HRMS.Application.Features.Employees.Commands;

public record CreateEmployeeCommand(
    Guid DepartmentId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string JobTitle,
    DateOnly DateOfBirth,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    string? PhoneNumber,
    string Role = ApplicationRoles.Employee) : IRequest<EmployeeDto>;

public record UpdateEmployeeCommand(
    Guid EmployeeId,
    Guid DepartmentId,
    string FirstName,
    string LastName,
    string JobTitle,
    DateOnly DateOfBirth,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    string? PhoneNumber,
    bool IsActive) : IRequest<EmployeeDto>;

public record DeleteEmployeeCommand(Guid EmployeeId) : IRequest;
public record UploadEmployeeProfileImageCommand(Guid EmployeeId, Stream FileStream, string FileName, string? ContentType) : IRequest<string>;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Role).Must(ApplicationRoles.All.Contains).WithMessage("Invalid role supplied.");
    }
}

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(128);
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    private readonly IIdentityService _identityService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEmployeeCommandHandler(
        IIdentityService identityService,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _identityService = identityService;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new AppException("Department not found.", (int)HttpStatusCode.NotFound);

        if (await _employeeRepository.ExistsByEmployeeCodeAsync(request.EmployeeCode, cancellationToken))
        {
            throw new AppException("Employee code already exists.");
        }

        if (await _employeeRepository.ExistsByWorkEmailAsync(request.Email, cancellationToken))
        {
            throw new AppException("Email already exists.");
        }

        var identityResult = await _identityService.RegisterAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.Role,
            cancellationToken);

        var employee = new Employee
        {
            UserId = identityResult.UserId,
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

        return _mapper.Map<EmployeeDto>(employee);
    }
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateEmployeeCommandHandler(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EmployeeDto> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", (int)HttpStatusCode.NotFound);

        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new AppException("Department not found.", (int)HttpStatusCode.NotFound);

        employee.DepartmentId = department.Id;
        employee.Department = department;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.JobTitle = request.JobTitle.Trim();
        employee.DateOfBirth = request.DateOfBirth;
        employee.JoinDate = request.JoinDate;
        employee.EmploymentType = request.EmploymentType;
        employee.PhoneNumber = request.PhoneNumber?.Trim();
        employee.IsActive = request.IsActive;
        employee.ModifiedUtc = DateTime.UtcNow;

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EmployeeDto>(employee);
    }
}

public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmployeeCommandHandler(IEmployeeRepository employeeRepository, IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", (int)HttpStatusCode.NotFound);

        employee.IsActive = false;
        employee.ModifiedUtc = DateTime.UtcNow;
        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class UploadEmployeeProfileImageCommandHandler : IRequestHandler<UploadEmployeeProfileImageCommand, string>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadEmployeeProfileImageCommandHandler(
        IEmployeeRepository employeeRepository,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadEmployeeProfileImageCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", (int)HttpStatusCode.NotFound);

        if (_currentUserService.IsInRole(ApplicationRoles.Employee) && _currentUserService.UserId != employee.UserId)
        {
            throw new AppException("You can only upload your own profile image.", 403);
        }

        var imageUrl = await _fileStorageService.SaveEmployeeProfileImageAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
        employee.ProfileImageUrl = imageUrl;
        employee.ModifiedUtc = DateTime.UtcNow;

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}
