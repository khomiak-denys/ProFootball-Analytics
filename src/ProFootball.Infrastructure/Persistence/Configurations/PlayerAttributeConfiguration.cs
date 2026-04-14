using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class PlayerAttributeConfiguration : IEntityTypeConfiguration<PlayerAttribute>
{
    public void Configure(EntityTypeBuilder<PlayerAttribute> builder)
    {
        builder.ToTable("player_attributes");

        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.Id).ValueGeneratedNever();

        builder.Property(attribute => attribute.PreferredFoot).HasMaxLength(32);
        builder.Property(attribute => attribute.AttackingWorkRate).HasMaxLength(32);
        builder.Property(attribute => attribute.DefensiveWorkRate).HasMaxLength(32);

        builder.HasIndex(attribute => attribute.PlayerApiId);
        builder.HasIndex(attribute => attribute.Date);
        builder.HasIndex(attribute => new { attribute.PlayerApiId, attribute.Date });
        builder.HasIndex(attribute => attribute.OverallRating);
        builder.HasIndex(attribute => attribute.Potential);

        builder.HasOne(attribute => attribute.Player)
            .WithMany()
            .HasForeignKey(attribute => attribute.PlayerApiId)
            .HasPrincipalKey(player => player.PlayerApiId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
