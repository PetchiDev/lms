using CareTrack.Application.Interfaces;

namespace CareTrack.Application;

public interface ICareTrackDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
