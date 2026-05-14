using System.Data;
using Dapper;
using CorteCor.Models;

namespace CorteCor.Handlers;

public class PedidoHandler
{
    private readonly IDatabaseHandler _databaseHandler;

    public PedidoHandler(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    private IDbConnection GetConnection() => _databaseHandler.GetConnection();

    public async Task<int> CriarPedidoAsync(Pedido pedido, IReadOnlyCollection<PedidoItem> itens)
    {
        const string insertPedidoSql = @"
INSERT INTO CorteCor_Pedido
    (IdSalao, IdPessoa, IdMeioPagamento, Status, TipoPagamento, ValidoAte,
     SubtotalProdutos, SubtotalServicos, Desconto, Acrescimo, ValorTotal,
     Observacoes, Origem, UsuarioOperador, IdVendaProduto, DataPedido, DataCriacao, DataAtualizacao)
VALUES
    (@IdSalao, @IdPessoa, @IdMeioPagamento, @Status, @TipoPagamento, @ValidoAte,
     @SubtotalProdutos, @SubtotalServicos, @Desconto, @Acrescimo, @ValorTotal,
     @Observacoes, @Origem, @UsuarioOperador, @IdVendaProduto, @DataPedido, NOW(), NOW());
SELECT LAST_INSERT_ID();";

        const string insertItemSql = @"
INSERT INTO CorteCor_PedidoItem
    (IdPedido, IdSalao, TipoItem, IdProduto, IdServico, Descricao, Quantidade, ValorUnitario,
     ValorTotal, Unidade, ControlaEstoque, CodigoTributacaoMunicipio, AliquotaIss, Ncm, Cfop)
VALUES
    (@IdPedido, @IdSalao, @TipoItem, @IdProduto, @IdServico, @Descricao, @Quantidade, @ValorUnitario,
     @ValorTotal, @Unidade, @ControlaEstoque, @CodigoTributacaoMunicipio, @AliquotaIss, @Ncm, @Cfop);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var idPedido = await conn.ExecuteScalarAsync<int>(insertPedidoSql, pedido, tx);
            foreach (var item in itens)
            {
                item.IdPedido = idPedido;
                item.IdSalao = pedido.IdSalao;
                await conn.ExecuteAsync(insertItemSql, item, tx);
            }

            tx.Commit();
            return idPedido;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<Pedido?> ObterPedidoAsync(int idSalao, int idPedido)
    {
        const string sql = @"
SELECT
    P.*,
    Cli.Nome AS NomeCliente,
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM CorteCor_PedidoItem I
        WHERE I.IdSalao = P.IdSalao
          AND I.IdPedido = P.IdPedido
          AND I.TipoItem = 'Servico'
    ) THEN 1 ELSE 0 END AS UNSIGNED) AS PossuiServico,
    (
        SELECT COUNT(1)
        FROM CorteCor_PedidoItem I
        WHERE I.IdSalao = P.IdSalao
          AND I.IdPedido = P.IdPedido
    ) AS QuantidadeItens,
    (
        SELECT N.Status
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = P.IdSalao
          AND N.IdVendaProduto = P.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS StatusFiscal
FROM CorteCor_Pedido P
LEFT JOIN CorteCor_Pessoa Cli ON Cli.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND P.IdPedido = @IdPedido
LIMIT 1;";

        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Pedido>(sql, new { IdSalao = idSalao, IdPedido = idPedido });
    }

    public async Task<List<PedidoItem>> ListarItensPedidoAsync(int idSalao, int idPedido)
    {
        const string sql = @"
SELECT
    IdItemPedido,
    IdPedido,
    IdSalao,
    TipoItem,
    IdProduto,
    IdServico,
    Descricao,
    Quantidade,
    ValorUnitario,
    ValorTotal,
    Unidade,
    ControlaEstoque,
    CodigoTributacaoMunicipio,
    AliquotaIss,
    Ncm,
    Cfop
FROM CorteCor_PedidoItem
WHERE IdSalao = @IdSalao
  AND IdPedido = @IdPedido
ORDER BY IdItemPedido;";

        using var conn = GetConnection();
        return (await conn.QueryAsync<PedidoItem>(sql, new { IdSalao = idSalao, IdPedido = idPedido })).ToList();
    }

    public async Task<PagedResult<Pedido>> ListarPedidosAsync(int idSalao, PedidoFiltro filtro)
    {
        var normalized = filtro ?? new PedidoFiltro();
        normalized.PageIndex = normalized.PageIndex <= 0 ? 1 : normalized.PageIndex;
        normalized.PageSize = normalized.PageSize <= 0 ? 10 : normalized.PageSize;
        var offset = (normalized.PageIndex - 1) * normalized.PageSize;

        const string baseSql = @"
FROM CorteCor_Pedido P
LEFT JOIN CorteCor_Pessoa Cli ON Cli.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND (@IdPessoa IS NULL OR P.IdPessoa = @IdPessoa)
  AND (@Status IS NULL OR P.Status = @Status)
  AND (
        @Pesquisa IS NULL
        OR Cli.Nome LIKE @Pesquisa
        OR COALESCE(P.TipoPagamento, '') LIKE COALESCE(@Pesquisa, '')
        OR CAST(P.IdPedido AS CHAR(20)) LIKE COALESCE(@Pesquisa, '')
      )
  AND (@DataInicio IS NULL OR DATE(P.DataPedido) >= @DataInicio)
  AND (@DataFim IS NULL OR DATE(P.DataPedido) <= @DataFim)
  AND (@SomenteVigentes = 0 OR (P.Status = 'Aberto' AND P.ValidoAte >= CURDATE()))";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";

        const string selectSql = @"
SELECT
    P.*,
    Cli.Nome AS NomeCliente,
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM CorteCor_PedidoItem I
        WHERE I.IdSalao = P.IdSalao
          AND I.IdPedido = P.IdPedido
          AND I.TipoItem = 'Servico'
    ) THEN 1 ELSE 0 END AS UNSIGNED) AS PossuiServico,
    (
        SELECT COUNT(1)
        FROM CorteCor_PedidoItem I
        WHERE I.IdSalao = P.IdSalao
          AND I.IdPedido = P.IdPedido
    ) AS QuantidadeItens,
    (
        SELECT N.Status
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = P.IdSalao
          AND N.IdVendaProduto = P.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS StatusFiscal
" + baseSql + @"
ORDER BY P.DataPedido DESC, P.IdPedido DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = GetConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@IdSalao", idSalao);
        parameters.Add("@IdPessoa", normalized.IdPessoa);
        parameters.Add("@Status", string.IsNullOrWhiteSpace(normalized.Status) ? null : normalized.Status.Trim());
        parameters.Add("@Pesquisa", string.IsNullOrWhiteSpace(normalized.Pesquisa) ? null : $"%{normalized.Pesquisa.Trim()}%");
        parameters.Add("@DataInicio", normalized.DataInicio?.Date);
        parameters.Add("@DataFim", normalized.DataFim?.Date);
        parameters.Add("@SomenteVigentes", normalized.SomenteVigentes);
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", normalized.PageSize);

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await conn.QueryAsync<Pedido>(selectSql, parameters)).ToList();

        return new PagedResult<Pedido>
        {
            Items = items,
            TotalCount = total,
            PageIndex = normalized.PageIndex,
            PageSize = normalized.PageSize
        };
    }

    public async Task AtualizarPedidosVencidosAsync(int idSalao)
    {
        const string sql = @"
UPDATE CorteCor_Pedido
SET Status = @StatusVencido,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND Status = @StatusAberto
  AND ValidoAte < CURDATE();";

        using var conn = GetConnection();
        await conn.ExecuteAsync(sql, new
        {
            IdSalao = idSalao,
            StatusAberto = PedidoStatus.Aberto,
            StatusVencido = PedidoStatus.Vencido
        });
    }

    public async Task CancelarPedidoAsync(int idSalao, int idPedido, string? usuario, string? observacao)
    {
        const string sql = @"
UPDATE CorteCor_Pedido
SET Status = @Status,
    UsuarioOperador = @UsuarioOperador,
    Observacoes = CASE
        WHEN @Observacoes IS NULL OR @Observacoes = '' THEN Observacoes
        WHEN Observacoes IS NULL OR Observacoes = '' THEN @Observacoes
        ELSE CONCAT(Observacoes, CHAR(13), CHAR(10), @Observacoes)
    END,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdPedido = @IdPedido
  AND Status NOT IN (@StatusConvertido, @StatusCancelado);";

        using var conn = GetConnection();
        await conn.ExecuteAsync(sql, new
        {
            IdSalao = idSalao,
            IdPedido = idPedido,
            Status = PedidoStatus.Cancelado,
            StatusConvertido = PedidoStatus.Convertido,
            StatusCancelado = PedidoStatus.Cancelado,
            UsuarioOperador = usuario,
            Observacoes = observacao
        });
    }

    public async Task MarcarPedidoComoConvertidoAsync(int idSalao, int idPedido, int idVendaProduto, string? usuario, string? observacao)
    {
        const string sql = @"
UPDATE CorteCor_Pedido
SET Status = @Status,
    IdVendaProduto = @IdVendaProduto,
    UsuarioOperador = @UsuarioOperador,
    Observacoes = CASE
        WHEN @Observacoes IS NULL OR @Observacoes = '' THEN Observacoes
        WHEN Observacoes IS NULL OR Observacoes = '' THEN @Observacoes
        ELSE CONCAT(Observacoes, CHAR(13), CHAR(10), @Observacoes)
    END,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdPedido = @IdPedido
  AND Status NOT IN (@StatusConvertido, @StatusCancelado);";

        using var conn = GetConnection();
        await conn.ExecuteAsync(sql, new
        {
            IdSalao = idSalao,
            IdPedido = idPedido,
            IdVendaProduto = idVendaProduto,
            Status = PedidoStatus.Convertido,
            StatusConvertido = PedidoStatus.Convertido,
            StatusCancelado = PedidoStatus.Cancelado,
            UsuarioOperador = usuario,
            Observacoes = observacao
        });
    }
}
