using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
{
    public void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        builder.ToTable("match_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.EventType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.MatchId);
        builder.HasIndex(x => new { x.MatchId, x.Minute });
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => new { x.MatchId, x.TeamId, x.EventType });

        builder.HasOne<FootballMatch>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.AssistPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_match_events_minute_range", "\"Minute\" >= 1 AND \"Minute\" <= 130");
        });
    }
}
