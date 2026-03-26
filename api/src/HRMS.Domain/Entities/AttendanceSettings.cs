using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class AttendanceSettings : BaseAuditableEntity
{
    public bool RequireGeoTaggedPhotoForAttendance { get; set; }
}
