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
    }
}
