using System.IdentityModel.Tokens.Jwt;
using Gamestore.BLL.Constants;
using Gamestore.BLL.Services.Concrete;
using Gamestore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Gamestore.Tests.Services;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GenerateTokenShouldIncludeUserIdentityAndRoles()
    {
        var user = CreateUser();
        var service = CreateService();

        var encodedToken = service.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.Name, token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(
            ["Administrator", "Moderator"],
            token.Claims
                .Where(claim => claim.Type == JwtClaimNames.Role)
                .Select(claim => claim.Value));
    }

    [Fact]
    public void GenerateTokenShouldUseConfiguredIssuerAudienceAndLifetime()
    {
        var service = CreateService();

        var encodedToken = service.GenerateToken(CreateUser());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);

        Assert.Equal("Gamestore.API", token.Issuer);
        Assert.Contains("Gamestore.UI", token.Audiences);
        Assert.Equal(Now.UtcDateTime.AddMinutes(60), token.ValidTo);
    }

    [Fact]
    public void ConstructorShouldRejectShortSigningKey()
    {
        var configuration = CreateConfiguration("short-key");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(configuration, new FixedTimeProvider(Now)));

        Assert.Equal("Jwt:Key must be at least 32 bytes long.", exception.Message);
    }

    private static JwtTokenService CreateService()
    {
        return new JwtTokenService(
            CreateConfiguration("Gamestore-Test-Signing-Key-At-Least-32-Bytes"),
            new FixedTimeProvider(Now));
    }

    private static IConfiguration CreateConfiguration(string signingKey)
    {
        var configuration = new Mock<IConfiguration>();
        configuration.SetupGet(value => value["Jwt:Issuer"]).Returns("Gamestore.API");
        configuration.SetupGet(value => value["Jwt:Audience"]).Returns("Gamestore.UI");
        configuration.SetupGet(value => value["Jwt:Key"]).Returns(signingKey);
        configuration.SetupGet(value => value["Jwt:ExpirationMinutes"]).Returns("60");

        return configuration.Object;
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Administrator",
            PasswordHash = "password-hash",
            UserRoles =
            [
                new UserRole
                {
                    Role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "Administrator",
                    },
                },
                new UserRole
                {
                    Role = new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "Moderator",
                    },
                },
            ],
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
