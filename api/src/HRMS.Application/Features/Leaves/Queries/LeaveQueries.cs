using AutoMapper;
using HRMS.Application.Common.Constants;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Application.DTOs;
using HRMS.Domain.Enums;
using MediatR;

namespace HRMS.Application.Features.Leaves.Queries;

public record GetLeaveRequestsQuery(Guid? EmployeeId = null, LeaveStatus? Status = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<PagedResult<LeaveRequestDto>>;

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, PagedResult<LeaveRequestDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetLeaveRequestsQueryHandler(
        IEmployeeRepository employeeRepository,
        ILeaveRequestRepository leaveRequestRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedResult<LeaveRequestDto>> Handle(GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId;

        if (_currentUserService.IsInRole(ApplicationRoles.Employee))
        {
            if (_currentUserService.UserId is null)
            {
                throw new AppException("User context is unavailable.", 401);
            }

            var employee = await _employeeRepository.GetByUserIdAsync(_currentUserService.UserId.Value, cancellationToken)
                ?? throw new AppException("Employee profile not found.", 404);

            employeeId = employee.Id;
        }

        var result = await _leaveRequestRepository.GetPagedAsync(
            new LeaveListFilter(employeeId, request.Status, request.PageNumber, request.PageSize),
            cancellationToken);

        return new PagedResult<LeaveRequestDto>
        {
            Items = _mapper.Map<IReadOnlyCollection<LeaveRequestDto>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
