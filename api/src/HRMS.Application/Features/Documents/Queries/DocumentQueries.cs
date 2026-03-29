using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Documents.Queries;

public record GetEmployeeDocumentsQuery(Guid EmployeeId) : IRequest<IReadOnlyCollection<EmployeeDocumentDto>>;

public class GetEmployeeDocumentsQueryHandler : IRequestHandler<GetEmployeeDocumentsQuery, IReadOnlyCollection<EmployeeDocumentDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeDocumentRepository _employeeDocumentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetEmployeeDocumentsQueryHandler(
        IEmployeeRepository employeeRepository,
        IEmployeeDocumentRepository employeeDocumentRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _employeeDocumentRepository = employeeDocumentRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<EmployeeDocumentDto>> Handle(GetEmployeeDocumentsQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", 404);

        if (_currentUserService.IsInRole("Employee") && _currentUserService.UserId != employee.UserId)
        {
            throw new AppException("You can only view your own document vault.", 403);
        }

        var documents = await _employeeDocumentRepository.GetByEmployeeAsync(request.EmployeeId, cancellationToken);
        return documents.Select(x => _mapper.Map<EmployeeDocumentDto>(x)).ToArray();
    }
}
