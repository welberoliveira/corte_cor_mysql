using MySqlConnector;

namespace CorteCor.Handlers;

internal static class MySqlOperationalResilience
{
    public static bool IsMaxUserConnections(Exception? exception)
    {
        while (exception != null)
        {
            if (exception is MySqlException mySqlException)
            {
                if (mySqlException.Number == 1203)
                {
                    return true;
                }

                if (ContainsConnectionLimitMessage(mySqlException.Message))
                {
                    return true;
                }
            }
            else if (ContainsConnectionLimitMessage(exception.Message))
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    private static bool ContainsConnectionLimitMessage(string? message) =>
        !string.IsNullOrWhiteSpace(message) &&
        (
            message.Contains("max_user_connections", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("too many user connections", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("already has more than", StringComparison.OrdinalIgnoreCase)
        );
}
