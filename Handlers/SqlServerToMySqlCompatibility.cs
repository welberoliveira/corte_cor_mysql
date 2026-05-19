using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace CorteCor.Handlers;

internal sealed class CompatibleDbConnection : DbConnection
{
    private readonly DbConnection _inner;

    public CompatibleDbConnection(DbConnection inner)
    {
        _inner = inner;
    }

    public override string ConnectionString
    {
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value;
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;
    public override int ConnectionTimeout => _inner.ConnectionTimeout;

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    public override void Close() => _inner.Close();

    public override void Open() => _inner.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new CompatibleDbTransaction(_inner.BeginTransaction(isolationLevel), this);

    protected override DbCommand CreateDbCommand() =>
        new CompatibleDbCommand(_inner.CreateCommand(), this);

    internal DbConnection Inner => _inner;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }
}

internal sealed class CompatibleDbTransaction : DbTransaction
{
    private readonly DbTransaction _inner;
    private readonly CompatibleDbConnection _connection;

    public CompatibleDbTransaction(DbTransaction inner, CompatibleDbConnection connection)
    {
        _inner = inner;
        _connection = connection;
    }

    public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

    protected override DbConnection DbConnection => _connection;

    public override void Commit() => _inner.Commit();

    public override void Rollback() => _inner.Rollback();

    internal DbTransaction Inner => _inner;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }
}

internal sealed class CompatibleDbCommand : DbCommand
{
    private readonly DbCommand _inner;
    private CompatibleDbConnection? _connection;
    private string? _commandText;

    public CompatibleDbCommand(DbCommand inner, CompatibleDbConnection? connection = null)
    {
        _inner = inner;
        _connection = connection;
    }

    public override string CommandText
    {
        get => _commandText ?? _inner.CommandText;
        set => _commandText = value;
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _inner.DesignTimeVisible;
        set => _inner.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set
        {
            _connection = value as CompatibleDbConnection;
            _inner.Connection = _connection?.Inner ?? value;
        }
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value is CompatibleDbTransaction compatible ? compatible.Inner : value;
    }

    public override void Cancel() => _inner.Cancel();

    public override int ExecuteNonQuery()
    {
        ApplyTranslatedSql();
        return _inner.ExecuteNonQuery();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ApplyTranslatedSql();
        return await _inner.ExecuteNonQueryAsync(cancellationToken);
    }

    public override object? ExecuteScalar()
    {
        ApplyTranslatedSql();
        return _inner.ExecuteScalar();
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        ApplyTranslatedSql();
        return await _inner.ExecuteScalarAsync(cancellationToken);
    }

    public override void Prepare()
    {
        ApplyTranslatedSql();
        _inner.Prepare();
    }

    public override Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        ApplyTranslatedSql();
        return _inner.PrepareAsync(cancellationToken);
    }

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        ApplyTranslatedSql();
        return _inner.ExecuteReader(behavior);
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        ApplyTranslatedSql();
        return await _inner.ExecuteReaderAsync(behavior, cancellationToken);
    }

    private void ApplyTranslatedSql()
    {
        _inner.CommandText = SqlServerToMySqlTranslator.Translate(_commandText ?? _inner.CommandText);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }
}

internal static class CompatibilityWrappers
{
    public static DbConnection Wrap(IDbConnection connection)
    {
        if (connection is CompatibleDbConnection compatible)
        {
            return compatible;
        }

        if (connection is DbConnection dbConnection)
        {
            return new CompatibleDbConnection(dbConnection);
        }

        throw new InvalidOperationException("A conexao precisa herdar de DbConnection para suportar operacoes assincronas.");
    }
}

internal static class SqlServerToMySqlTranslator
{
    private static readonly Regex IsNullRegex = new(@"\bISNULL\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GetDateRegex = new(@"\bGETDATE\s*\(\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScopeIdentityRegex = new(@"\bSCOPE_IDENTITY\s*\(\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OffsetFetchRegex = new(@"\bOFFSET\s+(?<offset>@?\w+)\s+ROWS\s+FETCH\s+NEXT\s+(?<take>@?\w+)\s+ROWS\s+ONLY\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateFromPartsRegex = new(@"\bDATEFROMPARTS\s*\(\s*YEAR\((?<expr>[^)]+)\)\s*,\s*MONTH\((?<expr2>[^)]+)\)\s*,\s*1\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConvertDate112Regex = new(@"CONVERT\s*\(\s*datetime\s*,\s*'(?<value>\d{8})'\s*,\s*112\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CastVarcharRegex = new(@"CAST\s*\(\s*(?<expr>.*?)\s+AS\s+varchar\((?<len>\d+)\)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CastNVarCharRegex = new(@"CAST\s*\(\s*(?<expr>.*?)\s+AS\s+nvarchar\((?<len>\d+)\)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CastDateRegex = new(@"CAST\s*\(\s*(?<expr>.*?)\s+AS\s+date\s*\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CastBitRegex = new(@"CAST\s*\(\s*(?<expr>.*?)\s+AS\s+bit\s*\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CastIntegerRegex = new(@"CAST\s*\(\s*(?<expr>.*?)\s+AS\s+(?:int|integer|bigint)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex DateAddRegex = new(@"\bDATEADD\s*\(\s*(?<part>day|minute)\s*,\s*(?<value>[^,]+?)\s*,\s*(?<expr>[^)]+?)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DateDiffRegex = new(@"\bDATEDIFF\s*\(\s*(?<part>day)\s*,\s*(?<start>[^,]+?)\s*,\s*(?<end>[^)]+?)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LikeContainsRegex = new(@"LIKE\s+'%'\s*\+\s*(?<param>@\w+)\s*\+\s*'%'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StringConcatLiteralRegex = new(@"'(?<text>(?:''|[^'])*)'\s*\+\s*(?<expr>[A-Za-z_][A-Za-z0-9_\.]*)", RegexOptions.Compiled);
    private static readonly Regex VarcharConvertRegex = new(@"CONVERT\s*\(\s*varchar\((?<len>\d+)\)\s*,\s*(?<expr>[^,]+?)\s*,\s*(?<style>\d+)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TopRegex = new(@"\bSELECT\s+TOP\s*(\(\s*(?<param>@\w+)\s*\)|(?<literal>\d+))\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Translate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        var translated = sql;
        translated = translated.Replace("[Usuario]", "`Usuario`", StringComparison.OrdinalIgnoreCase);
        translated = Regex.Replace(translated, @"\[(?<name>[^\]]+)\]", "`${name}`", RegexOptions.Compiled);
        translated = translated.Replace("dbo.", string.Empty, StringComparison.OrdinalIgnoreCase);
        translated = IsNullRegex.Replace(translated, "IFNULL(");
        translated = GetDateRegex.Replace(translated, "NOW()");
        translated = ScopeIdentityRegex.Replace(translated, "LAST_INSERT_ID()");
        translated = DateFromPartsRegex.Replace(translated, m => $"STR_TO_DATE(DATE_FORMAT({m.Groups["expr"].Value}, '%Y-%m-01'), '%Y-%m-%d')");
        translated = ConvertDate112Regex.Replace(translated, m => $"STR_TO_DATE('{m.Groups["value"].Value}', '%Y%m%d')");
        translated = translated.Replace("DATEADD(minute, DATEDIFF(minute, 0, s.Duracao), a.DataHora)", "DATE_ADD(a.DataHora, INTERVAL FLOOR(TIME_TO_SEC(s.Duracao) / 60) MINUTE)", StringComparison.OrdinalIgnoreCase);
        translated = DateDiffRegex.Replace(translated, m => $"TIMESTAMPDIFF({m.Groups["part"].Value.ToUpperInvariant()}, {m.Groups["start"].Value}, {m.Groups["end"].Value})");
        translated = DateAddRegex.Replace(translated, m =>
        {
            var expr = m.Groups["expr"].Value;
            var rawValue = m.Groups["value"].Value.Trim();
            var part = m.Groups["part"].Value.ToUpperInvariant();

            if (rawValue.StartsWith("-", StringComparison.Ordinal))
            {
                return $"DATE_SUB({expr}, INTERVAL {rawValue.TrimStart('-').Trim()} {part})";
            }

            return $"DATE_ADD({expr}, INTERVAL {rawValue} {part})";
        });
        translated = OffsetFetchRegex.Replace(translated, "LIMIT ${take} OFFSET ${offset}");
        translated = LikeContainsRegex.Replace(translated, "LIKE CONCAT('%', ${param}, '%')");
        translated = StringConcatLiteralRegex.Replace(translated, m => $"CONCAT('{m.Groups["text"].Value}', {m.Groups["expr"].Value})");
        translated = CastVarcharRegex.Replace(translated, m => $"CAST({m.Groups["expr"].Value} AS CHAR({m.Groups["len"].Value}))");
        translated = CastNVarCharRegex.Replace(translated, m => $"CAST({m.Groups["expr"].Value} AS CHAR({m.Groups["len"].Value}))");
        translated = CastDateRegex.Replace(translated, m => $"DATE({m.Groups["expr"].Value})");
        translated = CastBitRegex.Replace(translated, m => $"CAST({m.Groups["expr"].Value} AS UNSIGNED)");
        translated = CastIntegerRegex.Replace(translated, m => $"CAST({m.Groups["expr"].Value} AS SIGNED)");
        translated = VarcharConvertRegex.Replace(translated, ConvertVarcharExpression);
        translated = RewriteTopClauses(translated);
        return translated;
    }

    private static string ConvertVarcharExpression(Match match)
    {
        var len = match.Groups["len"].Value;
        var expr = match.Groups["expr"].Value.Trim();
        var style = match.Groups["style"].Value;

        return style switch
        {
            "103" when len == "10" => $"DATE_FORMAT({expr}, '%d/%m/%Y')",
            "103" when len == "16" => $"DATE_FORMAT({expr}, '%d/%m/%Y %H:%i')",
            _ when len == "5" => $"DATE_FORMAT({expr}, '%H:%i')",
            _ => $"CAST({expr} AS CHAR({len}))"
        };
    }

    private static string RewriteTopClauses(string sql)
    {
        var matches = TopRegex.Matches(sql);
        if (matches.Count == 0)
        {
            return sql;
        }

        var builder = new StringBuilder(sql);
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            var limit = match.Groups["param"].Success ? match.Groups["param"].Value : match.Groups["literal"].Value;
            builder.Remove(match.Index, match.Length);
            builder.Insert(match.Index, "SELECT ");

            var statementStart = match.Index;
            var depth = 0;
            var inString = false;
            var insertAt = builder.Length;

            for (var j = 0; j < statementStart; j++)
            {
                var ch = builder[j];
                if (ch == '\'')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (ch == '(') depth++;
                    else if (ch == ')') depth--;
                }
            }

            var selectDepth = depth;
            inString = false;

            for (var j = match.Index + "SELECT ".Length; j < builder.Length; j++)
            {
                var ch = builder[j];
                if (ch == '\'')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')')
                {
                    if (depth == selectDepth)
                    {
                        insertAt = j;
                        break;
                    }

                    depth--;
                    continue;
                }

                if (ch == ';' && depth == selectDepth)
                {
                    insertAt = j;
                    break;
                }
            }

            builder.Insert(insertAt, $" LIMIT {limit}");
        }

        return builder.ToString();
    }
}
