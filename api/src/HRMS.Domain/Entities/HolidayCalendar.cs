using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class HolidayCalendar : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<HolidayDate> Holidays { get; set; } = new List<HolidayDate>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
