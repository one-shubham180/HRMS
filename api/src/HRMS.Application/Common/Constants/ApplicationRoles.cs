namespace HRMS.Application.Common.Constants;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string HR = "HR";
    public const string Employee = "Employee";

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, HR, Employee };
}
