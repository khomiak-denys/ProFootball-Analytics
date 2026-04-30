using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class GenerationRunConfiguration : IEntityTypeConfiguration<GenerationRun>
{
    public void Configure(EntityTypeBuilder<GenerationRun> builder)
    {
        builder.ToTable("generation_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.GeneratorVersion)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Seed)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Profile)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Mode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasIndex(x => new { x.Profile, x.Mode, x.StartedAtUtc });
    }
}

