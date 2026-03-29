using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class AuditTrailEntry : BaseAuditableEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? NotificationItemId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldState { get; set; }
    public string? NewState { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;

    public NotificationItem? NotificationItem { get; set; }
}
