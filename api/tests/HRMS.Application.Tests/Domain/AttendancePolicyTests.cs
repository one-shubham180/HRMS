using HRMS.Domain.Enums;
using HRMS.Domain.Services;

namespace HRMS.Application.Tests.Domain;

public class AttendancePolicyTests
{
    [Fact]
    public void ResolveStatus_ShouldReturnLate_WhenCheckInAfterGraceTime()
    {
        var checkIn = new DateTime(2026, 3, 25, 10, 5, 0);

        var result = AttendancePolicy.ResolveStatus(checkIn);

        Assert.Equal(AttendanceStatus.Late, result);
    }

    [Fact]
    public void CalculateLossOfPayFactor_ShouldReturnHalfDay_WhenHalfDayStatus()
    {
        var result = AttendancePolicy.CalculateLossOfPayFactor(AttendanceStatus.HalfDay, 3.5m);

        Assert.Equal(0.5m, result);
    }
}
