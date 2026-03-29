using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Recruitment.Queries;

public record GetCandidatesQuery() : IRequest<IReadOnlyCollection<CandidateDto>>;
public record GetPerformanceAppraisalsQuery(Guid? EmployeeId = null) : IRequest<IReadOnlyCollection<PerformanceAppraisalDto>>;

public class GetCandidatesQueryHandler : IRequestHandler<GetCandidatesQuery, IReadOnlyCollection<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IMapper _mapper;

    public GetCandidatesQueryHandler(ICandidateRepository candidateRepository, IMapper mapper)
    {
        _candidateRepository = candidateRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<CandidateDto>> Handle(GetCandidatesQuery request, CancellationToken cancellationToken)
    {
        var candidates = await _candidateRepository.GetAllAsync(cancellationToken);
        return candidates.Select(x => _mapper.Map<CandidateDto>(x)).ToArray();
    }
}

public class GetPerformanceAppraisalsQueryHandler : IRequestHandler<GetPerformanceAppraisalsQuery, IReadOnlyCollection<PerformanceAppraisalDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPerformanceAppraisalRepository _performanceAppraisalRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetPerformanceAppraisalsQueryHandler(
        IEmployeeRepository employeeRepository,
        IPerformanceAppraisalRepository performanceAppraisalRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _performanceAppraisalRepository = performanceAppraisalRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<PerformanceAppraisalDto>> Handle(GetPerformanceAppraisalsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;

        if (!employeeId.HasValue && (_currentUserService.IsInRole("Admin") || _currentUserService.IsInRole("HR")))
        {
            var allAppraisals = await _performanceAppraisalRepository.GetAllAsync(cancellationToken);
            return allAppraisals.Select(x => _mapper.Map<PerformanceAppraisalDto>(x)).ToArray();
        }

        if (!employeeId.HasValue)
        {
            if (_currentUserService.UserId is null)
            {
                throw new AppException("User context is unavailable.", 401);
            }

            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
                ?? throw new AppException("Employee profile not found.", 404);
            employeeId = employee.Id;
        }

        var appraisals = await _performanceAppraisalRepository.GetByEmployeeAsync(employeeId.Value, cancellationToken);
        return appraisals.Select(x => _mapper.Map<PerformanceAppraisalDto>(x)).ToArray();
    }
}
