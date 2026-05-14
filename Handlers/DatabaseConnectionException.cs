namespace CorteCor.Handlers;

public sealed class DatabaseConnectionException : Exception
{
    public DatabaseConnectionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
