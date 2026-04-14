using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class FootballMatchConfiguration : IEntityTypeConfiguration<FootballMatch>
{
    public void Configure(EntityTypeBuilder<FootballMatch> builder)
    {
        builder.ToTable("matches");

        builder.HasKey(match => match.Id);

        builder.Property(match => match.Id).ValueGeneratedNever();

        builder.Property(match => match.Season)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(match => match.MatchApiId).IsUnique();
        builder.HasIndex(match => match.Date);
        builder.HasIndex(match => match.Season);
        builder.HasIndex(match => match.LeagueId);
        builder.HasIndex(match => match.HomeTeamApiId);
        builder.HasIndex(match => match.AwayTeamApiId);

        builder.HasOne(match => match.Country)
            .WithMany()
            .HasForeignKey(match => match.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(match => match.League)
            .WithMany()
            .HasForeignKey(match => match.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
