using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class AnalyticsFactDailyConfiguration : IEntityTypeConfiguration<AnalyticsFactDaily>
{
    public void Configure(EntityTypeBuilder<AnalyticsFactDaily> builder)
    {
        builder.ToTable("analytics_fact_daily");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.DateKey)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Season)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.MetricKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.MetricValue)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.HasIndex(x => new { x.DateKey, x.Season, x.LeagueId, x.MetricKey }).IsUnique();
        builder.HasIndex(x => x.LeagueId);
        builder.HasIndex(x => x.MetricKey);

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_analytics_fact_daily_metric_value_nonnegative", "\"MetricValue\" >= 0");
        });
    }
}
