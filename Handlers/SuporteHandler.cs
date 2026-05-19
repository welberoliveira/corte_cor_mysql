using CorteCor.Models;
using Dapper;

namespace CorteCor.Handlers;

public class SuporteHandler
{
    private readonly IDatabaseHandler _databaseHandler;

    public SuporteHandler(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    public async Task RegistrarAsync(SuporteChamado chamado)
    {
        const string sql = @"
INSERT INTO CorteCor_SuporteChamado
    (IdChamado, IdSalao, NomeUsuario, EmailUsuario, Mensagem, UrlOrigem, Status, ErroEmail, DataCriacao, DataAtualizacao)
VALUES
    (@IdChamado, @IdSalao, @NomeUsuario, @EmailUsuario, @Mensagem, @UrlOrigem, @Status, @ErroEmail, NOW(), NOW());";

        using var conn = _databaseHandler.GetConnection();
        await conn.ExecuteAsync(sql, chamado);
    }

    public async Task AtualizarStatusAsync(int idSalao, Guid idChamado, string status, string? erroEmail)
    {
        const string sql = @"
UPDATE CorteCor_SuporteChamado
SET Status = @Status,
    ErroEmail = @ErroEmail,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdChamado = @IdChamado;";

        using var conn = _databaseHandler.GetConnection();
        await conn.ExecuteAsync(sql, new { IdSalao = idSalao, IdChamado = idChamado, Status = status, ErroEmail = erroEmail });
    }

    public async Task<PagedResult<SuporteChamado>> ListarAsync(int idSalao, SuporteChamadoFiltro filtro)
    {
        var normalizado = filtro ?? new SuporteChamadoFiltro();
        normalizado.PageIndex = normalizado.PageIndex <= 0 ? 1 : normalizado.PageIndex;
        normalizado.PageSize = normalizado.PageSize <= 0 ? 15 : normalizado.PageSize;
        var offset = (normalizado.PageIndex - 1) * normalizado.PageSize;

        const string baseSql = @"
FROM CorteCor_SuporteChamado
WHERE IdSalao = @IdSalao
  AND (@Status IS NULL OR Status = @Status)
  AND (@Pesquisa IS NULL OR NomeUsuario LIKE @Pesquisa OR EmailUsuario LIKE @Pesquisa OR Mensagem LIKE @Pesquisa OR UrlOrigem LIKE @Pesquisa OR CAST(IdChamado AS CHAR) LIKE @Pesquisa)";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";
        const string selectSql = @"
SELECT *
" + baseSql + @"
ORDER BY
    CASE
        WHEN Status = 'Solicitado' THEN 1
        WHEN Status = 'Em análise' THEN 2
        WHEN Status = 'Concluído' THEN 3
        WHEN Status = 'Cancelado' THEN 4
        ELSE 5
    END,
    DataAtualizacao DESC,
    DataCriacao DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = _databaseHandler.GetConnection();
        var parametros = new
        {
            IdSalao = idSalao,
            Status = string.IsNullOrWhiteSpace(normalizado.Status) ? null : normalizado.Status.Trim(),
            Pesquisa = string.IsNullOrWhiteSpace(normalizado.Pesquisa) ? null : $"%{normalizado.Pesquisa.Trim()}%",
            Offset = offset,
            normalizado.PageSize
        };

        var total = await conn.ExecuteScalarAsync<int>(countSql, parametros);
        var chamados = (await conn.QueryAsync<SuporteChamado>(selectSql, parametros)).ToList();

        return new PagedResult<SuporteChamado>
        {
            Items = chamados,
            TotalCount = total,
            PageIndex = normalizado.PageIndex,
            PageSize = normalizado.PageSize
        };
    }
}
