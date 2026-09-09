namespace Gamestore.BLL.Exceptions;

public sealed class OutOfStockException(string message) : Exception(message);
