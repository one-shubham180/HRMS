using HRMS.Domain.Services;

namespace HRMS.Application.Tests.Domain;

public class PayrollCalculatorTests
{
    [Fact]
    public void CalculateNetSalary_ShouldProrateSalaryAndSubtractDeductions()
    {
        var result = PayrollCalculator.CalculateNetSalary(54000m, 4000m, 27.5m, 30);

        Assert.Equal(45500m, result);
    }
}
