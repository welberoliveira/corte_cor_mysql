using System.Data;
using Dapper;
using CorteCor.Models;

namespace CorteCor.Handlers;

public class VendaEstoqueHandler
{
    private readonly IDatabaseHandler _databaseHandler;

    public VendaEstoqueHandler(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    private IDbConnection GetConnection() => _databaseHandler.GetConnection();

    public async Task<int> CriarVendaAsync(
        VendaProduto venda,
        IReadOnlyCollection<VendaProdutoItem> itens,
        IReadOnlyCollection<MovimentoEstoque> movimentos)
    {
        const string insertVendaSql = @"
INSERT INTO CorteCor_VendaProduto
    (IdSalao, IdPessoa, IdMeioPagamento, Status, TipoPagamento, Recorrencia, RecebidoNaHora, SolicitarEmissaoFiscalServico,
     SubtotalProdutos, SubtotalServicos, Desconto, Acrescimo, ValorTotal, Observacoes, Origem, UsuarioOperador,
     DataVenda, DataCriacao, DataAtualizacao)
VALUES
    (@IdSalao, @IdPessoa, @IdMeioPagamento, @Status, @TipoPagamento, @Recorrencia, @RecebidoNaHora, @SolicitarEmissaoFiscalServico,
     @SubtotalProdutos, @SubtotalServicos, @Desconto, @Acrescimo, @ValorTotal, @Observacoes, @Origem, @UsuarioOperador,
     @DataVenda, NOW(), NOW());
SELECT LAST_INSERT_ID();";

        const string insertItemSql = @"
INSERT INTO CorteCor_VendaProdutoItem
    (IdVendaProduto, IdSalao, TipoItem, IdProduto, IdServico, Descricao, Quantidade, ValorUnitario,
     ValorTotal, Unidade, ControlaEstoque, CodigoTributacaoMunicipio, AliquotaIss, Ncm, Cfop)
VALUES
    (@IdVendaProduto, @IdSalao, @TipoItem, @IdProduto, @IdServico, @Descricao, @Quantidade, @ValorUnitario,
     @ValorTotal, @Unidade, @ControlaEstoque, @CodigoTributacaoMunicipio, @AliquotaIss, @Ncm, @Cfop);";

        const string updateEstoqueSql = @"
UPDATE CorteCor_Produto
SET EstoqueAtual = @SaldoPosterior
WHERE IdProduto = @IdProduto
  AND IdSalao = @IdSalao;";

        const string insertMovimentoSql = @"
INSERT INTO CorteCor_MovimentoEstoque
    (IdMovimento, IdSalao, IdProduto, IdVendaProduto, TipoMovimento, Origem, Quantidade,
     SaldoAnterior, SaldoPosterior, Observacao, UsuarioOperador, DataMovimento)
VALUES
    (@IdMovimento, @IdSalao, @IdProduto, @IdVendaProduto, @TipoMovimento, @Origem, @Quantidade,
     @SaldoAnterior, @SaldoPosterior, @Observacao, @UsuarioOperador, @DataMovimento);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var idVendaProduto = await conn.ExecuteScalarAsync<int>(insertVendaSql, venda, tx);

            foreach (var item in itens)
            {
                item.IdVendaProduto = idVendaProduto;
                item.IdSalao = venda.IdSalao;
                await conn.ExecuteAsync(insertItemSql, item, tx);
            }

            foreach (var movimento in movimentos)
            {
                movimento.IdVendaProduto = idVendaProduto;
                await conn.ExecuteAsync(updateEstoqueSql, new
                {
                    movimento.IdProduto,
                    movimento.IdSalao,
                    movimento.SaldoPosterior
                }, tx);
                await conn.ExecuteAsync(insertMovimentoSql, movimento, tx);
            }

            tx.Commit();
            return idVendaProduto;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task AtualizarVendaAsync(VendaProduto venda, IReadOnlyCollection<VendaProdutoItem> itens)
    {
        const string updateVendaSql = @"
UPDATE CorteCor_VendaProduto
SET IdPessoa = @IdPessoa,
    IdMeioPagamento = @IdMeioPagamento,
    TipoPagamento = @TipoPagamento,
    Recorrencia = @Recorrencia,
    RecebidoNaHora = @RecebidoNaHora,
    SolicitarEmissaoFiscalServico = @SolicitarEmissaoFiscalServico,
    SubtotalProdutos = @SubtotalProdutos,
    SubtotalServicos = @SubtotalServicos,
    Desconto = @Desconto,
    Acrescimo = @Acrescimo,
    ValorTotal = @ValorTotal,
    Observacoes = @Observacoes,
    Origem = @Origem,
    UsuarioOperador = @UsuarioOperador,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string deleteItensSql = @"
DELETE FROM CorteCor_VendaProdutoItem
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string insertItemSql = @"
INSERT INTO CorteCor_VendaProdutoItem
    (IdVendaProduto, IdSalao, TipoItem, IdProduto, IdServico, Descricao, Quantidade, ValorUnitario,
     ValorTotal, Unidade, ControlaEstoque, CodigoTributacaoMunicipio, AliquotaIss, Ncm, Cfop)
VALUES
    (@IdVendaProduto, @IdSalao, @TipoItem, @IdProduto, @IdServico, @Descricao, @Quantidade, @ValorUnitario,
     @ValorTotal, @Unidade, @ControlaEstoque, @CodigoTributacaoMunicipio, @AliquotaIss, @Ncm, @Cfop);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(updateVendaSql, venda, tx);
            await conn.ExecuteAsync(deleteItensSql, new { venda.IdSalao, venda.IdVendaProduto }, tx);

            foreach (var item in itens)
            {
                item.IdVendaProduto = venda.IdVendaProduto;
                item.IdSalao = venda.IdSalao;
                await conn.ExecuteAsync(insertItemSql, item, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<VendaProduto?> ObterVendaAsync(int idSalao, int idVendaProduto)
    {
        const string sql = @"
SELECT
    V.*,
    P.Nome AS NomeCliente,
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
          AND N.Status NOT IN ('Cancelada', 'Rejeitada')
    ) THEN 1 ELSE 0 END AS UNSIGNED) AS PossuiNotaAtiva,
    (
        SELECT N.Status
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS StatusFiscal,
    (
        SELECT N.TipoNota
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS TipoDocumentoFiscal
FROM CorteCor_VendaProduto V
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = V.IdPessoa
WHERE V.IdSalao = @IdSalao
  AND V.IdVendaProduto = @IdVendaProduto
LIMIT 1;";

        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<VendaProduto>(sql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto });
    }

    public async Task<List<VendaProdutoItem>> ListarItensVendaAsync(int idSalao, int idVendaProduto)
    {
        const string sql = @"
SELECT
    IdItemVenda,
    IdVendaProduto,
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
    Cfop,
    COALESCE(QuantidadeCancelada, 0) AS QuantidadeCancelada,
    COALESCE(QuantidadeDevolvida, 0) AS QuantidadeDevolvida,
    COALESCE(QuantidadeTrocada, 0) AS QuantidadeTrocada
FROM CorteCor_VendaProdutoItem
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto
ORDER BY IdItemVenda;";

        using var conn = GetConnection();
        return (await conn.QueryAsync<VendaProdutoItem>(sql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto })).ToList();
    }

    public async Task<PagedResult<VendaProduto>> ListarVendasAsync(int idSalao, VendaProdutoFiltro filtro)
    {
        var normalized = filtro ?? new VendaProdutoFiltro();
        normalized.PageIndex = normalized.PageIndex <= 0 ? 1 : normalized.PageIndex;
        normalized.PageSize = normalized.PageSize <= 0 ? 12 : normalized.PageSize;

        var offset = (normalized.PageIndex - 1) * normalized.PageSize;

        const string baseSql = @"
FROM CorteCor_VendaProduto V
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = V.IdPessoa
WHERE V.IdSalao = @IdSalao
  AND (@IdPessoa IS NULL OR V.IdPessoa = @IdPessoa)
  AND (@Status IS NULL OR V.Status = @Status)
  AND (@Recorrencia IS NULL OR V.Recorrencia = @Recorrencia)
  AND (
        @Pesquisa IS NULL
        OR P.Nome LIKE @Pesquisa
        OR COALESCE(V.TipoPagamento, '') LIKE COALESCE(@Pesquisa, '')
        OR CAST(V.IdVendaProduto AS CHAR(20)) LIKE COALESCE(@Pesquisa, '')
      )
  AND (@DataInicio IS NULL OR DATE(V.DataVenda) >= @DataInicio)
  AND (@DataFim IS NULL OR DATE(V.DataVenda) <= @DataFim)";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";

        const string selectSql = @"
SELECT
    V.*,
    P.Nome AS NomeCliente,
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
          AND N.Status NOT IN ('Cancelada', 'Rejeitada')
    ) THEN 1 ELSE 0 END AS UNSIGNED) AS PossuiNotaAtiva,
    (
        SELECT N.Status
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS StatusFiscal,
    (
        SELECT N.TipoNota
        FROM CorteCor_NotaFiscal N
        WHERE N.IdSalao = V.IdSalao
          AND N.IdVendaProduto = V.IdVendaProduto
        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
        LIMIT 1
    ) AS TipoDocumentoFiscal
" + baseSql + @"
ORDER BY V.DataVenda DESC, V.IdVendaProduto DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = GetConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@IdSalao", idSalao);
        parameters.Add("@IdPessoa", normalized.IdPessoa);
        parameters.Add("@Status", string.IsNullOrWhiteSpace(normalized.Status) ? null : normalized.Status.Trim());
        parameters.Add("@Recorrencia", string.IsNullOrWhiteSpace(normalized.Recorrencia) ? null : normalized.Recorrencia.Trim());
        parameters.Add("@Pesquisa", string.IsNullOrWhiteSpace(normalized.Pesquisa) ? null : $"%{normalized.Pesquisa.Trim()}%");
        parameters.Add("@DataInicio", normalized.DataInicio?.Date);
        parameters.Add("@DataFim", normalized.DataFim?.Date);
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", normalized.PageSize);

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await conn.QueryAsync<VendaProduto>(selectSql, parameters)).ToList();

        return new PagedResult<VendaProduto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = normalized.PageIndex,
            PageSize = normalized.PageSize
        };
    }

    public async Task<PagedResult<MovimentoEstoque>> ListarMovimentosAsync(int idSalao, EstoqueMovimentoFiltro filtro)
    {
        var normalized = filtro ?? new EstoqueMovimentoFiltro();
        normalized.PageIndex = normalized.PageIndex <= 0 ? 1 : normalized.PageIndex;
        normalized.PageSize = normalized.PageSize <= 0 ? 15 : normalized.PageSize;
        var offset = (normalized.PageIndex - 1) * normalized.PageSize;

        const string baseSql = @"
FROM CorteCor_MovimentoEstoque M
INNER JOIN CorteCor_Produto P ON P.IdProduto = M.IdProduto
WHERE M.IdSalao = @IdSalao
  AND (@IdProduto IS NULL OR M.IdProduto = @IdProduto)
  AND (@TipoMovimento IS NULL OR M.TipoMovimento = @TipoMovimento)
  AND (
        @Pesquisa IS NULL
        OR P.Nome LIKE @Pesquisa
        OR COALESCE(M.Observacao, '') LIKE COALESCE(@Pesquisa, '')
        OR CAST(COALESCE(M.IdVendaProduto, 0) AS CHAR(20)) LIKE COALESCE(@Pesquisa, '')
      )
  AND (@DataInicio IS NULL OR DATE(M.DataMovimento) >= @DataInicio)
  AND (@DataFim IS NULL OR DATE(M.DataMovimento) <= @DataFim)";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";
        const string selectSql = @"
SELECT
    M.*,
    P.Nome AS NomeProduto
" + baseSql + @"
ORDER BY M.DataMovimento DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = GetConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@IdSalao", idSalao);
        parameters.Add("@IdProduto", normalized.IdProduto);
        parameters.Add("@TipoMovimento", string.IsNullOrWhiteSpace(normalized.TipoMovimento) ? null : normalized.TipoMovimento.Trim());
        parameters.Add("@Pesquisa", string.IsNullOrWhiteSpace(normalized.Pesquisa) ? null : $"%{normalized.Pesquisa.Trim()}%");
        parameters.Add("@DataInicio", normalized.DataInicio?.Date);
        parameters.Add("@DataFim", normalized.DataFim?.Date);
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", normalized.PageSize);

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await conn.QueryAsync<MovimentoEstoque>(selectSql, parameters)).ToList();

        return new PagedResult<MovimentoEstoque>
        {
            Items = items,
            TotalCount = total,
            PageIndex = normalized.PageIndex,
            PageSize = normalized.PageSize
        };
    }

    public Task<PagedResult<ProdutoEstoquePosicao>> ListarPosicaoEstoqueAsync(int idSalao, string? pesquisa, int pageIndex, int pageSize) =>
        ListarPosicaoEstoqueAsync(idSalao, pesquisa, false, pageIndex, pageSize);

    public async Task<PagedResult<ProdutoEstoquePosicao>> ListarPosicaoEstoqueAsync(int idSalao, string? pesquisa, bool somenteBaixo, int pageIndex, int pageSize)
    {
        pageIndex = pageIndex <= 0 ? 1 : pageIndex;
        pageSize = pageSize <= 0 ? 15 : pageSize;
        var offset = (pageIndex - 1) * pageSize;

        const string baseSql = @"
FROM CorteCor_Produto P
LEFT JOIN CorteCor_CategoriaProduto C ON C.IdCategoria = P.IdCategoria
WHERE P.IdSalao = @IdSalao
  AND (P.Excluido = 0 OR P.Excluido IS NULL)
  AND (@Pesquisa IS NULL OR P.Nome LIKE @Pesquisa OR COALESCE(P.CodigoProprio, '') LIKE COALESCE(@Pesquisa, ''))
  AND (@SomenteBaixo = 0 OR (
        COALESCE(P.ControlarEstoque, 0) = 1
        AND COALESCE(P.EstoqueAtual, 0) <= COALESCE(P.EstoqueMinimo, 0)
      ))";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";
        const string selectSql = @"
SELECT
    P.IdProduto,
    P.Nome,
    P.CodigoProprio,
    C.Nome AS CategoriaNome,
    COALESCE(P.ControlarEstoque, 0) AS ControlarEstoque,
    COALESCE(P.EstoqueAtual, 0) AS EstoqueAtual,
    COALESCE(P.EstoqueMinimo, 0) AS EstoqueMinimo,
    COALESCE(P.PrecoVenda, 0) AS PrecoVenda,
    P.PrecoCusto
" + baseSql + @"
ORDER BY P.Nome
LIMIT @PageSize OFFSET @Offset;";

        using var conn = GetConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@IdSalao", idSalao);
        parameters.Add("@Pesquisa", string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%");
        parameters.Add("@SomenteBaixo", somenteBaixo);
        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", pageSize);

        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await conn.QueryAsync<ProdutoEstoquePosicao>(selectSql, parameters)).ToList();

        return new PagedResult<ProdutoEstoquePosicao>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<EstoqueResumo> ObterResumoEstoqueAsync(int idSalao)
    {
        const string sql = @"
SELECT
    SUM(CASE WHEN COALESCE(ControlarEstoque, 0) = 1 THEN 1 ELSE 0 END) AS ProdutosComControle,
    SUM(CASE WHEN COALESCE(ControlarEstoque, 0) = 1 AND COALESCE(EstoqueAtual, 0) <= COALESCE(EstoqueMinimo, 0) THEN 1 ELSE 0 END) AS ProdutosComEstoqueBaixo,
    SUM(CASE WHEN COALESCE(ControlarEstoque, 0) = 1 THEN COALESCE(EstoqueAtual, 0) * COALESCE(PrecoCusto, 0) ELSE 0 END) AS ValorCustoEstoque,
    SUM(CASE WHEN COALESCE(ControlarEstoque, 0) = 1 THEN COALESCE(EstoqueAtual, 0) * COALESCE(PrecoVenda, 0) ELSE 0 END) AS ValorVendaEstoque
FROM CorteCor_Produto
WHERE IdSalao = @IdSalao
  AND (Excluido = 0 OR Excluido IS NULL);";

        using var conn = GetConnection();
        return await conn.QueryFirstAsync<EstoqueResumo>(sql, new { IdSalao = idSalao });
    }

    public async Task RegistrarAjusteEstoqueAsync(MovimentoEstoque movimento)
    {
        const string estoqueSql = @"
SELECT ISNULL(EstoqueAtual, 0)
FROM CorteCor_Produto
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto;";

        const string updateSql = @"
UPDATE CorteCor_Produto
SET EstoqueAtual = @SaldoPosterior
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto;";

        const string insertSql = @"
INSERT INTO CorteCor_MovimentoEstoque
    (IdMovimento, IdSalao, IdProduto, IdVendaProduto, TipoMovimento, Origem, Quantidade,
     SaldoAnterior, SaldoPosterior, Observacao, UsuarioOperador, DataMovimento)
VALUES
    (@IdMovimento, @IdSalao, @IdProduto, @IdVendaProduto, @TipoMovimento, @Origem, @Quantidade,
     @SaldoAnterior, @SaldoPosterior, @Observacao, @UsuarioOperador, @DataMovimento);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            movimento.SaldoAnterior = await conn.ExecuteScalarAsync<decimal>(estoqueSql, new { movimento.IdSalao, movimento.IdProduto }, tx);
            movimento.SaldoPosterior = movimento.TipoMovimento switch
            {
                MovimentoEstoqueTipo.Entrada => movimento.SaldoAnterior + movimento.Quantidade,
                MovimentoEstoqueTipo.Saida => movimento.SaldoAnterior - movimento.Quantidade,
                MovimentoEstoqueTipo.Estorno => movimento.SaldoAnterior + movimento.Quantidade,
                _ => movimento.SaldoAnterior + movimento.Quantidade
            };

            await conn.ExecuteAsync(updateSql, new
            {
                movimento.IdSalao,
                movimento.IdProduto,
                movimento.SaldoPosterior
            }, tx);

            await conn.ExecuteAsync(insertSql, movimento, tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<VendaPosVenda>> ListarPosVendaAsync(int idSalao, int idVendaProduto)
    {
        const string sql = @"
SELECT
    PV.IdPosVenda,
    PV.IdSalao,
    PV.IdVendaProduto,
    PV.TipoOperacao,
    PV.Status,
    PV.ValorCredito,
    PV.ValorReposicao,
    PV.DiferencaFinanceira,
    PV.Observacoes,
    PV.UsuarioOperador,
    PV.DataOperacao,
    P.Nome AS NomeCliente
FROM CorteCor_VendaPosVenda PV
INNER JOIN CorteCor_VendaProduto V ON V.IdVendaProduto = PV.IdVendaProduto AND V.IdSalao = PV.IdSalao
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = V.IdPessoa
WHERE PV.IdSalao = @IdSalao
  AND PV.IdVendaProduto = @IdVendaProduto
ORDER BY PV.DataOperacao DESC, PV.IdPosVenda DESC;";

        using var conn = GetConnection();
        return (await conn.QueryAsync<VendaPosVenda>(sql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto })).ToList();
    }

    public async Task<int> ProcessarPosVendaAsync(
        int idSalao,
        int idVendaProduto,
        VendaPosVenda posVenda,
        IReadOnlyCollection<VendaPosVendaItem> itens,
        IReadOnlyCollection<MovimentoEstoque> movimentos,
        string novoStatusVenda,
        string? observacaoVenda)
    {
        const string selectVendaSql = @"
SELECT TOP 1 *
FROM CorteCor_VendaProduto
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string insertPosVendaSql = @"
INSERT INTO CorteCor_VendaPosVenda
    (IdSalao, IdVendaProduto, TipoOperacao, Status, ValorCredito, ValorReposicao, DiferencaFinanceira,
     Observacoes, UsuarioOperador, DataOperacao)
VALUES
    (@IdSalao, @IdVendaProduto, @TipoOperacao, @Status, @ValorCredito, @ValorReposicao, @DiferencaFinanceira,
     @Observacoes, @UsuarioOperador, @DataOperacao);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        const string insertPosVendaItemSql = @"
INSERT INTO CorteCor_VendaPosVendaItem
    (IdPosVenda, IdSalao, IdVendaProduto, IdItemVenda, TipoRegistro, TipoItem, IdProduto, IdServico,
     Descricao, Quantidade, ValorUnitario, ValorTotal, Unidade, ControlaEstoque)
VALUES
    (@IdPosVenda, @IdSalao, @IdVendaProduto, @IdItemVenda, @TipoRegistro, @TipoItem, @IdProduto, @IdServico,
     @Descricao, @Quantidade, @ValorUnitario, @ValorTotal, @Unidade, @ControlaEstoque);";

        const string updateVendaSql = @"
UPDATE CorteCor_VendaProduto
SET Status = @Status,
    Observacoes = @Observacoes,
    DataAtualizacao = GETDATE(),
    UsuarioOperador = @UsuarioOperador
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string updateProdutoSql = @"
UPDATE CorteCor_Produto
SET EstoqueAtual = @SaldoPosterior
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto;";

        const string insertMovimentoSql = @"
INSERT INTO CorteCor_MovimentoEstoque
    (IdMovimento, IdSalao, IdProduto, IdVendaProduto, TipoMovimento, Origem, Quantidade,
     SaldoAnterior, SaldoPosterior, Observacao, UsuarioOperador, DataMovimento)
VALUES
    (@IdMovimento, @IdSalao, @IdProduto, @IdVendaProduto, @TipoMovimento, @Origem, @Quantidade,
     @SaldoAnterior, @SaldoPosterior, @Observacao, @UsuarioOperador, @DataMovimento);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var venda = await conn.QueryFirstOrDefaultAsync<VendaProduto>(selectVendaSql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto }, tx)
                ?? throw new InvalidOperationException("Venda não encontrada para o pós-venda.");

            posVenda.IdSalao = idSalao;
            posVenda.IdVendaProduto = idVendaProduto;
            var idPosVenda = await conn.ExecuteScalarAsync<int>(insertPosVendaSql, posVenda, tx);

            var colunaQuantidade = posVenda.TipoOperacao switch
            {
                var tipo when string.Equals(tipo, VendaPosVendaTipo.Devolucao, StringComparison.OrdinalIgnoreCase) => "QuantidadeDevolvida",
                var tipo when string.Equals(tipo, VendaPosVendaTipo.Troca, StringComparison.OrdinalIgnoreCase) => "QuantidadeTrocada",
                _ => "QuantidadeCancelada"
            };

            foreach (var item in itens)
            {
                item.IdPosVenda = idPosVenda;
                item.IdSalao = idSalao;
                item.IdVendaProduto = idVendaProduto;
                await conn.ExecuteAsync(insertPosVendaItemSql, item, tx);

                if (item.IdItemVenda.HasValue && string.Equals(item.TipoRegistro, VendaPosVendaRegistroTipo.Origem, StringComparison.OrdinalIgnoreCase))
                {
                    var updateItemSql = $@"
UPDATE CorteCor_VendaProdutoItem
SET {colunaQuantidade} = ISNULL({colunaQuantidade}, 0) + @Quantidade
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto
  AND IdItemVenda = @IdItemVenda;";

                    await conn.ExecuteAsync(updateItemSql, new
                    {
                        IdSalao = idSalao,
                        IdVendaProduto = idVendaProduto,
                        item.IdItemVenda,
                        item.Quantidade
                    }, tx);
                }
            }

            foreach (var movimento in movimentos)
            {
                await conn.ExecuteAsync(updateProdutoSql, new
                {
                    movimento.IdSalao,
                    movimento.IdProduto,
                    movimento.SaldoPosterior
                }, tx);

                await conn.ExecuteAsync(insertMovimentoSql, movimento, tx);
            }

            var observacaoFinal = string.IsNullOrWhiteSpace(observacaoVenda)
                ? venda.Observacoes
                : string.IsNullOrWhiteSpace(venda.Observacoes)
                    ? observacaoVenda
                    : $"{venda.Observacoes}{Environment.NewLine}{observacaoVenda}";

            await conn.ExecuteAsync(updateVendaSql, new
            {
                IdSalao = idSalao,
                IdVendaProduto = idVendaProduto,
                Status = novoStatusVenda,
                Observacoes = observacaoFinal,
                UsuarioOperador = posVenda.UsuarioOperador
            }, tx);

            tx.Commit();
            return idPosVenda;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task CancelarVendaAsync(int idSalao, int idVendaProduto, string? usuario, string? observacao)
    {
        const string selectVendaSql = @"
SELECT TOP 1 *
FROM CorteCor_VendaProduto
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string selectItensSql = @"
SELECT *
FROM CorteCor_VendaProdutoItem
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string estoqueAtualSql = @"
SELECT ISNULL(EstoqueAtual, 0)
FROM CorteCor_Produto
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto;";

        const string updateVendaSql = @"
UPDATE CorteCor_VendaProduto
SET Status = @Status,
    Observacoes = @Observacoes,
    DataAtualizacao = GETDATE(),
    UsuarioOperador = @UsuarioOperador
WHERE IdSalao = @IdSalao
  AND IdVendaProduto = @IdVendaProduto;";

        const string updateProdutoSql = @"
UPDATE CorteCor_Produto
SET EstoqueAtual = @SaldoPosterior
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto;";

        const string insertMovimentoSql = @"
INSERT INTO CorteCor_MovimentoEstoque
    (IdMovimento, IdSalao, IdProduto, IdVendaProduto, TipoMovimento, Origem, Quantidade,
     SaldoAnterior, SaldoPosterior, Observacao, UsuarioOperador, DataMovimento)
VALUES
    (@IdMovimento, @IdSalao, @IdProduto, @IdVendaProduto, @TipoMovimento, @Origem, @Quantidade,
     @SaldoAnterior, @SaldoPosterior, @Observacao, @UsuarioOperador, @DataMovimento);";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var venda = await conn.QueryFirstOrDefaultAsync<VendaProduto>(selectVendaSql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto }, tx)
                ?? throw new InvalidOperationException("Venda não encontrada.");

            if (string.Equals(venda.Status, VendaProdutoStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
            {
                tx.Commit();
                return;
            }

            var itens = (await conn.QueryAsync<VendaProdutoItem>(selectItensSql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto }, tx)).ToList();
            foreach (var item in itens.Where(i => i.ControlaEstoque && i.IdProduto.HasValue && i.QuantidadeDisponivelPosVenda > 0m))
            {
                var saldoAnterior = await conn.ExecuteScalarAsync<decimal>(estoqueAtualSql, new { IdSalao = idSalao, IdProduto = item.IdProduto!.Value }, tx);
                var saldoPosterior = saldoAnterior + item.QuantidadeDisponivelPosVenda;

                await conn.ExecuteAsync(updateProdutoSql, new
                {
                    IdSalao = idSalao,
                    IdProduto = item.IdProduto!.Value,
                    SaldoPosterior = saldoPosterior
                }, tx);

                await conn.ExecuteAsync(insertMovimentoSql, new MovimentoEstoque
                {
                    IdMovimento = Guid.NewGuid(),
                    IdSalao = idSalao,
                    IdProduto = item.IdProduto!.Value,
                    IdVendaProduto = idVendaProduto,
                    TipoMovimento = MovimentoEstoqueTipo.Estorno,
                    Origem = MovimentoEstoqueOrigem.CancelamentoVenda,
                    Quantidade = item.QuantidadeDisponivelPosVenda,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = saldoPosterior,
                    Observacao = string.IsNullOrWhiteSpace(observacao)
                        ? $"Estorno do estoque pela venda {idVendaProduto}."
                        : observacao,
                    UsuarioOperador = usuario,
                    DataMovimento = DateTime.Now
                }, tx);
            }

            var observacaoFinal = string.IsNullOrWhiteSpace(observacao)
                ? venda.Observacoes
                : string.IsNullOrWhiteSpace(venda.Observacoes)
                    ? observacao
                    : $"{venda.Observacoes}{Environment.NewLine}{observacao}";

            await conn.ExecuteAsync(updateVendaSql, new
            {
                IdSalao = idSalao,
                IdVendaProduto = idVendaProduto,
                Status = VendaProdutoStatus.Cancelada,
                Observacoes = observacaoFinal,
                UsuarioOperador = usuario
            }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
