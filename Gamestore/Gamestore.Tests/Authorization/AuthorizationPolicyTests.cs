using System.Security.Claims;
using Gamestore.API.Constants;
using Gamestore.API.DependencyResolvers;
using Gamestore.BLL.Constants;
using Gamestore.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestore.Tests.Authorization;

public class AuthorizationPolicyTests
{
    [Theory]
    [InlineData(AuthorizationPolicyNames.UserOrHigher, RoleNames.User)]
    [InlineData(AuthorizationPolicyNames.UserOrHigher, RoleNames.Administrator)]
    [InlineData(AuthorizationPolicyNames.ModeratorOrHigher, RoleNames.Moderator)]
    [InlineData(AuthorizationPolicyNames.ModeratorOrHigher, RoleNames.Manager)]
    [InlineData(AuthorizationPolicyNames.ManagerOrHigher, RoleNames.Manager)]
    [InlineData(AuthorizationPolicyNames.ManagerOrHigher, RoleNames.Administrator)]
    [InlineData(AuthorizationPolicyNames.AdministratorOnly, RoleNames.Administrator)]
    public async Task PolicyShouldAllowConfiguredRole(string policyName, string roleName)
    {
        using var provider = CreateServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(roleName);

        var result = await authorizationService.AuthorizeAsync(user, null, policyName);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(AuthorizationPolicyNames.ModeratorOrHigher, RoleNames.User)]
    [InlineData(AuthorizationPolicyNames.ManagerOrHigher, RoleNames.Moderator)]
    [InlineData(AuthorizationPolicyNames.AdministratorOnly, RoleNames.Manager)]
    public async Task PolicyShouldRejectInsufficientRole(string policyName, string roleName)
    {
        using var provider = CreateServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(roleName);

        var result = await authorizationService.AuthorizeAsync(user, null, policyName);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task PolicyShouldRejectUnauthenticatedUser()
    {
        using var provider = CreateServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await authorizationService.AuthorizeAsync(
            user,
            null,
            AuthorizationPolicyNames.UserOrHigher);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:Key"] = "test-signing-key-with-at-least-32-characters",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtAuthentication(configuration);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreateUser(string roleName)
    {
        var identity = new ClaimsIdentity(
            [new Claim(JwtClaimNames.Role, roleName)],
            authenticationType: "Test",
            nameType: ClaimTypes.Name,
            roleType: JwtClaimNames.Role);

        return new ClaimsPrincipal(identity);
    }
}
