using CorteCor.Logs;
using System;
using System.Data.SqlClient;

using Microsoft.Extensions.Configuration;
using System.IO;

using System.Data;

namespace CorteCor.Handlers
{

public interface IDatabaseHandler
{
    IDbConnection GetConnection();
}

public class DatabaseHandler : IDatabaseHandler
{
    private const string LegacyFallbackConnectionString =
        "Server=websql3.internetbrasil.net;Database=tonni;User Id=tonni;Password=bW3M*60ZccuD;";

    private readonly string ConnectionString;
    private readonly Log _logger = new Log();
    private readonly IConfiguration? _configuration;

    public DatabaseHandler(IConfiguration? configuration = null)
    {
        _configuration = configuration;
        var updatedPath = AppDomain.CurrentDomain.BaseDirectory;
        var config = _configuration ?? new ConfigurationBuilder()
            .SetBasePath(updatedPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var configuredConnectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? config.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__TonniDb")
            ?? config.GetConnectionString("TonniDb")
            ?? string.Empty;

        ConnectionString = ResolverConnectionString(configuredConnectionString);
    }

    public IDbConnection GetConnection()
    {
        try
        {
            ValidarConnectionString(ConnectionString);
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.Write($"Error connecting to the database: {ex.Message}");
            throw;
        }
    }

    public void ExecuteQuery(string query, params SqlParameter[] parameters)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = query;
        try
        {
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = p.ParameterName;
                    param.Value = p.Value;
                    command.Parameters.Add(param);
                }
            }
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Write($"Error executing query: {ex.Message}");
            throw;
        }
    }

    public static string ResolverConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            return LegacyFallbackConnectionString;
        }

        return connectionString;
    }

    private static void ValidarConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string do banco nao foi configurada. Defina ConnectionStrings__DefaultConnection " +
                "ou preencha DefaultConnection em appsettings.Development.json.");
        }
    }
}
}

