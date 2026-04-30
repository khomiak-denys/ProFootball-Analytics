using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Querying.ReadModels;

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
    public DbSet<DashboardOverviewReadModel> DashboardOverview => Set<DashboardOverviewReadModel>();
    public DbSet<MatchOutcomeStatsReadModel> MatchOutcomeStats => Set<MatchOutcomeStatsReadModel>();
    public DbSet<PlayerRatingTrendMonthlyReadModel> PlayerRatingTrendMonthly => Set<PlayerRatingTrendMonthlyReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProFootballDbContext).Assembly);

        modelBuilder.Entity<DashboardOverviewReadModel>(builder =>
        {
            builder.HasNoKey();
            builder.ToView("vw_dashboard_overview");
            builder.Property(x => x.Season).HasColumnName("season");
            builder.Property(x => x.LeagueId).HasColumnName("league_id");
            builder.Property(x => x.LeagueName).HasColumnName("league_name");
            builder.Property(x => x.CountryName).HasColumnName("country_name");
            builder.Property(x => x.MatchCount).HasColumnName("match_count");
            builder.Property(x => x.TotalClubs).HasColumnName("total_clubs");
            builder.Property(x => x.HomeWins).HasColumnName("home_wins");
            builder.Property(x => x.Draws).HasColumnName("draws");
            builder.Property(x => x.AwayWins).HasColumnName("away_wins");
            builder.Property(x => x.AvgGoalsPerMatch).HasColumnName("avg_goals_per_match");
            builder.Property(x => x.LastMatchDate).HasColumnName("last_match_date");
        });

        modelBuilder.Entity<MatchOutcomeStatsReadModel>(builder =>
        {
            builder.HasNoKey();
            builder.ToView("vw_match_outcome_stats");
            builder.Property(x => x.Season).HasColumnName("season");
            builder.Property(x => x.LeagueId).HasColumnName("league_id");
            builder.Property(x => x.LeagueName).HasColumnName("league_name");
            builder.Property(x => x.CountryName).HasColumnName("country_name");
            builder.Property(x => x.MatchCount).HasColumnName("match_count");
            builder.Property(x => x.HomeWins).HasColumnName("home_wins");
            builder.Property(x => x.Draws).HasColumnName("draws");
            builder.Property(x => x.AwayWins).HasColumnName("away_wins");
            builder.Property(x => x.AvgGoalsPerMatch).HasColumnName("avg_goals_per_match");
        });

        modelBuilder.Entity<PlayerRatingTrendMonthlyReadModel>(builder =>
        {
            builder.HasNoKey();
            builder.ToView("vw_player_rating_trend_monthly");
            builder.Property(x => x.Season).HasColumnName("season");
            builder.Property(x => x.LeagueId).HasColumnName("league_id");
            builder.Property(x => x.MonthStart).HasColumnName("month_start");
            builder.Property(x => x.AvgOverallRating).HasColumnName("avg_overall_rating");
        });

        base.OnModelCreating(modelBuilder);
    }
}
