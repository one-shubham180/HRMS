using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Common.Models;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Identity;
using HRMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace HRMS.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly HrmsDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly IUnitOfWork _unitOfWork;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IRefreshTokenRepository refreshTokenRepository,
        JwtTokenService jwtTokenService,
        HrmsDbContext context,
        IOptions<JwtOptions> jwtOptions,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password, string role, CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(role);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            EmailConfirmed = true,
            MustChangePassword = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await _userManager.AddToRoleAsync(user, role);

        return await BuildAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken)
            ?? throw new AppException("Invalid email or password.", 401);

        await EnsureUserIsActiveAsync(user, cancellationToken);

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            throw new AppException("Invalid email or password.", 401);
        }

        return await BuildAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new AppException("Refresh token is invalid.", 401);

        if (storedRefreshToken.IsRevoked || storedRefreshToken.ExpiryUtc <= DateTime.UtcNow)
        {
            throw new AppException("Refresh token has expired.", 401);
        }

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedUtc = DateTime.UtcNow;
        _refreshTokenRepository.Update(storedRefreshToken);

        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == storedRefreshToken.UserId, cancellationToken)
            ?? throw new AppException("User no longer exists.", 404);

        await EnsureUserIsActiveAsync(user, cancellationToken);

        var authResult = await BuildAuthResultAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return authResult;
    }

    public async Task AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(role);

        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, existingRoles);
        }

        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<PasswordSetupResult> GeneratePasswordSetupAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(resetToken);
        var resetLink = $"https://hrms.local/reset-password?userId={user.Id}&token={encodedToken}";

        return new PasswordSetupResult(resetToken, resetLink);
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        user.IsActive = false;
        user.DeactivatedUtc = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new AppException(string.Join("; ", updateResult.Errors.Select(x => x.Description)));
        }

        await _refreshTokenRepository.RevokeActiveTokensAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var employeeIsActive = await _context.Employees
            .Where(x => x.UserId == userId)
            .Select(x => (bool?)x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return employeeIsActive is not false;
    }

    private async Task<AuthResult> BuildAuthResultAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await EnsureUserIsActiveAsync(user, cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var employeeId = await _context.Employees
            .Where(x => x.UserId == user.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var (accessToken, expiresUtc) = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            $"{user.FirstName} {user.LastName}".Trim(),
            roles.ToArray());

        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiryUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshTokenValue, expiresUtc, user.Id, user.Email ?? string.Empty, roles.ToArray(), employeeId);
    }

    private async Task EnsureUserIsActiveAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.IsActive)
        {
            throw new AppException("Your account has been deactivated.", 403);
        }

        var employeeIsInactive = await _context.Employees
            .AnyAsync(x => x.UserId == user.Id && !x.IsActive, cancellationToken);

        if (employeeIsInactive)
        {
            throw new AppException("Your employee profile is inactive.", 403);
        }
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Name = role,
                NormalizedName = role.ToUpperInvariant()
            });
        }
    }
}
