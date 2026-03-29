using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class ShiftDefinition : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TimeOnly StartTimeLocal { get; set; }
    public TimeOnly EndTimeLocal { get; set; }
    public decimal StandardHours { get; set; }
    public int BreakMinutes { get; set; }
    public int MinimumOvertimeMinutes { get; set; } = 30;

    public ICollection<RosterAssignment> RosterAssignments { get; set; } = new List<RosterAssignment>();
}
