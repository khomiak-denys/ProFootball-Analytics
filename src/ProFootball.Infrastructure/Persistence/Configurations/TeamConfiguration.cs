using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(team => team.Id);

        builder.Property(team => team.Id).ValueGeneratedNever();

        builder.Property(team => team.LongName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(team => team.ShortName)
            .HasMaxLength(20);

        builder.HasIndex(team => team.LongName);
        builder.HasIndex(team => team.TeamFifaApiId);
    }
}
