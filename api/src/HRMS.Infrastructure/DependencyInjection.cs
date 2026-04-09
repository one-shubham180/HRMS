using HRMS.Application.Common.Interfaces;
using HRMS.Infrastructure.Persistence;
using HRMS.Infrastructure.Repositories;
using HRMS.Infrastructure.Seeding;
using HRMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using HRMS.Infrastructure.Identity;

namespace HRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AiAssistantOptions>(configuration.GetSection(AiAssistantOptions.SectionName));

        services.AddDbContext<HrmsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<HrmsDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HrmsDbContext>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHttpClient<IAiAssistantService, AiAssistantService>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IHolidayCalendarRepository, HolidayCalendarRepository>();
        services.AddScoped<IShiftDefinitionRepository, ShiftDefinitionRepository>();
        services.AddScoped<IRosterAssignmentRepository, RosterAssignmentRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAttendanceSettingsRepository, AttendanceSettingsRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<ISalaryStructureRepository, SalaryStructureRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();
        services.AddScoped<IEmployeeDocumentRepository, EmployeeDocumentRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IPerformanceAppraisalRepository, PerformanceAppraisalRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IOvertimeService, OvertimeService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IDocumentVaultService, DocumentVaultService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<SeedDataInitializer>();

        return services;
    }
}
