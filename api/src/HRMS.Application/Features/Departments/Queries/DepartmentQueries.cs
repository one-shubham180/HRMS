using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;
using System.Net;

namespace HRMS.Application.Features.Departments.Queries;

public record GetDepartmentsQuery() : IRequest<IReadOnlyCollection<DepartmentDto>>;
public record GetDepartmentByIdQuery(Guid DepartmentId) : IRequest<DepartmentDto>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyCollection<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;

    public GetDepartmentsQueryHandler(IDepartmentRepository departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<DepartmentDto>>(departments);
    }
}

public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IMapper _mapper;

    public GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }

    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new AppException("Department not found.", (int)HttpStatusCode.NotFound);

        return _mapper.Map<DepartmentDto>(department);
    }
}
