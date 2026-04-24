using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class TeamAttributeConfiguration : IEntityTypeConfiguration<TeamAttribute>
{
    public void Configure(EntityTypeBuilder<TeamAttribute> builder)
    {
        builder.ToTable("team_attributes");

        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.Id).ValueGeneratedNever();

        builder.HasIndex(attribute => attribute.TeamId);
        builder.HasIndex(attribute => attribute.Date);
        builder.HasIndex(attribute => new { attribute.TeamId, attribute.Date });

        builder.HasOne(attribute => attribute.Team)
            .WithMany()
            .HasForeignKey(attribute => attribute.TeamId)
            .HasPrincipalKey(team => team.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
