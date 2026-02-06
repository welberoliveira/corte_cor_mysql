using System;
using System.Data.SqlClient;

public class DatabaseHandler
{
    private const string ConnectionString = "Server=websql3.internetbrasil.net;Database=tonni;User Id=tonni;Password=bW3M*60ZccuD;";
    private readonly Log _logger = new Log();

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
