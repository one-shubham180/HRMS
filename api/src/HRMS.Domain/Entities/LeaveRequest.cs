using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class LeaveRequest : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedUtc { get; set; }
    public string? ReviewRemarks { get; set; }

    public Employee? Employee { get; set; }
}
