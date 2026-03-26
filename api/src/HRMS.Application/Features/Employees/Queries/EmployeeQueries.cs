using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Application.DTOs;
using MediatR;
using System.Net;

namespace HRMS.Application.Features.Employees.Queries;

public record GetEmployeesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? DepartmentId = null,
    string? SortBy = null,
    bool Descending = false) : IRequest<PagedResult<EmployeeDto>>;

public record GetEmployeeByIdQuery(Guid EmployeeId) : IRequest<EmployeeDto>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;

    public GetEmployeesQueryHandler(IEmployeeRepository employeeRepository, IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var result = await _employeeRepository.GetPagedAsync(
            new EmployeeListFilter(request.PageNumber, request.PageSize, request.Search, request.DepartmentId, request.SortBy, request.Descending),
            cancellationToken);

        return new PagedResult<EmployeeDto>
        {
            Items = _mapper.Map<IReadOnlyCollection<EmployeeDto>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}

public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;

    public GetEmployeeByIdQueryHandler(IEmployeeRepository employeeRepository, IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _mapper = mapper;
    }

    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", (int)HttpStatusCode.NotFound);

        return _mapper.Map<EmployeeDto>(employee);
    }
}
