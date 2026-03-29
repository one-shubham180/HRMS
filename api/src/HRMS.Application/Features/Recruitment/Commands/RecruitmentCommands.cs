using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Constants;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using MediatR;

namespace HRMS.Application.Features.Recruitment.Commands;

public record CreateCandidateCommand(
    Guid DepartmentId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string JobTitle,
    string? Notes) : IRequest<CandidateDto>;

public record UpdateCandidateStatusCommand(
    Guid CandidateId,
    CandidateStatus Status,
    string? Notes,
    string? EmployeeCode,
    DateOnly? JoinDate,
    EmploymentType? EmploymentType) : IRequest<CandidateDto>;

public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateCandidateStatusCommandValidator : AbstractValidator<UpdateCandidateStatusCommand>
{
    public UpdateCandidateStatusCommandValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty();
        When(x => x.Status == CandidateStatus.Hired, () =>
        {
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(32);
            RuleFor(x => x.JoinDate).NotNull();
            RuleFor(x => x.EmploymentType).NotNull();
        });
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateCandidateCommandHandler : IRequestHandler<CreateCandidateCommand, CandidateDto>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCandidateCommandHandler(
        IDepartmentRepository departmentRepository,
        ICandidateRepository candidateRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _candidateRepository = candidateRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CandidateDto> Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new AppException("Department not found.", 404);

        if (await _candidateRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new AppException("Candidate email already exists.");
        }

        var candidate = new Candidate
        {
            DepartmentId = department.Id,
            Department = department,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            JobTitle = request.JobTitle.Trim(),
            Notes = request.Notes?.Trim()
        };

        await _candidateRepository.AddAsync(candidate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CandidateDto>(candidate);
    }
}

public class UpdateCandidateStatusCommandHandler : IRequestHandler<UpdateCandidateStatusCommand, CandidateDto>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPerformanceAppraisalRepository _performanceAppraisalRepository;
    private readonly IIdentityService _identityService;
    private readonly IOnboardingService _onboardingService;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCandidateStatusCommandHandler(
        ICandidateRepository candidateRepository,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IPerformanceAppraisalRepository performanceAppraisalRepository,
        IIdentityService identityService,
        IOnboardingService onboardingService,
        IAuditTrailService auditTrailService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _candidateRepository = candidateRepository;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _performanceAppraisalRepository = performanceAppraisalRepository;
        _identityService = identityService;
        _onboardingService = onboardingService;
        _auditTrailService = auditTrailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CandidateDto> Handle(UpdateCandidateStatusCommand request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId, cancellationToken)
            ?? throw new AppException("Candidate not found.", 404);

        var previousStatus = candidate.Status;
        candidate.Status = request.Status;
        candidate.Notes = request.Notes?.Trim() ?? candidate.Notes;
        candidate.ModifiedUtc = DateTime.UtcNow;

        if (request.Status == CandidateStatus.Hired)
        {
            if (candidate.ConvertedEmployeeId.HasValue)
            {
                throw new AppException("Candidate has already been converted to an employee.");
            }

            if (await _employeeRepository.ExistsByEmployeeCodeAsync(request.EmployeeCode!, cancellationToken))
            {
                throw new AppException("Employee code already exists.");
            }

            if (await _employeeRepository.ExistsByWorkEmailAsync(candidate.Email, cancellationToken))
            {
                throw new AppException("Employee email already exists.");
            }

            var department = await _departmentRepository.GetByIdAsync(candidate.DepartmentId, cancellationToken)
                ?? throw new AppException("Department not found.", 404);

            var identity = await _identityService.RegisterAsync(
                candidate.FirstName,
                candidate.LastName,
                candidate.Email,
                GenerateTemporaryPassword(),
                ApplicationRoles.Employee,
                cancellationToken);

            var employee = new Employee
            {
                UserId = identity.UserId,
                DepartmentId = department.Id,
                Department = department,
                EmployeeCode = request.EmployeeCode!.Trim(),
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                WorkEmail = candidate.Email,
                PhoneNumber = candidate.PhoneNumber,
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                JoinDate = request.JoinDate!.Value,
                JobTitle = candidate.JobTitle,
                EmploymentType = request.EmploymentType!.Value,
                SourceCandidateId = candidate.Id
            };

            await _employeeRepository.AddAsync(employee, cancellationToken);

            candidate.ConvertedEmployeeId = employee.Id;
            candidate.HiredDate = request.JoinDate.Value;

            var appraisal = new PerformanceAppraisal
            {
                EmployeeId = employee.Id,
                Employee = employee,
                InitializedFromCandidateId = candidate.Id,
                CycleName = $"{request.JoinDate.Value.Year} Initial Cycle",
                StartDate = request.JoinDate.Value,
                EndDate = request.JoinDate.Value.AddMonths(3),
                Status = AppraisalStatus.Initialized,
                GoalsSummary = "Initial goals to be finalized with reporting manager."
            };

            await _performanceAppraisalRepository.AddAsync(appraisal, cancellationToken);
            await _onboardingService.SendWelcomeEmailAsync(employee, cancellationToken);
            await _auditTrailService.WriteAsync(
                null,
                nameof(Candidate),
                candidate.Id,
                "CandidateConvertedToEmployee",
                previousStatus.ToString(),
                request.Status.ToString(),
                employee.EmployeeCode,
                null,
                cancellationToken);
        }

        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CandidateDto>(candidate);
    }

    private static string GenerateTemporaryPassword() => $"Temp!{Guid.NewGuid():N}A1";
}
