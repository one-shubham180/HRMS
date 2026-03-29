using HRMS.Domain.Common;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities;

public class EmployeeDocument : BaseAuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? PayrollRecordId { get; set; }
    public DocumentCategory Category { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsSystemGenerated { get; set; }
    public Guid? UploadedByUserId { get; set; }

    public Employee? Employee { get; set; }
    public PayrollRecord? PayrollRecord { get; set; }
}
