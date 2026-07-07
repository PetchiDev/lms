using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Notifications;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        CareTrackDbContext db,
        ITenantContext tenant,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _tenant = tenant;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAsync(string userId, Guid universityId, string type, string title, string body, string channel, Guid? relatedEntityId = null, CancellationToken cancellationToken = default)
    {
        var parsedChannel = Enum.TryParse<NotificationChannel>(channel, true, out var ch) ? ch : NotificationChannel.InApp;

        _db.Notifications.Add(new Notification
        {
            UniversityId = universityId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Channel = parsedChannel,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = type
        });
        await _db.SaveChangesAsync(cancellationToken);

        if (parsedChannel is NotificationChannel.Email or NotificationChannel.Push)
        {
            _logger.LogInformation("Notification [{Type}] to {UserId}: {Title} (channel: {Channel})", type, userId, title, channel);
            if (parsedChannel == NotificationChannel.Email)
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user?.Email is not null)
                    await _emailService.SendEmailAsync(user.Email, title, body, cancellationToken: cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _tenant.UserId ?? throw new ForbiddenException("Authentication required.");

        return await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(n.Id, n.Type, n.Title, n.Body, n.Channel.ToString(), n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userId = _tenant.UserId ?? throw new ForbiddenException("Authentication required.");
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessRemindersAsync(CancellationToken cancellationToken = default)
    {
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
        var staleEntries = await _db.LogbookEntries
            .IgnoreQueryFilters()
            .Where(e => e.Status == LogbookEntryStatus.PendingSignoff && e.SubmittedAt <= threeDaysAgo)
            .GroupBy(e => e.UniversityId)
            .Select(g => new { UniversityId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var group in staleEntries)
        {
            var supervisors = await _db.Supervisors.AsNoTracking()
                .Where(s => s.UniversityId == group.UniversityId && s.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var sup in supervisors)
                await SendAsync(sup.UserId, group.UniversityId, "signoff_reminder",
                    $"{group.Count} entries pending", "You have logbook entries awaiting sign-off for 3+ days.", "InApp", null, cancellationToken);
        }
    }
}
