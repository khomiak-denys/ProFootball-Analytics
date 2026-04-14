using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.Id).ValueGeneratedNever();

        builder.Property(player => player.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(player => player.Name);
        builder.HasIndex(player => player.PlayerApiId).IsUnique();
        builder.HasIndex(player => player.PlayerFifaApiId);
    }
}
