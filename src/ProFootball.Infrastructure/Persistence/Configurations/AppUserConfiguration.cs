using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(user => user.Login)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(user => user.Login)
            .IsUnique();

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_app_users_login_lowercase", "\"Login\" = lower(\"Login\")");
        });
    }
}
