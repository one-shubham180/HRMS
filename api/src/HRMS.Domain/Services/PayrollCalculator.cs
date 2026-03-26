namespace HRMS.Domain.Services;

public static class PayrollCalculator
{
    public static decimal CalculateNetSalary(decimal grossSalary, decimal deductions, decimal payableDays, int daysInMonth)
    {
        if (daysInMonth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysInMonth), "Days in month must be greater than zero.");
        }

        var proratedGross = grossSalary / daysInMonth * payableDays;
        var net = proratedGross - deductions;
        return Math.Round(Math.Max(net, 0), 2, MidpointRounding.AwayFromZero);
    }
}
