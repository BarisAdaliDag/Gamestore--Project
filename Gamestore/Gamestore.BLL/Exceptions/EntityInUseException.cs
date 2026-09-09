namespace Gamestore.BLL.Exceptions;

public class EntityInUseException(string entityName, object identifier)
    : Exception($"{entityName} '{identifier}' cannot be deleted because it is in use.")
{
}
