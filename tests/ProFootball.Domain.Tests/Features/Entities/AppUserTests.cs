using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Domain.Tests.Features.Entities;

public class AppUserTests
{
    [Fact]
    public void Constructor_ShouldSetValues_AndTrimNames()
    {
        var createdAt = new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc);

        var user = new AppUser(
            "  Denys ",
            "  Test ",
            "  manager ",
            "  MANAGER ",
            "  hash-value ",
            AppUserRole.Manager,
            isActive: true,
            createdAtUtc: createdAt);

        Assert.Equal("Denys", user.FirstName);
        Assert.Equal("Test", user.LastName);
        Assert.Equal("manager", user.Login);
        Assert.Equal("MANAGER", user.NormalizedLogin);
        Assert.Equal("hash-value", user.PasswordHash);
        Assert.Equal(AppUserRole.Manager, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal(createdAt, user.CreatedAtUtc);
        Assert.Equal("Denys Test", user.GetDisplayName());
    }

    [Theory]
    [InlineData(null, "Test", "manager", "MANAGER", "hash")]
    [InlineData("", "Test", "manager", "MANAGER", "hash")]
    [InlineData("Denys", null, "manager", "MANAGER", "hash")]
    [InlineData("Denys", "", "manager", "MANAGER", "hash")]
    [InlineData("Denys", "Test", null, "MANAGER", "hash")]
    [InlineData("Denys", "Test", "", "MANAGER", "hash")]
    [InlineData("Denys", "Test", "manager", null, "hash")]
    [InlineData("Denys", "Test", "manager", "", "hash")]
    [InlineData("Denys", "Test", "manager", "MANAGER", null)]
    [InlineData("Denys", "Test", "manager", "MANAGER", "")]
    public void Constructor_ShouldThrow_WhenRequiredInputMissing(
        string? firstName,
        string? lastName,
        string? login,
        string? normalizedLogin,
        string? passwordHash)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new AppUser(
                firstName!,
                lastName!,
                login!,
                normalizedLogin!,
                passwordHash!,
                AppUserRole.Analyst,
                isActive: true,
                createdAtUtc: DateTime.UtcNow));
    }
}
