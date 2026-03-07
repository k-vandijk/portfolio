namespace Dashboard.Domain.Exceptions;

public class ValidationException : Exception
{
    public ValidationException() : base("The provided data is invalid.") { }
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
