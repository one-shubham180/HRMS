using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class Candidate : BaseAuditableEntity
{
    public Guid DepartmentId { get; set; }
    public Guid? ConvertedEmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public CandidateStatus Status { get; set; } = CandidateStatus.Applied;
    public DateOnly? HiredDate { get; set; }
    public string? Notes { get; set; }

    public Department? Department { get; set; }
    public Employee? ConvertedEmployee { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
