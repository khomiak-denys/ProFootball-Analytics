using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Id).ValueGeneratedNever();

        builder.Property(country => country.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(country => country.Name);
    }
}
