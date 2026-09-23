namespace Gamestore.BLL.Exceptions;

public sealed class ExternalAuthenticationUnavailableException(Exception innerException)
    : Exception("The external authentication service is unavailable.", innerException);
