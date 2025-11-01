namespace Lanflix.Application.Common.Exceptions;

public class NotFoundException : ApplicationException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found")
    {
    }
}
