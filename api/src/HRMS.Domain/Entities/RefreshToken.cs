using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class RefreshToken : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedUtc { get; set; }
}
