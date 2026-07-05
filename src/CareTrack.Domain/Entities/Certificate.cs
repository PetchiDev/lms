using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Certificate : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ProgrammeId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? PdfBlobUrl { get; set; }

    public Student Student { get; set; } = null!;
    public Programme Programme { get; set; } = null!;
}
