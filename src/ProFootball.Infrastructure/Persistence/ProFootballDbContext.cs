using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence;

public sealed class ProFootballDbContext(DbContextOptions<ProFootballDbContext> options) : DbContext(options)
{
    public DbSet<FootballClub> FootballClubs => Set<FootballClub>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProFootballDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
