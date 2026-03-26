using AutoMapper;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;

namespace HRMS.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Department, DepartmentDto>();
        CreateMap<Employee, EmployeeDto>()
            .ForMember(d => d.DepartmentName, c => c.MapFrom(s => s.Department != null ? s.Department.Name : string.Empty))
            .ForMember(d => d.GrossSalary, c => c.MapFrom(s => s.SalaryStructure != null ? s.SalaryStructure.GrossSalary : 0m))
            .ForMember(d => d.FullName, c => c.MapFrom(s => s.FullName));
        CreateMap<AttendanceRecord, AttendanceRecordDto>();
        CreateMap<LeaveRequest, LeaveRequestDto>()
            .ForMember(d => d.EmployeeName, c => c.MapFrom(s => s.Employee != null ? s.Employee.FullName : string.Empty));
        CreateMap<SalaryStructure, SalaryStructureDto>();
        CreateMap<PayrollRecord, PayrollRecordDto>()
            .ForMember(d => d.EmployeeName, c => c.MapFrom(s => s.Employee != null ? s.Employee.FullName : string.Empty));
    }
}
