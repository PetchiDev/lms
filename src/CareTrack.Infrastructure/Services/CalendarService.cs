using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Calendar;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarService(CareTrackDbContext db, ITenantContext tenant, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<CalendarEventResponse>> GetMyScheduleAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var start = from?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date;
        var end = to?.ToDateTime(TimeOnly.MaxValue) ?? start.AddDays(7);

        var query = _db.CalendarEvents.AsNoTracking()
            .Include(e => e.LiveClassSession)
            .Where(e => e.EndAt >= start && e.StartAt <= end);

        if (_tenant.CohortId.HasValue)
            query = query.Where(e => e.CohortId == null || e.CohortId == _tenant.CohortId);

        var events = await query.OrderBy(e => e.StartAt).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        return events.Select(e =>
        {
            var joinUrl = e.LiveClassSession?.JoinUrl;
            var canJoin = e.EventType == CalendarEventType.LiveClass
                && e.StartAt.AddMinutes(-15) <= now && e.EndAt >= now;
            return new CalendarEventResponse(e.Id, e.Title, e.Description, e.StartAt, e.EndAt, e.EventType.ToString(), joinUrl, canJoin);
        }).ToList();
    }

    public async Task<CalendarEventResponse> CreateEventAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser && _tenant.Role != UserRole.UniversityAdmin)
            throw new ForbiddenException("Insufficient permissions.");

        var universityId = _tenant.UniversityId
            ?? throw new ValidationException("University context required.");

        if (!Enum.TryParse<CalendarEventType>(request.EventType, true, out var eventType))
            throw new ValidationException("Invalid event type.");

        var evt = new CalendarEvent
        {
            UniversityId = universityId,
            CohortId = request.CohortId,
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            EventType = eventType
        };
        _db.CalendarEvents.Add(evt);

        if (eventType == CalendarEventType.LiveClass && !string.IsNullOrWhiteSpace(request.JoinUrl))
        {
            _db.LiveClassSessions.Add(new LiveClassSession
            {
                CalendarEvent = evt,
                JoinUrl = request.JoinUrl,
                MinAttendanceMinutes = 45
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CalendarEventResponse(evt.Id, evt.Title, evt.Description, evt.StartAt, evt.EndAt, evt.EventType.ToString(), request.JoinUrl, false);
    }

    public async Task<IReadOnlyList<DiscussionThreadResponse>> GetLessonThreadsAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _db.DiscussionThreads.AsNoTracking()
            .Where(t => t.LessonId == lessonId)
            .Select(t => new DiscussionThreadResponse(t.Id, t.Title, t.Posts.Count, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscussionThreadResponse> CreateThreadAsync(Guid lessonId, CreateThreadRequest request, CancellationToken cancellationToken = default)
    {
        var universityId = _tenant.UniversityId ?? throw new ValidationException("University context required.");
        var userId = _tenant.UserId ?? throw new ForbiddenException("Authentication required.");

        var thread = new DiscussionThread
        {
            UniversityId = universityId,
            LessonId = lessonId,
            Title = request.Title,
            CreatedByUserId = userId
        };
        _db.DiscussionThreads.Add(thread);
        _db.DiscussionPosts.Add(new DiscussionPost
        {
            Thread = thread,
            Body = request.Body,
            CreatedByUserId = userId
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new DiscussionThreadResponse(thread.Id, thread.Title, 1, thread.CreatedAt);
    }

    public async Task<IReadOnlyList<DiscussionPostResponse>> GetThreadPostsAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var posts = await _db.DiscussionPosts.AsNoTracking()
            .Where(p => p.ThreadId == threadId && p.ParentPostId == null)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<DiscussionPostResponse>();
        foreach (var post in posts)
        {
            var author = await _userManager.FindByIdAsync(post.CreatedByUserId);
            var replies = await _db.DiscussionPosts.AsNoTracking()
                .Where(r => r.ParentPostId == post.Id)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var replyDtos = new List<DiscussionPostResponse>();
            foreach (var reply in replies)
            {
                var replyAuthor = await _userManager.FindByIdAsync(reply.CreatedByUserId);
                replyDtos.Add(new DiscussionPostResponse(reply.Id, reply.Body, replyAuthor?.FullName ?? "User", reply.CreatedAt, []));
            }

            result.Add(new DiscussionPostResponse(post.Id, post.Body, author?.FullName ?? "User", post.CreatedAt, replyDtos));
        }

        return result;
    }

    public async Task<DiscussionPostResponse> CreatePostAsync(Guid threadId, CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _tenant.UserId ?? throw new ForbiddenException("Authentication required.");
        var thread = await _db.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken)
            ?? throw new NotFoundException("Thread not found.");

        if (thread.IsLocked) throw new ConflictException("Thread is locked.");

        var post = new DiscussionPost
        {
            ThreadId = threadId,
            Body = request.Body,
            CreatedByUserId = userId,
            ParentPostId = request.ParentPostId
        };
        _db.DiscussionPosts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        var author = await _userManager.FindByIdAsync(userId);
        return new DiscussionPostResponse(post.Id, post.Body, author?.FullName ?? "User", post.CreatedAt, []);
    }
}
