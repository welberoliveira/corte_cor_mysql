using System.Data;
using CorteCor.Models;
using Dapper;

namespace CorteCor.Handlers;

public class CompraHandler
{
    private readonly IDatabaseHandler _databaseHandler;

    public CompraHandler(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    private IDbConnection GetConnection() => _databaseHandler.GetConnection();

    public async Task<int> RegistrarCompraAsync(Compra compra, IReadOnlyCollection<CompraItem> itens, IReadOnlyCollection<MovimentoEstoque> movimentos)
    {
        const string insertCompraSql = @"
INSERT INTO CorteCor_Compra
    (IdSalao, IdPessoaFornecedor, IdPlano, IdConta, Status, Recorrencia, PagaNaHora, ValorTotal, Documento,
     Observacoes, UsuarioOperador, IdTituloFinanceiro, DataCompra, DataVencimento, DataCriacao, DataAtualizacao)
VALUES
    (@IdSalao, @IdPessoaFornecedor, @IdPlano, @IdConta, @Status, @Recorrencia, @PagaNaHora, @ValorTotal, @Documento,
     @Observacoes, @UsuarioOperador, @IdTituloFinanceiro, @DataCompra, @DataVencimento, NOW(), NOW());
SELECT LAST_INSERT_ID();";

        const string insertItemSql = @"
INSERT INTO CorteCor_CompraItem
    (IdCompra, IdSalao, IdProduto, NomeProduto, Quantidade, ValorUnitario, ValorTotal)
VALUES
    (@IdCompra, @IdSalao, @IdProduto, @NomeProduto, @Quantidade, @ValorUnitario, @ValorTotal);";

        const string updateProdutoSql = @"
UPDATE CorteCor_Produto
SET EstoqueAtual = @SaldoPosterior,
    PrecoCusto = @PrecoCusto
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
            var idCompra = await conn.ExecuteScalarAsync<int>(insertCompraSql, compra, tx);
            foreach (var item in itens)
            {
                item.IdCompra = idCompra;
                item.IdSalao = compra.IdSalao;
                await conn.ExecuteAsync(insertItemSql, item, tx);
            }

            foreach (var movimento in movimentos)
            {
                await conn.ExecuteAsync(updateProdutoSql, new
                {
                    movimento.IdSalao,
                    movimento.IdProduto,
                    movimento.SaldoPosterior,
                    PrecoCusto = itens.First(i => i.IdProduto == movimento.IdProduto).ValorUnitario
                }, tx);

                await conn.ExecuteAsync(insertMovimentoSql, movimento, tx);
            }

            tx.Commit();
            return idCompra;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task AtualizarTituloCompraAsync(int idSalao, int idCompra, Guid idTitulo)
    {
        const string sql = @"
UPDATE CorteCor_Compra
SET IdTituloFinanceiro = @IdTitulo,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdCompra = @IdCompra;";

        using var conn = GetConnection();
        await conn.ExecuteAsync(sql, new { IdSalao = idSalao, IdCompra = idCompra, IdTitulo = idTitulo });
    }

    public Task<CompraCancelamentoResult> CancelarCompraAsync(int idSalao, int idCompra, string? usuario, string? justificativa) =>
        CancelarCompraInternoAsync(idSalao, idCompra, null, usuario, justificativa);

    public Task<CompraCancelamentoResult> CancelarCompraPorTituloAsync(int idSalao, Guid idTitulo, string? usuario, string? justificativa) =>
        CancelarCompraInternoAsync(idSalao, null, idTitulo, usuario, justificativa);

    private async Task<CompraCancelamentoResult> CancelarCompraInternoAsync(int idSalao, int? idCompra, Guid? idTitulo, string? usuario, string? justificativa)
    {
        const string selectCompraSql = @"
SELECT *
FROM CorteCor_Compra
WHERE IdSalao = @IdSalao
  AND (@IdCompra IS NULL OR IdCompra = @IdCompra)
  AND (@IdTituloFinanceiro IS NULL OR IdTituloFinanceiro = @IdTituloFinanceiro)
LIMIT 1
FOR UPDATE;";

        const string selectItensSql = @"
SELECT *
FROM CorteCor_CompraItem
WHERE IdSalao = @IdSalao
  AND IdCompra = @IdCompra;";

        const string selectProdutoSql = @"
SELECT *
FROM CorteCor_Produto
WHERE IdSalao = @IdSalao
  AND IdProduto = @IdProduto
LIMIT 1
FOR UPDATE;";

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

        const string updateCompraSql = @"
UPDATE CorteCor_Compra
SET Status = @Status,
    Observacoes = @Observacoes,
    UsuarioOperador = @UsuarioOperador,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdCompra = @IdCompra;";

        const string updateTituloSql = @"
UPDATE CorteCor_FinanceiroTitulo
SET Status = @Status,
    DataLiquidacao = NULL,
    ValorLiquidado = 0,
    ValorAberto = 0,
    Conciliado = 0,
    Observacoes = @Observacoes,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdTitulo = @IdTitulo;";

        using var conn = GetConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            var idTituloFinanceiro = idTitulo?.ToString();
            var compra = await conn.QueryFirstOrDefaultAsync<Compra>(selectCompraSql, new
            {
                IdSalao = idSalao,
                IdCompra = idCompra,
                IdTituloFinanceiro = idTituloFinanceiro
            }, tx);

            if (compra == null)
            {
                tx.Commit();
                return new CompraCancelamentoResult { CompraLocalizada = false };
            }

            var motivo = string.IsNullOrWhiteSpace(justificativa)
                ? "Cancelamento solicitado pelo operador."
                : justificativa.Trim();

            var observacaoCancelamento = $"Compra #{compra.IdCompra} cancelada. Justificativa: {motivo} Estoque ajustado automaticamente.";
            var resultado = new CompraCancelamentoResult
            {
                IdCompra = compra.IdCompra,
                CompraLocalizada = true
            };

            if (!string.Equals(compra.Status, CompraStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
            {
                var itens = (await conn.QueryAsync<CompraItem>(selectItensSql, new
                {
                    IdSalao = idSalao,
                    compra.IdCompra
                }, tx)).ToList();

                foreach (var item in itens.Where(i => i.Quantidade > 0m))
                {
                    var produto = await conn.QueryFirstOrDefaultAsync<Produto>(selectProdutoSql, new
                    {
                        IdSalao = idSalao,
                        item.IdProduto
                    }, tx) ?? throw new InvalidOperationException($"Produto '{item.NomeProduto}' nao encontrado para estorno da compra.");

                    var saldoAnterior = produto.EstoqueAtual ?? 0m;
                    if (saldoAnterior < item.Quantidade)
                    {
                        throw new InvalidOperationException($"Nao foi possivel cancelar a compra #{compra.IdCompra}: o estoque atual do produto '{produto.Nome}' ({saldoAnterior:N0}) e menor que a quantidade a estornar ({item.Quantidade:N0}).");
                    }

                    var saldoPosterior = saldoAnterior - item.Quantidade;
                    await conn.ExecuteAsync(updateProdutoSql, new
                    {
                        IdSalao = idSalao,
                        item.IdProduto,
                        SaldoPosterior = saldoPosterior
                    }, tx);

                    await conn.ExecuteAsync(insertMovimentoSql, new MovimentoEstoque
                    {
                        IdMovimento = Guid.NewGuid(),
                        IdSalao = idSalao,
                        IdProduto = item.IdProduto,
                        IdVendaProduto = null,
                        TipoMovimento = MovimentoEstoqueTipo.Saida,
                        Origem = MovimentoEstoqueOrigem.CancelamentoCompra,
                        Quantidade = item.Quantidade,
                        SaldoAnterior = saldoAnterior,
                        SaldoPosterior = saldoPosterior,
                        Observacao = LimitarTexto(observacaoCancelamento, 1000),
                        UsuarioOperador = usuario,
                        DataMovimento = DateTime.Now
                    }, tx);

                    resultado.QuantidadeMovimentosEstoque++;
                }

                var observacaoCompra = AnexarObservacao(compra.Observacoes, observacaoCancelamento, 1000);
                await conn.ExecuteAsync(updateCompraSql, new
                {
                    IdSalao = idSalao,
                    compra.IdCompra,
                    Status = CompraStatus.Cancelada,
                    Observacoes = observacaoCompra,
                    UsuarioOperador = usuario
                }, tx);

                resultado.CanceladaAgora = true;
                resultado.EstoqueAjustado = resultado.QuantidadeMovimentosEstoque > 0;
            }

            if (compra.IdTituloFinanceiro.HasValue)
            {
                var observacaoTitulo = $"Cancelado automaticamente pelo cancelamento da compra #{compra.IdCompra}. Estoque ajustado automaticamente. Justificativa: {motivo}";
                var linhasTitulo = await conn.ExecuteAsync(updateTituloSql, new
                {
                    IdSalao = idSalao,
                    IdTitulo = compra.IdTituloFinanceiro.Value,
                    Status = FinanceiroStatusTitulo.Cancelado,
                    Observacoes = LimitarTexto(observacaoTitulo, 4000)
                }, tx);

                resultado.TituloFinanceiroCancelado = linhasTitulo > 0;
            }

            tx.Commit();
            return resultado;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<PagedResult<Compra>> ListarComprasAsync(int idSalao, CompraFiltro filtro)
    {
        var normalizado = filtro ?? new CompraFiltro();
        normalizado.PageIndex = normalizado.PageIndex <= 0 ? 1 : normalizado.PageIndex;
        normalizado.PageSize = normalizado.PageSize <= 0 ? 15 : normalizado.PageSize;
        var offset = (normalizado.PageIndex - 1) * normalizado.PageSize;

        const string baseSql = @"
FROM CorteCor_Compra C
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = C.IdPessoaFornecedor
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = C.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = C.IdConta
WHERE C.IdSalao = @IdSalao
  AND (@Status IS NULL OR C.Status = @Status)
  AND (@Pesquisa IS NULL OR COALESCE(P.Nome, '') LIKE @Pesquisa OR COALESCE(C.Documento, '') LIKE @Pesquisa OR COALESCE(C.Observacoes, '') LIKE @Pesquisa)
  AND (@DataInicio IS NULL OR DATE(C.DataCompra) >= @DataInicio)
  AND (@DataFim IS NULL OR DATE(C.DataCompra) <= @DataFim)";

        const string countSql = "SELECT COUNT(1) " + baseSql + ";";
        const string selectSql = @"
SELECT
    C.*,
    P.Nome AS NomeFornecedor,
    CASE
        WHEN PC.IdPlano IS NULL THEN NULL
        ELSE CONCAT(COALESCE(NULLIF(PC.Codigo, ''), ''), CASE WHEN NULLIF(PC.Codigo, '') IS NULL THEN '' ELSE ' - ' END, COALESCE(NULLIF(PC.Nome, ''), PC.Descricao))
    END AS NomePlano,
    CC.Nome AS NomeConta,
    (SELECT COUNT(1) FROM CorteCor_CompraItem I WHERE I.IdSalao = C.IdSalao AND I.IdCompra = C.IdCompra) AS QuantidadeItens
" + baseSql + @"
ORDER BY C.DataCompra DESC, C.IdCompra DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = GetConnection();
        var parametros = new
        {
            IdSalao = idSalao,
            Status = string.IsNullOrWhiteSpace(normalizado.Status) ? null : normalizado.Status.Trim(),
            Pesquisa = string.IsNullOrWhiteSpace(normalizado.Pesquisa) ? null : $"%{normalizado.Pesquisa.Trim()}%",
            DataInicio = normalizado.DataInicio?.Date,
            DataFim = normalizado.DataFim?.Date,
            Offset = offset,
            normalizado.PageSize
        };

        var total = await conn.ExecuteScalarAsync<int>(countSql, parametros);
        var itens = (await conn.QueryAsync<Compra>(selectSql, parametros)).ToList();

        return new PagedResult<Compra>
        {
            Items = itens,
            TotalCount = total,
            PageIndex = normalizado.PageIndex,
            PageSize = normalizado.PageSize
        };
    }

    private static string AnexarObservacao(string? atual, string nova, int limite)
    {
        var texto = string.IsNullOrWhiteSpace(atual)
            ? nova
            : $"{atual}{Environment.NewLine}{nova}";

        return LimitarTexto(texto, limite);
    }

    private static string LimitarTexto(string texto, int limite) =>
        texto.Length <= limite ? texto : texto[..limite];
}
