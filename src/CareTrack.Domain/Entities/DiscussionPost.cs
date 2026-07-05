using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class DiscussionPost : BaseEntity
{
    public Guid ThreadId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? ParentPostId { get; set; }

    public DiscussionThread Thread { get; set; } = null!;
    public DiscussionPost? ParentPost { get; set; }
    public ICollection<DiscussionPost> Replies { get; set; } = [];
}
