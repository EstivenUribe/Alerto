using Alerto.Domain.Entities;
using Alerto.Domain.Enums;
using Alerto.Domain.Exceptions;

namespace Alerto.DomainTests;

public sealed class UserTests
{
    private static readonly DateTime UtcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);

    private static User CreateValidUser()
        => User.Create("operador01", "Operador Uno", "operador@alerto.local", UserRole.Operator, "hashed_password", UtcNow);

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldBeActiveByDefault()
    {
        var user = CreateValidUser();

        user.IsActive.Should().BeTrue();
        user.IsTwoFactorEnabled.Should().BeFalse();
        user.Version.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldRaiseUserCreatedDomainEvent()
    {
        var user = CreateValidUser();

        user.DomainEvents.Should().ContainSingle()
            .Which.GetType().Name.Should().Be("UserCreatedDomainEvent");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyUsername_ShouldThrowEntityValidationException(string username)
    {
        var act = () => User.Create(username, "Display", "email@test.com", UserRole.Operator, "hash", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    [Fact]
    public void Create_WithUsernameExceeding80Chars_ShouldThrowEntityValidationException()
    {
        var longUsername = new string('x', 81);

        var act = () => User.Create(longUsername, "Display", "email@test.com", UserRole.Operator, "hash", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    public void Create_WithInvalidEmail_ShouldThrowEntityValidationException(string email)
    {
        var act = () => User.Create("user1", "Display", email, UserRole.Operator, "hash", UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    // ── 2FA Provisioning ────────────────────────────────────────────────────

    [Fact]
    public void ProvisionTwoFactor_ShouldStorSecretButNotEnable2FA()
    {
        var user = CreateValidUser();

        user.ProvisionTwoFactor("JBSWY3DPEHPK3PXP", UtcNow.AddMinutes(1));

        user.TotpSecret.Should().NotBeNullOrEmpty();
        user.IsTwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public void EnableTwoFactor_WithoutProvisioningFirst_ShouldThrowTwoFactorProvisioningException()
    {
        var user = CreateValidUser();

        var act = () => user.EnableTwoFactor(UtcNow.AddMinutes(1));

        act.Should().Throw<TwoFactorProvisioningException>();
    }

    [Fact]
    public void EnableTwoFactor_AfterProvisioning_ShouldEnable2FAAndRaiseEvent()
    {
        var user = CreateValidUser();
        user.ProvisionTwoFactor("JBSWY3DPEHPK3PXP", UtcNow.AddMinutes(1));
        user.ClearDomainEvents();

        user.EnableTwoFactor(UtcNow.AddMinutes(2));

        user.IsTwoFactorEnabled.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle()
            .Which.GetType().Name.Should().Be("TwoFactorEnabledDomainEvent");
    }

    [Fact]
    public void DisableTwoFactor_ShouldClearSecretAndDisable2FA()
    {
        var user = CreateValidUser();
        user.ProvisionTwoFactor("JBSWY3DPEHPK3PXP", UtcNow.AddMinutes(1));
        user.EnableTwoFactor(UtcNow.AddMinutes(2));

        user.DisableTwoFactor(UtcNow.AddMinutes(3));

        user.IsTwoFactorEnabled.Should().BeFalse();
        user.TotpSecret.Should().BeNull();
    }

    // ── UpdateProfile ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_ShouldIncrementVersion()
    {
        var user = CreateValidUser();

        user.UpdateProfile("Nuevo Nombre", "nuevo@alerto.local", UserRole.Analyst, true, UtcNow.AddMinutes(1));

        user.Version.Should().Be(1);
        user.DisplayName.Should().Be("Nuevo Nombre");
        user.Role.Should().Be(UserRole.Analyst);
    }

    [Fact]
    public void UpdateProfile_CanDeactivateUser()
    {
        var user = CreateValidUser();

        user.UpdateProfile("Nombre", "email@alerto.local", UserRole.Operator, false, UtcNow.AddMinutes(1));

        user.IsActive.Should().BeFalse();
    }

    // ── RefreshToken ─────────────────────────────────────────────────────────

    [Fact]
    public void GetActiveRefreshToken_WithExpiredToken_ShouldThrowDomainRuleException()
    {
        var user = CreateValidUser();
        var token = RefreshToken.Issue(user.Id, "token-value", UtcNow.AddDays(7), "127.0.0.1");
        user.AddRefreshToken(token, UtcNow);
        token.Revoke(UtcNow.AddHours(1));

        var act = () => user.GetActiveRefreshToken("token-value");

        act.Should().Throw<DomainRuleException>();
    }
}
