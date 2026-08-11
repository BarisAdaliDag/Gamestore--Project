namespace Gamestore.BLL.Exceptions;

public class EntityAlreadyExistsException(string entityName, string propertyName, object value)
    : Exception($"{entityName} with {propertyName} '{value}' already exists.")
{
}
