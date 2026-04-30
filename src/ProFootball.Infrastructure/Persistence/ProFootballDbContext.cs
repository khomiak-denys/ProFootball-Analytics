using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence;

public sealed class ProFootballDbContext(DbContextOptions<ProFootballDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<League> Leagues => Set<League>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamAttribute> TeamAttributes => Set<TeamAttribute>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerAttribute> PlayerAttributes => Set<PlayerAttribute>();
    public DbSet<FootballMatch> Matches => Set<FootballMatch>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<PlayerMatchStat> PlayerMatchStats => Set<PlayerMatchStat>();
    public DbSet<TeamSeasonStat> TeamSeasonStats => Set<TeamSeasonStat>();
    public DbSet<AnalyticsFactDaily> AnalyticsFactDaily => Set<AnalyticsFactDaily>();
    public DbSet<GenerationRun> GenerationRuns => Set<GenerationRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProFootballDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
