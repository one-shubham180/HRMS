using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class PerformanceAppraisal : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? InitializedFromCandidateId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public AppraisalStatus Status { get; set; } = AppraisalStatus.Initialized;
    public string? GoalsSummary { get; set; }

    public Employee? Employee { get; set; }
    public Candidate? InitializedFromCandidate { get; set; }
}
