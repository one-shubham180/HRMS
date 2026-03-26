using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class AttendanceRecord : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public AttendanceStatus Status { get; set; }
    public decimal WorkedHours { get; set; }
    public string? Notes { get; set; }

    public Employee? Employee { get; set; }
}
