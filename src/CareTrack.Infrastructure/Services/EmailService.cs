using CareTrack.Application.Interfaces;
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

    public EmailService(
        IOptions<SendGridSettings> settings,
        IOptions<AppSettings> appSettings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _frontendBaseUrl = appSettings.Value.FrontendBaseUrl.TrimEnd('/');
    }

    public Task SendInviteEmailAsync(string email, string fullName, string activationToken, CancellationToken cancellationToken = default)
    {
        var activationUrl = $"{_frontendBaseUrl}/activate?token={Uri.EscapeDataString(activationToken)}";
        var html = $"""
            <p>Hi {fullName},</p>
            <p>You have been invited to CareTrack LMS. Click the link below to activate your account (valid 72 hours):</p>
            <p><a href="{activationUrl}">Activate account</a></p>
            <p>If the button does not work, copy this URL:<br/>{activationUrl}</p>
            """;
        return SendEmailAsync(email, "Activate your CareTrack account", html, cancellationToken);
    }

    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogInformation("SendGrid disabled — would email {Email}: {Subject}", email, subject);
            return;
        }

        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
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

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
}

public class AppSettings
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
