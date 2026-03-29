using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class Employee : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? HolidayCalendarId { get; set; }
    public Guid? SourceCandidateId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateOnly JoinDate { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public decimal AnnualLeaveBalance { get; set; } = 18;
    public decimal SickLeaveBalance { get; set; } = 8;
    public decimal CasualLeaveBalance { get; set; } = 6;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedUtc { get; set; }
    public Guid? DeactivatedByUserId { get; set; }

    public Department? Department { get; set; }
    public HolidayCalendar? HolidayCalendar { get; set; }
    public Candidate? SourceCandidate { get; set; }
    public SalaryStructure? SalaryStructure { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    public ICollection<RosterAssignment> RosterAssignments { get; set; } = new List<RosterAssignment>();
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    public ICollection<PerformanceAppraisal> PerformanceAppraisals { get; set; } = new List<PerformanceAppraisal>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
