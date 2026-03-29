using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class HolidayDate : BaseAuditableEntity
{
    public Guid HolidayCalendarId { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOptional { get; set; }

    public HolidayCalendar? HolidayCalendar { get; set; }
}
