namespace Lanflix.Application.Common.Exceptions;

public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("One or more validation errors occurred")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}
