using AutoMapper;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;

namespace HRMS.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Department, DepartmentDto>()
            .ConstructUsing(source => new DepartmentDto(
                source.Id,
                source.Name,
                source.Code,
                source.Description));

        CreateMap<Employee, EmployeeDto>()
            .ConstructUsing(source => new EmployeeDto(
                source.Id,
                source.UserId,
                source.DepartmentId,
                source.Department != null ? source.Department.Name : string.Empty,
                source.EmployeeCode,
                source.FirstName,
                source.LastName,
                source.FullName,
                source.WorkEmail,
                source.PhoneNumber,
                source.DateOfBirth,
                source.JoinDate,
                source.JobTitle,
                source.EmploymentType,
                source.AnnualLeaveBalance,
                source.SickLeaveBalance,
                source.CasualLeaveBalance,
                source.ProfileImageUrl,
                source.IsActive,
                source.SalaryStructure != null ? source.SalaryStructure.GrossSalary : 0m));

        CreateMap<AttendanceRecord, AttendanceRecordDto>()
            .ConstructUsing(source => new AttendanceRecordDto(
                source.Id,
                source.EmployeeId,
                source.Employee != null ? source.Employee.FullName : string.Empty,
                source.RosterAssignmentId,
                source.WorkDate,
                source.CheckInUtc,
                source.CheckInCapturedPhotoUtc,
                source.CheckInPhotoUrl,
                source.CheckInLatitude,
                source.CheckInLongitude,
                source.CheckInLocationLabel,
                source.CheckOutUtc,
                source.CheckOutCapturedPhotoUtc,
                source.CheckOutPhotoUrl,
                source.CheckOutLatitude,
                source.CheckOutLongitude,
                source.CheckOutLocationLabel,
                source.Status,
                source.WorkedHours,
                source.ScheduledShiftName,
                source.ScheduledStartTimeLocal,
                source.ScheduledEndTimeLocal,
                source.ScheduledHours,
                source.OvertimeHours,
                source.IsHoliday,
                source.HolidayName,
                source.IsRestDay,
                source.Notes));

        CreateMap<ShiftDefinition, ShiftDefinitionDto>()
            .ConstructUsing(source => new ShiftDefinitionDto(
                source.Id,
                source.Name,
                source.Code,
                source.StartTimeLocal,
                source.EndTimeLocal,
                source.StandardHours,
                source.BreakMinutes,
                source.MinimumOvertimeMinutes));

        CreateMap<HolidayDate, HolidayDateDto>()
            .ConstructUsing(source => new HolidayDateDto(
                source.Id,
                source.Date,
                source.Name,
                source.IsOptional));

        CreateMap<HolidayCalendar, HolidayCalendarDto>()
            .ConstructUsing(source => new HolidayCalendarDto(
                source.Id,
                source.Name,
                source.Code,
                source.IsDefault,
                source.Holidays
                    .OrderBy(x => x.Date)
                    .Select(x => new HolidayDateDto(x.Id, x.Date, x.Name, x.IsOptional))
                    .ToArray()));

        CreateMap<RosterAssignment, RosterAssignmentDto>()
            .ConstructUsing(source => new RosterAssignmentDto(
                source.Id,
                source.EmployeeId,
                source.Employee != null ? source.Employee.FullName : string.Empty,
                source.ShiftDefinitionId,
                source.ShiftDefinition != null ? source.ShiftDefinition.Name : string.Empty,
                source.ShiftDefinition != null ? source.ShiftDefinition.StartTimeLocal : null,
                source.ShiftDefinition != null ? source.ShiftDefinition.EndTimeLocal : null,
                source.ShiftDefinition != null ? source.ShiftDefinition.StandardHours : 0m,
                source.ShiftDefinition != null ? source.ShiftDefinition.BreakMinutes : 0,
                source.WorkDate,
                source.IsRestDay,
                source.Notes));

        CreateMap<LeaveRequest, LeaveRequestDto>()
            .ConstructUsing(source => new LeaveRequestDto(
                source.Id,
                source.EmployeeId,
                source.Employee != null ? source.Employee.FullName : string.Empty,
                source.LeaveType,
                source.Status,
                source.StartDate,
                source.EndDate,
                source.TotalDays,
                source.Reason,
                source.ReviewRemarks));

        CreateMap<SalaryStructure, SalaryStructureDto>()
            .ConstructUsing(source => new SalaryStructureDto(
                source.Id,
                source.EmployeeId,
                source.BasicSalary,
                source.HouseRentAllowance,
                source.ConveyanceAllowance,
                source.MedicalAllowance,
                source.OtherAllowance,
                source.ProvidentFundDeduction,
                source.TaxDeduction,
                source.GrossSalary,
                source.TotalDeductions));

        CreateMap<PayrollRecord, PayrollRecordDto>()
            .ConstructUsing(source => new PayrollRecordDto(
                source.Id,
                source.EmployeeId,
                source.Employee != null ? source.Employee.FullName : string.Empty,
                source.Year,
                source.Month,
                source.PayableDays,
                source.LossOfPayDays,
                source.GrossSalary,
                source.TotalDeductions,
                source.NetSalary,
                source.PayslipNumber,
                source.GeneratedUtc));

        CreateMap<NotificationItem, NotificationDto>()
            .ConstructUsing(source => new NotificationDto(
                source.Id,
                source.RecipientUserId,
                source.TriggeredByUserId,
                source.Type,
                source.Status,
                source.Title,
                source.Message,
                source.RelatedEntityType,
                source.RelatedEntityId,
                source.DeliveredUtc,
                source.ReadUtc));

        CreateMap<AuditTrailEntry, AuditTrailDto>()
            .ConstructUsing(source => new AuditTrailDto(
                source.Id,
                source.ActorUserId,
                source.NotificationItemId,
                source.EntityType,
                source.EntityId,
                source.Action,
                source.OldState,
                source.NewState,
                source.Metadata,
                source.OccurredUtc));

        CreateMap<EmployeeDocument, EmployeeDocumentDto>()
            .ConstructUsing(source => new EmployeeDocumentDto(
                source.Id,
                source.EmployeeId,
                source.PayrollRecordId,
                source.Category,
                source.FileName,
                source.StoragePath,
                source.ContentType,
                source.FileSize,
                source.IsSystemGenerated,
                source.UploadedByUserId,
                source.CreatedUtc));

        CreateMap<Candidate, CandidateDto>()
            .ConstructUsing(source => new CandidateDto(
                source.Id,
                source.DepartmentId,
                source.Department != null ? source.Department.Name : string.Empty,
                source.ConvertedEmployeeId,
                source.FirstName,
                source.LastName,
                source.FullName,
                source.Email,
                source.PhoneNumber,
                source.JobTitle,
                source.Status,
                source.HiredDate,
                source.Notes));

        CreateMap<PerformanceAppraisal, PerformanceAppraisalDto>()
            .ConstructUsing(source => new PerformanceAppraisalDto(
                source.Id,
                source.EmployeeId,
                source.Employee != null ? source.Employee.FullName : string.Empty,
                source.InitializedFromCandidateId,
                source.CycleName,
                source.StartDate,
                source.EndDate,
                source.Status,
                source.GoalsSummary));
    }
}
