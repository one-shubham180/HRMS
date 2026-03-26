namespace HRMS.Application.Common.Models;

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresUtc,
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    Guid? EmployeeId);
