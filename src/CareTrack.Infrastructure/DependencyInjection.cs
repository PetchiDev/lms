using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Jobs;
using CareTrack.Infrastructure.Services;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CareTrackDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<CareTrackDbContext>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));
        services.Configure<AppSettings>(configuration.GetSection("App"));
        services.AddHttpContextAccessor();

        services.AddScoped<CareTrack.Application.Common.ITenantContext, Services.TenantContext>();
        services.AddScoped<CareTrack.Application.ICareTrackDbContext>(sp => sp.GetRequiredService<CareTrackDbContext>());
        services.AddScoped<Application.Interfaces.IJwtTokenService, Services.JwtTokenService>();
        services.AddScoped<Application.Interfaces.IEmailService, Services.EmailService>();
        services.AddScoped<Application.Interfaces.IBlobStorageService, Services.BlobStorageService>();
        services.AddScoped<Application.Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<Application.Interfaces.IUniversityService, Services.UniversityService>();
        services.AddScoped<Application.Interfaces.IProgrammeService, Services.ProgrammeService>();
        services.AddScoped<Application.Interfaces.IContentService, Services.ContentService>();
        services.AddScoped<Application.Interfaces.IEnrolmentService, Services.EnrolmentService>();
        services.AddScoped<Application.Interfaces.ILearningService, Services.LearningService>();
        services.AddScoped<Application.Interfaces.IAssessmentService, Services.AssessmentService>();
        services.AddScoped<Application.Interfaces.IReportingService, Services.ReportingService>();
        services.AddScoped<Application.Interfaces.IClinicalService, Services.ClinicalService>();
        services.AddScoped<Application.Interfaces.IIntegrationService, Services.IntegrationService>();
        services.AddScoped<Application.Interfaces.INotificationService, Services.NotificationService>();
        services.AddScoped<Application.Interfaces.ICalendarService, Services.CalendarService>();
        services.AddScoped<Application.Interfaces.IContentVersionService, Services.ContentVersionService>();

        services.AddHttpClient("Sis", client =>
        {
            var baseUrl = configuration["Integrations:Sis:ApiBaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl) && !baseUrl.Contains("PLACEHOLDER"))
                client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Integrations:Sis:TimeoutSeconds", 30));
        });

        services.AddHostedService<SignOffEscalationJob>();
        services.AddHostedService<SisRosterSyncJob>();
        services.AddHostedService<HospitalAttendanceFeedJob>();
        services.AddHostedService<NotificationReminderJob>();

        return services;
    }
}

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}
