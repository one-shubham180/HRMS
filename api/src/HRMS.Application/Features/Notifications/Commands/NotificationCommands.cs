using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using MediatR;

namespace HRMS.Application.Features.Notifications.Commands;

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest;
public record MarkAllNotificationsAsReadCommand() : IRequest<int>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IAuditTrailService auditTrailService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _auditTrailService = auditTrailService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new AppException("Notification not found.", 404);

        if (notification.RecipientUserId != _currentUserService.UserId.Value)
        {
            throw new AppException("You are not allowed to update this notification.", 403);
        }

        if (notification.Status == Domain.Enums.NotificationStatus.Read)
        {
            return Unit.Value;
        }

        var previousStatus = notification.Status;
        notification.Status = Domain.Enums.NotificationStatus.Read;
        notification.ReadUtc = DateTime.UtcNow;
        notification.ModifiedUtc = DateTime.UtcNow;
        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditTrailService.WriteAsync(
            _currentUserService.UserId,
            nameof(Domain.Entities.NotificationItem),
            notification.Id,
            "NotificationRead",
            previousStatus.ToString(),
            notification.Status.ToString(),
            notification.Title,
            notification.Id,
            cancellationToken);

        return Unit.Value;
    }
}

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditTrailService _auditTrailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationRepository notificationRepository,
        IAuditTrailService auditTrailService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _auditTrailService = auditTrailService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new AppException("User context is unavailable.", 401);
        }

        var notifications = await _notificationRepository.GetByRecipientAsync(_currentUserService.UserId.Value, cancellationToken);
        var unreadNotifications = notifications
            .Where(x => x.Status != Domain.Enums.NotificationStatus.Read)
            .ToArray();

        foreach (var notification in unreadNotifications)
        {
            notification.Status = Domain.Enums.NotificationStatus.Read;
            notification.ReadUtc = DateTime.UtcNow;
            notification.ModifiedUtc = DateTime.UtcNow;
            _notificationRepository.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (unreadNotifications.Length > 0)
        {
            await _auditTrailService.WriteAsync(
                _currentUserService.UserId,
                nameof(Domain.Entities.NotificationItem),
                null,
                "NotificationsReadInBulk",
                "Mixed",
                "Read",
                $"{unreadNotifications.Length} notifications marked as read",
                null,
                cancellationToken);
        }

        return unreadNotifications.Length;
    }
}
