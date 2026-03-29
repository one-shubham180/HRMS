using AutoMapper;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Notifications.Queries;

public record GetMyNotificationsQuery() : IRequest<IReadOnlyCollection<NotificationDto>>;
public record GetRecentAuditTrailQuery(int Take = 100) : IRequest<IReadOnlyCollection<AuditTrailDto>>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, IReadOnlyCollection<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var notifications = await _notificationRepository.GetByRecipientAsync(_currentUserService.UserId.Value, cancellationToken);
        return notifications.Select(x => _mapper.Map<NotificationDto>(x)).ToArray();
    }
}

public class GetRecentAuditTrailQueryHandler : IRequestHandler<GetRecentAuditTrailQuery, IReadOnlyCollection<AuditTrailDto>>
{
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly IMapper _mapper;

    public GetRecentAuditTrailQueryHandler(IAuditTrailRepository auditTrailRepository, IMapper mapper)
    {
        _auditTrailRepository = auditTrailRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<AuditTrailDto>> Handle(GetRecentAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var entries = await _auditTrailRepository.GetRecentAsync(request.Take, cancellationToken);
        return entries.Select(x => _mapper.Map<AuditTrailDto>(x)).ToArray();
    }
}
