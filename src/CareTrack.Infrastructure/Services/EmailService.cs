using CareTrack.Application.Interfaces;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CareTrack.Infrastructure.Services;

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "CareTrack";
    public bool Enabled { get; set; }
}

public class EmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly string _frontendBaseUrl;
    private readonly CareTrackDbContext _db;

    private const string DefaultInviteSubject = "Activate your CareTrack account";

    public EmailService(
        IOptions<SendGridSettings> settings,
        IOptions<AppSettings> appSettings,
        CareTrackDbContext db,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _db = db;
        _frontendBaseUrl = appSettings.Value.FrontendBaseUrl.TrimEnd('/');
    }

    public async Task SendInviteEmailAsync(
        string email,
        string fullName,
        string activationToken,
        Guid? universityId = null,
        CancellationToken cancellationToken = default)
    {
        var activationUrl = $"{_frontendBaseUrl}/#/activate?token={Uri.EscapeDataString(activationToken)}";
        var subject = DefaultInviteSubject;
        var html = BuildDefaultInviteHtml(fullName, activationUrl);
        string? fromEmail = null;
        string? fromName = null;

        if (universityId.HasValue)
        {
            var uni = await _db.Universities.AsNoTracking()
                .Where(u => u.Id == universityId.Value)
                .Select(u => new
                {
                    u.Name,
                    u.LogoUrl,
                    u.EmailInviteSubject,
                    u.EmailInviteBodyHtml,
                    u.EmailFromEmail,
                    u.EmailFromName
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (uni is not null)
            {
                fromEmail = uni.EmailFromEmail;
                fromName = uni.EmailFromName;

                if (!string.IsNullOrWhiteSpace(uni.EmailInviteSubject))
                    subject = uni.EmailInviteSubject;

                if (!string.IsNullOrWhiteSpace(uni.EmailInviteBodyHtml))
                {
                    html = RenderTemplate(uni.EmailInviteBodyHtml, fullName, activationUrl, uni.Name, uni.LogoUrl);
                }
                else if (!string.IsNullOrWhiteSpace(uni.Name))
                {
                    html = BuildDefaultInviteHtml(fullName, activationUrl, uni.Name, uni.LogoUrl);
                }
            }
        }

        await SendEmailAsync(email, subject, html, fromEmail, fromName, cancellationToken);
    }

    public async Task SendEmailAsync(
        string email,
        string subject,
        string body,
        string? fromEmail = null,
        string? fromName = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogInformation("SendGrid disabled — would email {Email}: {Subject}", email, subject);
            return;
        }

        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(fromEmail ?? _settings.FromEmail, fromName ?? _settings.FromName);
        var to = new EmailAddress(email);
        var isHtml = body.Contains('<') && body.Contains('>');
        var msg = isHtml
            ? MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: StripHtml(body), htmlContent: body)
            : MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: body, htmlContent: body);

        var response = await client.SendEmailAsync(msg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid failed ({StatusCode}) to {Email}: {Error}", response.StatusCode, email, error);
            throw new InvalidOperationException($"SendGrid email failed: {response.StatusCode}");
        }

        _logger.LogInformation("Email sent via SendGrid to {Email}: {Subject}", email, subject);
    }

    private static string RenderTemplate(
        string template,
        string fullName,
        string activationUrl,
        string universityName,
        string? logoUrl)
    {
        return template
            .Replace("{{FullName}}", fullName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{ActivationUrl}}", activationUrl, StringComparison.OrdinalIgnoreCase)
            .Replace("{{UniversityName}}", universityName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{LogoUrl}}", logoUrl ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDefaultInviteHtml(
        string fullName,
        string activationUrl,
        string? universityName = null,
        string? logoUrl = null)
    {
        var org = string.IsNullOrWhiteSpace(universityName) ? "CareTrack LMS" : universityName;
        var logoBlock = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"""<p><img src="{logoUrl}" alt="{org}" style="max-height:48px"/></p>""";

        return $"""
            {logoBlock}
            <p>Hi {fullName},</p>
            <p>You have been invited to {org}. Click the link below to activate your account (valid 72 hours):</p>
            <p><a href="{activationUrl}">Activate account</a></p>
            <p>If the button does not work, copy this URL:<br/>{activationUrl}</p>
            """;
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
}

public class AppSettings
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
