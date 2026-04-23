using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence;

public sealed class ProFootballDbContext(DbContextOptions<ProFootballDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<FootballClub> FootballClubs => Set<FootballClub>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamAttribute> TeamAttributes => Set<TeamAttribute>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerAttribute> PlayerAttributes => Set<PlayerAttribute>();
    public DbSet<FootballMatch> Matches => Set<FootballMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProFootballDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
