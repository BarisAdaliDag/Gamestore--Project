namespace Gamestore.Domain.Constants;

/// <summary>
/// Payment method names supported by the game store.
/// These values are part of the public API contract, so they must match
/// the names expected by the front-end and the epic specification exactly.
/// </summary>
public static class PaymentMethods
{
    public const string Bank = "Bank";

    public const string IBoxTerminal = "IBox terminal";

    public const string Visa = "Visa";

    public static readonly IReadOnlyCollection<string> All = [Bank, IBoxTerminal, Visa];
}
