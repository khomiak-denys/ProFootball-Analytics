using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class FootballClubConfiguration : IEntityTypeConfiguration<FootballClub>
{
    public void Configure(EntityTypeBuilder<FootballClub> builder)
    {
        builder.ToTable("football_clubs");

        builder.HasKey(club => club.Id);

        builder.Property(club => club.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(club => club.FoundedYear)
            .IsRequired();
    }
}
