using HRMS.Domain.Enums;

namespace HRMS.Domain.Services;

public static class AttendancePolicy
{
    private static readonly TimeSpan GraceTime = new(9, 45, 0);
    private static readonly TimeSpan HalfDayThreshold = new(13, 0, 0);
    private const decimal StandardWorkingHours = 8m;

    public static AttendanceStatus ResolveStatus(DateTime checkInLocalTime)
    {
        var time = checkInLocalTime.TimeOfDay;

        if (time >= HalfDayThreshold)
        {
            return AttendanceStatus.HalfDay;
        }

        return time > GraceTime ? AttendanceStatus.Late : AttendanceStatus.Present;
    }

    public static decimal CalculateWorkedHours(DateTime checkInUtc, DateTime checkOutUtc)
    {
        var hours = (decimal)(checkOutUtc - checkInUtc).TotalHours;
        return Math.Round(Math.Max(hours, 0), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateLossOfPayFactor(AttendanceStatus status, decimal workedHours)
    {
        if (status == AttendanceStatus.HalfDay || workedHours < StandardWorkingHours / 2)
        {
            return 0.5m;
        }

        return 0m;
    }
}
