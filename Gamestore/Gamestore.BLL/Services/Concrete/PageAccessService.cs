using Gamestore.BLL.Services.Abstract;
using Gamestore.Domain.Constants;

namespace Gamestore.BLL.Services.Concrete;

public class PageAccessService : IPageAccessService
{
    private const int GuestRank = 0;
    private const int UserRank = 1;
    private const int ModeratorRank = 2;
    private const int ManagerRank = 3;
    private const int AdministratorRank = 4;

    private static readonly Dictionary<string, int> RoleRanks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RoleNames.Guest] = GuestRank,
            [RoleNames.User] = UserRank,
            [RoleNames.Moderator] = ModeratorRank,
            [RoleNames.Manager] = ManagerRank,
            [RoleNames.Administrator] = AdministratorRank,
        };

    private static readonly Dictionary<string, int> PageMinimumRoleRanks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Login"] = GuestRank,
            ["Games"] = GuestRank,
            ["Game"] = GuestRank,
            ["Genres"] = GuestRank,
            ["Genre"] = GuestRank,
            ["Platforms"] = GuestRank,
            ["Platform"] = GuestRank,
            ["Publishers"] = GuestRank,
            ["Publisher"] = GuestRank,

            ["Basket"] = UserRank,
            ["MakeOrder"] = UserRank,

            ["AddGame"] = ManagerRank,
            ["UpdateGame"] = ManagerRank,
            ["DeleteGame"] = ManagerRank,
            ["AddGenre"] = ManagerRank,
            ["UpdateGenre"] = ManagerRank,
            ["DeleteGenre"] = ManagerRank,
            ["AddPlatform"] = ManagerRank,
            ["UpdatePlatform"] = ManagerRank,
            ["DeletePlatform"] = ManagerRank,
            ["AddPublisher"] = ManagerRank,
            ["UpdatePublisher"] = ManagerRank,
            ["DeletePublisher"] = ManagerRank,
            ["Orders"] = ManagerRank,
            ["Order"] = ManagerRank,
            ["UpdateOrder"] = ManagerRank,
            ["History"] = ManagerRank,

            ["Users"] = AdministratorRank,
            ["User"] = AdministratorRank,
            ["AddUser"] = AdministratorRank,
            ["UpdateUser"] = AdministratorRank,
            ["DeleteUser"] = AdministratorRank,
            ["Roles"] = AdministratorRank,
            ["Role"] = AdministratorRank,
            ["AddRole"] = AdministratorRank,
            ["UpdateRole"] = AdministratorRank,
            ["DeleteRole"] = AdministratorRank,
        };

    public bool HasAccess(string targetPage, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(targetPage)
            || !PageMinimumRoleRanks.TryGetValue(targetPage.Trim(), out var minimumRoleRank))
        {
            return false;
        }

        var highestRoleRank = GuestRank;

        foreach (var role in roles)
        {
            if (RoleRanks.TryGetValue(role, out var roleRank) && roleRank > highestRoleRank)
            {
                highestRoleRank = roleRank;
            }
        }

        return highestRoleRank >= minimumRoleRank;
    }
}
