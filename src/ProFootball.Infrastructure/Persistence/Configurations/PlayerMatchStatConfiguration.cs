using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class PlayerMatchStatConfiguration : IEntityTypeConfiguration<PlayerMatchStat>
{
    public void Configure(EntityTypeBuilder<PlayerMatchStat> builder)
    {
        builder.ToTable("player_match_stats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Xg)
            .HasPrecision(10, 3)
            .IsRequired();

        builder.HasIndex(x => x.MatchId);
        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => new { x.MatchId, x.PlayerId }).IsUnique();

        builder.HasOne<FootballMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_player_match_stats_minutes_nonnegative", "\"Minutes\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_shots_nonnegative", "\"Shots\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_passes_nonnegative", "\"Passes\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_tackles_nonnegative", "\"Tackles\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_goals_nonnegative", "\"Goals\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_assists_nonnegative", "\"Assists\" >= 0");
            table.HasCheckConstraint("CK_player_match_stats_xg_nonnegative", "\"Xg\" >= 0");
        });
    }
}
