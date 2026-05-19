using System.Data;
using CorteCor.Logs;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace CorteCor.Handlers;

public interface IDatabaseHandler
{
    IDbConnection GetConnection();
}

public class DatabaseHandler : IDatabaseHandler
{
    private readonly string _connectionString;
    private readonly Log _logger = new();

    public DatabaseHandler(IConfiguration? configuration = null)
    {
        var config = configuration ?? AppConfigurationFactory.Build();

        var configuredConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? config.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__TonniDb")
            ?? config.GetConnectionString("TonniDb")
            ?? string.Empty;

        _connectionString = ResolverConnectionString(configuredConnectionString);
    }

    public IDbConnection GetConnection()
    {
        try
        {
            ValidarConnectionString(_connectionString);
            var normalizedConnectionString = NormalizeMySqlConnectionString(_connectionString);
            var connection = new MySqlConnection(normalizedConnectionString);
            connection.Open();

            return CompatibilityWrappers.Wrap(connection);
        }
        catch (Exception ex)
        {
            _logger.Write($"Erro ao conectar ao banco de dados: {ex.Message}");
            throw new DatabaseConnectionException("Ocorreu um erro.", ex);
        }
    }

    public void ExecuteQuery(string query, params IDataParameter[] parameters)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = query;
        try
        {
            foreach (var parameter in parameters ?? Array.Empty<IDataParameter>())
            {
                var param = command.CreateParameter();
                param.ParameterName = parameter.ParameterName;
                param.Value = parameter.Value;
                command.Parameters.Add(param);
            }

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Write($"Error executing query: {ex.Message}");
            throw;
        }
    }

    public static string ResolverConnectionString(string? connectionString) =>
        string.IsNullOrWhiteSpace(connectionString) ||
        connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : connectionString;

    public static string NormalizeMySqlConnectionString(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);

        if (!HasOption(connectionString, "Pooling"))
        {
            builder.Pooling = true;
        }

        if (builder.Pooling && !HasOption(connectionString, "MaximumPoolSize", "Maximum Pool Size", "MaxPoolSize", "Max Pool Size"))
        {
            builder.MaximumPoolSize = 10;
        }

        if (builder.Pooling && !HasOption(connectionString, "MinimumPoolSize", "Minimum Pool Size", "MinPoolSize", "Min Pool Size"))
        {
            builder.MinimumPoolSize = 0;
        }

        if (builder.Pooling && !HasOption(connectionString, "ConnectionIdleTimeout", "Connection Idle Timeout"))
        {
            builder.ConnectionIdleTimeout = 30;
        }

        if (builder.Pooling && !HasOption(connectionString, "ConnectionLifeTime", "Connection Lifetime"))
        {
            builder.ConnectionLifeTime = 180;
        }

        if (builder.ConnectionTimeout == 0)
        {
            builder.ConnectionTimeout = 60;
        }

        if (builder.DefaultCommandTimeout < 180)
        {
            builder.DefaultCommandTimeout = 180;
        }

        builder.AllowUserVariables = true;
        if (!HasOption(connectionString, "Character Set", "CharSet"))
        {
            builder.CharacterSet = "utf8mb4";
        }

        if (builder.SslMode == MySqlSslMode.None)
        {
            builder.SslMode = MySqlSslMode.Preferred;
        }

        return builder.ConnectionString;
    }

    private static bool HasOption(string connectionString, params string[] optionNames)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || optionNames.Length == 0)
        {
            return false;
        }

        var normalizedOptionNames = optionNames
            .Select(NormalizeOptionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = NormalizeOptionName(part[..separatorIndex]);
            if (normalizedOptionNames.Contains(key))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeOptionName(string optionName) =>
        optionName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static void ValidarConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string do banco nao foi configurada. Defina ConnectionStrings__DefaultConnection " +
                "ou preencha DefaultConnection em appsettings.Local.json.");
        }
    }
}
