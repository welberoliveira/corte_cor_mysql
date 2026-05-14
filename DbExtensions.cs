using System;
using System.Data;

public static class DbExtensions
{
    public static IDbCommand CreateCommand(this IDbConnection connection, string commandText)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        return cmd;
    }

    public static void AddWithValue(this IDbCommand command, string parameterName, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    public static void AddWithBinaryValue(this IDbCommand command, string parameterName, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value ?? DBNull.Value;
        parameter.DbType = DbType.Binary;
        command.Parameters.Add(parameter);
    }
}
