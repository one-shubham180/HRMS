using HRMS.Application.Common.Models;

namespace HRMS.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password, string role, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
}
