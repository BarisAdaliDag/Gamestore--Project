using System.Security.Cryptography;
using System.Text;

namespace Gamestore.BLL.Helpers;

public static class NorthwindReferenceIdentity
{
    public static string Normalize(string value)
    {
        return string.Join(
            ' ',
            value.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    public static Guid CreateSupplierId(string companyName)
    {
        return CreateStableGuid($"northwind-supplier:{Normalize(companyName)}");
    }

    public static Guid CreateCategoryId(string categoryName)
    {
        return CreateStableGuid($"northwind-category:{Normalize(categoryName)}");
    }

    private static Guid CreateStableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash[..16]);
    }
}
