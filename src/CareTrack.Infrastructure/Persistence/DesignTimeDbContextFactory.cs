using CareTrack.Application.Common;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace CareTrack.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CareTrackDbContext>
{
    public CareTrackDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext, DesignTimeTenantContext>();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<CareTrackDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=lms;Username=postgres;Password=Password@1")
            .Options;

        return new CareTrackDbContext(options, provider.GetRequiredService<ITenantContext>());
    }
}

internal sealed class DesignTimeTenantContext : ITenantContext
{
    public string? UserId => null;
    public Domain.Enums.UserRole Role => Domain.Enums.UserRole.ApolloAdmin;
    public Guid? UniversityId => null;
    public Guid? CohortId => null;
    public Guid? StudentId => null;
    public Guid? SupervisorId => null;
    public bool IsApolloUser => true;
    public void EnsureUniversityAccess(Guid universityId) { }
}
