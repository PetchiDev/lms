using CareTrack.Application.Common;
using CareTrack.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Persistence;

internal static class TenantQueryFilterExtensions
{
    internal static void ApplyTenantFilter<TEntity>(ModelBuilder builder, ITenantContext tenantContext)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            tenantContext.IsApolloUser
            || !tenantContext.UniversityId.HasValue
            || e.UniversityId == tenantContext.UniversityId);
    }
}
