using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Content> Contents { get; }
    DbSet<Episode> Episodes { get; }
    DbSet<ServerSetting> ServerSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
