using Gamestore.BLL.Services.Concrete;
using Gamestore.Domain.Constants;

namespace Gamestore.Tests.Services;

public class PageAccessServiceTests
{
    private readonly PageAccessService _service = new();

    [Theory]
    [InlineData("Games")]
    [InlineData("Game")]
    [InlineData("Genres")]
    [InlineData("Platforms")]
    [InlineData("Publishers")]
    [InlineData("Login")]
    public void HasAccessShouldAllowGuestPages(string targetPage)
    {
        var result = _service.HasAccess(targetPage, []);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessShouldRejectProtectedPageForGuest()
    {
        var result = _service.HasAccess("Basket", []);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Basket")]
    [InlineData("MakeOrder")]
    public void HasAccessShouldAllowUserPagesForUser(string targetPage)
    {
        var result = _service.HasAccess(targetPage, [RoleNames.User]);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessShouldRejectManagerPageForModerator()
    {
        var result = _service.HasAccess("AddGame", [RoleNames.Moderator]);

        Assert.False(result);
    }

    [Theory]
    [InlineData("AddGame")]
    [InlineData("UpdatePublisher")]
    [InlineData("Orders")]
    [InlineData("History")]
    public void HasAccessShouldAllowManagerPagesForManager(string targetPage)
    {
        var result = _service.HasAccess(targetPage, [RoleNames.Manager]);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessShouldUseHighestRoleWhenUserHasMultipleRoles()
    {
        var result = _service.HasAccess(
            "UpdateOrder",
            [RoleNames.User, RoleNames.Manager]);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Users")]
    [InlineData("AddUser")]
    [InlineData("Roles")]
    [InlineData("UpdateRole")]
    public void HasAccessShouldAllowAdministrationPagesForAdministrator(string targetPage)
    {
        var result = _service.HasAccess(targetPage, [RoleNames.Administrator]);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessShouldRejectAdministrationPageForManager()
    {
        var result = _service.HasAccess("Users", [RoleNames.Manager]);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("UnknownPage")]
    public void HasAccessShouldRejectUnknownOrEmptyPage(string targetPage)
    {
        var result = _service.HasAccess(targetPage, [RoleNames.Administrator]);

        Assert.False(result);
    }

    [Fact]
    public void HasAccessShouldIgnoreRoleNameCasing()
    {
        var result = _service.HasAccess("addgame", ["manager"]);

        Assert.True(result);
    }
}
