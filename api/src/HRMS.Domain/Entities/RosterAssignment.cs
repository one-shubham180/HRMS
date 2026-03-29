using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class RosterAssignment : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid ShiftDefinitionId { get; set; }
    public DateOnly WorkDate { get; set; }
    public bool IsRestDay { get; set; }
    public string? Notes { get; set; }

    public Employee? Employee { get; set; }
    public ShiftDefinition? ShiftDefinition { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
