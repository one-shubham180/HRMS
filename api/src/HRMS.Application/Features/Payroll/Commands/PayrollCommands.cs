using AutoMapper;
using FluentValidation;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Services;
using MediatR;

namespace HRMS.Application.Features.Payroll.Commands;

public record UpsertSalaryStructureCommand(
    Guid EmployeeId,
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal ConveyanceAllowance,
    decimal MedicalAllowance,
    decimal OtherAllowance,
    decimal ProvidentFundDeduction,
    decimal TaxDeduction) : IRequest<SalaryStructureDto>;

public record GenerateMonthlyPayrollCommand(Guid EmployeeId, int Year, int Month) : IRequest<PayrollRecordDto>;

public class UpsertSalaryStructureCommandValidator : AbstractValidator<UpsertSalaryStructureCommand>
{
    public UpsertSalaryStructureCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BasicSalary).GreaterThan(0);
        RuleFor(x => x.HouseRentAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConveyanceAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MedicalAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OtherAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProvidentFundDeduction).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxDeduction).GreaterThanOrEqualTo(0);
    }
}

public class GenerateMonthlyPayrollCommandValidator : AbstractValidator<GenerateMonthlyPayrollCommand>
{
    public GenerateMonthlyPayrollCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class UpsertSalaryStructureCommandHandler : IRequestHandler<UpsertSalaryStructureCommand, SalaryStructureDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISalaryStructureRepository _salaryStructureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpsertSalaryStructureCommandHandler(
        IEmployeeRepository employeeRepository,
        ISalaryStructureRepository salaryStructureRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _salaryStructureRepository = salaryStructureRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SalaryStructureDto> Handle(UpsertSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", 404);

        var salaryStructure = await _salaryStructureRepository.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        if (salaryStructure is null)
        {
            salaryStructure = new SalaryStructure
            {
                EmployeeId = employee.Id
            };

            await _salaryStructureRepository.AddAsync(salaryStructure, cancellationToken);
        }

        salaryStructure.BasicSalary = request.BasicSalary;
        salaryStructure.HouseRentAllowance = request.HouseRentAllowance;
        salaryStructure.ConveyanceAllowance = request.ConveyanceAllowance;
        salaryStructure.MedicalAllowance = request.MedicalAllowance;
        salaryStructure.OtherAllowance = request.OtherAllowance;
        salaryStructure.ProvidentFundDeduction = request.ProvidentFundDeduction;
        salaryStructure.TaxDeduction = request.TaxDeduction;
        salaryStructure.ModifiedUtc = DateTime.UtcNow;
        salaryStructure.Employee = employee;

        _salaryStructureRepository.Update(salaryStructure);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SalaryStructureDto>(salaryStructure);
    }
}

public class GenerateMonthlyPayrollCommandHandler : IRequestHandler<GenerateMonthlyPayrollCommand, PayrollRecordDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ISalaryStructureRepository _salaryStructureRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GenerateMonthlyPayrollCommandHandler(
        IEmployeeRepository employeeRepository,
        ISalaryStructureRepository salaryStructureRepository,
        IAttendanceRepository attendanceRepository,
        ILeaveRequestRepository leaveRequestRepository,
        IPayrollRepository payrollRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _salaryStructureRepository = salaryStructureRepository;
        _attendanceRepository = attendanceRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _payrollRepository = payrollRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PayrollRecordDto> Handle(GenerateMonthlyPayrollCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Employee not found.", 404);

        var salaryStructure = await _salaryStructureRepository.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new AppException("Salary structure does not exist for the employee.");

        var lossOfPayFromAttendance = await _attendanceRepository.GetLossOfPayDaysAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);
        var unpaidLeaveDays = await _leaveRequestRepository.GetApprovedUnpaidLeaveDaysAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);

        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var totalLossOfPayDays = lossOfPayFromAttendance + unpaidLeaveDays;
        var payableDays = Math.Max(daysInMonth - totalLossOfPayDays, 0);
        var deductions = salaryStructure.TotalDeductions;
        var netSalary = PayrollCalculator.CalculateNetSalary(salaryStructure.GrossSalary, deductions, payableDays, daysInMonth);

        var payroll = await _payrollRepository.GetByEmployeeAndPeriodAsync(request.EmployeeId, request.Year, request.Month, cancellationToken);
        if (payroll is null)
        {
            payroll = new PayrollRecord
            {
                EmployeeId = employee.Id,
                Year = request.Year,
                Month = request.Month,
                PayslipNumber = $"PS-{request.Year}{request.Month:D2}-{employee.EmployeeCode}"
            };

            await _payrollRepository.AddAsync(payroll, cancellationToken);
        }

        payroll.PayableDays = payableDays;
        payroll.LossOfPayDays = totalLossOfPayDays;
        payroll.GrossSalary = salaryStructure.GrossSalary;
        payroll.TotalDeductions = deductions;
        payroll.NetSalary = netSalary;
        payroll.GeneratedUtc = DateTime.UtcNow;
        payroll.Employee = employee;
        payroll.ModifiedUtc = DateTime.UtcNow;

        _payrollRepository.Update(payroll);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PayrollRecordDto>(payroll);
    }
}
