using HRMS.Application.Common.Constants;
using AutoMapper;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Payroll.Queries;

public record GetPayrollRecordsQuery(
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    int? Year = null,
    int? Month = null,
    int PageNumber = 1,
    int PageSize = 10)
    : IRequest<PagedResult<PayrollRecordDto>>;
public record GetSalaryStructureQuery(Guid EmployeeId) : IRequest<SalaryStructureDto?>;
public record ExportPayrollRecordsQuery(int Year, int Month, Guid? DepartmentId = null, Guid? EmployeeId = null)
    : IRequest<IReadOnlyCollection<PayrollRecordDto>>;

public class GetPayrollRecordsQueryHandler : IRequestHandler<GetPayrollRecordsQuery, PagedResult<PayrollRecordDto>>
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetPayrollRecordsQueryHandler(
        IPayrollRepository payrollRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResult<PayrollRecordDto>> Handle(GetPayrollRecordsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;
        var departmentId = request.DepartmentId;

        if (_currentUserService.IsInRole(ApplicationRoles.Employee) && _currentUserService.UserId.HasValue)
        {
            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken);
            employeeId = employee?.Id;
            departmentId = null;
        }

        var result = await _payrollRepository.GetPagedAsync(
            new PayrollListFilter(employeeId, departmentId, request.Year, request.Month, request.PageNumber, request.PageSize),
            cancellationToken);

        return new PagedResult<PayrollRecordDto>
        {
            Items = _mapper.Map<IReadOnlyCollection<PayrollRecordDto>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}

public class ExportPayrollRecordsQueryHandler : IRequestHandler<ExportPayrollRecordsQuery, IReadOnlyCollection<PayrollRecordDto>>
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ExportPayrollRecordsQueryHandler(
        IPayrollRepository payrollRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _payrollRepository = payrollRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<PayrollRecordDto>> Handle(ExportPayrollRecordsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;
        var departmentId = request.DepartmentId;

        if (_currentUserService.IsInRole(ApplicationRoles.Employee) && _currentUserService.UserId.HasValue)
        {
            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken);
            employeeId = employee?.Id;
            departmentId = null;
        }

        var records = await _payrollRepository.GetFilteredAsync(
            new PayrollListFilter(employeeId, departmentId, request.Year, request.Month),
            cancellationToken);

        return _mapper.Map<IReadOnlyCollection<PayrollRecordDto>>(records);
    }
}

public class GetSalaryStructureQueryHandler : IRequestHandler<GetSalaryStructureQuery, SalaryStructureDto?>
{
    private readonly ISalaryStructureRepository _salaryStructureRepository;
    private readonly IMapper _mapper;

    public GetSalaryStructureQueryHandler(ISalaryStructureRepository salaryStructureRepository, IMapper mapper)
    {
        _salaryStructureRepository = salaryStructureRepository;
        _mapper = mapper;
    }

    public async Task<SalaryStructureDto?> Handle(GetSalaryStructureQuery request, CancellationToken cancellationToken)
    {
        var salaryStructure = await _salaryStructureRepository.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken);

        return salaryStructure is null ? null : _mapper.Map<SalaryStructureDto>(salaryStructure);
    }
}
