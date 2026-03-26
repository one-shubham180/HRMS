using HRMS.Application.Common.Constants;
using AutoMapper;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Payroll.Queries;

public record GetPayrollRecordsQuery(Guid? EmployeeId = null, int? Year = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<PagedResult<PayrollRecordDto>>;

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

        if (_currentUserService.IsInRole(ApplicationRoles.Employee) && _currentUserService.UserId.HasValue)
        {
            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken);
            employeeId = employee?.Id;
        }

        var result = await _payrollRepository.GetPagedAsync(
            new PayrollListFilter(employeeId, request.Year, request.PageNumber, request.PageSize),
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
