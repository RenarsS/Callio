namespace Callio.Core.Domain.Exceptions;

public class InvalidFieldException : Exception
{
    public InvalidFieldException(string field) : base($"{field} value is invalid.")
    {
        
    }
}