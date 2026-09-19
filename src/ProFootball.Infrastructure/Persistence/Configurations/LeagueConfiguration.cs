using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("leagues");

        builder.HasKey(league => league.Id);

        builder.Property(league => league.Id).ValueGeneratedNever();

        builder.Property(league => league.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(league => league.CountryName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(league => league.MaxTeams)
            .HasColumnName("max_teams");

        builder.Property(league => league.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.HasIndex(league => league.Name);
        builder.HasIndex(league => league.CountryName);
    }
}
