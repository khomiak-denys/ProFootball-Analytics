using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class TeamSeasonStatConfiguration : IEntityTypeConfiguration<TeamSeasonStat>
{
    public void Configure(EntityTypeBuilder<TeamSeasonStat> builder)
    {
        builder.ToTable("team_season_stats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Season)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => new { x.Season, x.LeagueId, x.TeamId }).IsUnique();
        builder.HasIndex(x => x.LeagueId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => new { x.LeagueId, x.Season, x.Points });

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_team_season_stats_matches_nonnegative", "\"Matches\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_wins_nonnegative", "\"Wins\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_draws_nonnegative", "\"Draws\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_losses_nonnegative", "\"Losses\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_goals_for_nonnegative", "\"GoalsFor\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_goals_against_nonnegative", "\"GoalsAgainst\" >= 0");
            table.HasCheckConstraint("CK_team_season_stats_points_nonnegative", "\"Points\" >= 0");
        });
    }
}
