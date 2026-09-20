namespace Gamestore.BLL.Exceptions;

public class ForbiddenException(string message) : Exception(message);