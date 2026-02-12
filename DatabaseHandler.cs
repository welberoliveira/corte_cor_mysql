using System;
using System.Data.SqlClient;

using Microsoft.Extensions.Configuration;
using System.IO;

public class DatabaseHandler
{
    private readonly string ConnectionString;
    private readonly Log _logger = new Log();

    public DatabaseHandler()
    {
        var updatedPath = AppDomain.CurrentDomain.BaseDirectory;
        // Ajuste para quando rodar local vs publicado, se necessário.
        // Mas o appsettings.json costuma estar no BaseDirectory.

        var builder = new ConfigurationBuilder()
            .SetBasePath(updatedPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var config = builder.Build();
        ConnectionString = config.GetConnectionString("DefaultConnection") 
                           ?? "Server=websql3.internetbrasil.net;Database=tonni;User Id=tonni;Password=bW3M*60ZccuD;";
    }

    public SqlConnection GetConnection()
    {
        try
        {
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
        using var command = new SqlCommand(query, connection);
        try
        {
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Write($"Error executing query: {ex.Message}");
            throw;
        }
    }
}
