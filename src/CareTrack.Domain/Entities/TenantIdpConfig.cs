using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class TenantIdpConfig : BaseEntity
{
    public Guid UniversityId { get; set; }
    public IdpProviderType ProviderType { get; set; } = IdpProviderType.Native;
    public string? MetadataUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecretPlaceholder { get; set; }
    public string? AuthorityUrl { get; set; }
    public bool IsEnabled { get; set; } = true;

    public University University { get; set; } = null!;
}
