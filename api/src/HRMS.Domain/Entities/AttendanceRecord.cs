using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class AttendanceRecord : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? RosterAssignmentId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckInCapturedPhotoUtc { get; set; }
    public string? CheckInPhotoUrl { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public string? CheckInLocationLabel { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public DateTime? CheckOutCapturedPhotoUtc { get; set; }
    public string? CheckOutPhotoUrl { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }
    public string? CheckOutLocationLabel { get; set; }
    public AttendanceStatus Status { get; set; }
    public decimal WorkedHours { get; set; }
    public string? ScheduledShiftName { get; set; }
    public TimeOnly? ScheduledStartTimeLocal { get; set; }
    public TimeOnly? ScheduledEndTimeLocal { get; set; }
    public decimal ScheduledHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public bool IsRestDay { get; set; }
    public string? Notes { get; set; }

    public Employee? Employee { get; set; }
    public RosterAssignment? RosterAssignment { get; set; }
}
