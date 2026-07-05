namespace CareTrack.Application.DTOs.Notifications;

public record NotificationResponse(Guid Id, string Type, string Title, string Body, string Channel, bool IsRead, DateTime CreatedAt);
public record CreateNotificationRequest(string UserId, string Type, string Title, string Body, string Channel);
