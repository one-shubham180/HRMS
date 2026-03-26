namespace HRMS.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresUtc,
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    Guid? EmployeeId);
