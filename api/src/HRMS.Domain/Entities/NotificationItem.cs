using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class NotificationItem : BaseAuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RelatedEntityType { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public DateTime? DeliveredUtc { get; set; }
    public DateTime? ReadUtc { get; set; }
}
