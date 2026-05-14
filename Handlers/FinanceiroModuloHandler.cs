using System.Data;
using Dapper;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class FinanceiroModuloHandler : IFinanceiroModuloHandler
    {
        private readonly IDatabaseHandler _databaseHandler;

        public FinanceiroModuloHandler(IDatabaseHandler databaseHandler)
        {
            _databaseHandler = databaseHandler;
        }

        private IDbConnection GetConnection() => _databaseHandler.GetConnection();

        public async Task SincronizarTitulosPagamentoAsync(int idSalao)
        {
            const string pagamentosSql = @"
SELECT
    P.IdPagamento,
    P.IdAgendamento,
    A.IdPessoa,
    S.IdSalao,
    S.Nome AS NomeServico,
    Pe.Nome AS NomePessoa,
    P.Status,
    P.Valor,
    P.Descricao,
    P.Tipo,
    COALESCE(P.PagoEm, P.CriadoEm, NOW()) AS DataReferencia,
    P.PagoEm,
    P.CriadoEm
FROM CorteCor_Pagamento P
INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = P.IdAgendamento
INNER JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
LEFT JOIN CorteCor_Pessoa Pe ON Pe.IdPessoa = A.IdPessoa
WHERE S.IdSalao = @IdSalao
  AND COALESCE(P.Ativo, 0) = 1;";

            const string selectTituloSql = @"
SELECT *
FROM CorteCor_FinanceiroTitulo
WHERE IdSalao = @IdSalao
  AND IdPagamento = @IdPagamento
LIMIT 1;";

            const string insertSql = @"
INSERT INTO CorteCor_FinanceiroTitulo
    (IdTitulo, IdSalao, Tipo, Origem, IdPessoa, IdAgendamento, IdVendaProduto, IdPagamento, IdPlano, IdConta,
     Descricao, Documento, Status, ValorOriginal, ValorLiquidado, ValorAberto, DataCompetencia,
     DataVencimento, DataLiquidacao, Conciliado, Observacoes, DataCriacao, DataAtualizacao)
VALUES
    (@IdTitulo, @IdSalao, @Tipo, @Origem, @IdPessoa, @IdAgendamento, @IdVendaProduto, @IdPagamento, @IdPlano, @IdConta,
     @Descricao, @Documento, @Status, @ValorOriginal, @ValorLiquidado, @ValorAberto, @DataCompetencia,
     @DataVencimento, @DataLiquidacao, @Conciliado, @Observacoes, NOW(), NOW());";

            const string updateSql = @"
UPDATE CorteCor_FinanceiroTitulo
SET IdPessoa = @IdPessoa,
    IdAgendamento = @IdAgendamento,
    IdVendaProduto = @IdVendaProduto,
    Descricao = @Descricao,
    Status = @Status,
    ValorOriginal = @ValorOriginal,
    ValorLiquidado = @ValorLiquidado,
    ValorAberto = @ValorAberto,
    DataCompetencia = @DataCompetencia,
    DataVencimento = @DataVencimento,
    DataLiquidacao = @DataLiquidacao,
    Tipo = @Tipo,
    Origem = @Origem,
    Observacoes = @Observacoes,
    DataAtualizacao = NOW()
WHERE IdTitulo = @IdTitulo
  AND IdSalao = @IdSalao;";

            using var conn = GetConnection();
            var pagamentos = (await conn.QueryAsync<FinanceiroPagamentoSyncRow>(pagamentosSql, new { IdSalao = idSalao })).ToList();

            foreach (var pagamento in pagamentos)
            {
                var existente = await conn.QueryFirstOrDefaultAsync<FinanceiroTitulo>(selectTituloSql, new
                {
                    IdSalao = idSalao,
                    pagamento.IdPagamento
                });

                var status = MapearStatusTitulo(pagamento.Status, pagamento.DataReferencia, pagamento.PagoEm);
                var titulo = new FinanceiroTitulo
                {
                    IdTitulo = existente?.IdTitulo ?? Guid.NewGuid(),
                    IdSalao = idSalao,
                    Tipo = FinanceiroTipoTitulo.Receber,
                    Origem = FinanceiroOrigemTitulo.PagamentoAgendamento,
                    IdPessoa = pagamento.IdPessoa,
                    IdAgendamento = pagamento.IdAgendamento,
                    IdVendaProduto = null,
                    IdPagamento = pagamento.IdPagamento,
                    Descricao = string.IsNullOrWhiteSpace(pagamento.Descricao)
                        ? $"Recebimento do agendamento {pagamento.IdAgendamento} - {pagamento.NomeServico}"
                        : pagamento.Descricao!,
                    Documento = pagamento.IdPagamento.ToString(),
                    Status = status,
                    ValorOriginal = pagamento.Valor,
                    ValorLiquidado = status == FinanceiroStatusTitulo.Liquidado ? pagamento.Valor : 0m,
                    ValorAberto = status == FinanceiroStatusTitulo.Liquidado || status == FinanceiroStatusTitulo.Cancelado ? 0m : pagamento.Valor,
                    DataCompetencia = pagamento.DataReferencia.Date,
                    DataVencimento = pagamento.DataReferencia.Date,
                    DataLiquidacao = status == FinanceiroStatusTitulo.Liquidado ? pagamento.PagoEm ?? pagamento.DataReferencia : null,
                    Conciliado = existente?.Conciliado ?? false,
                    Observacoes = $"Sincronizado automaticamente a partir do pagamento {pagamento.IdPagamento}",
                    TipoPagamento = pagamento.Tipo
                };

                if (existente == null)
                {
                    await conn.ExecuteAsync(insertSql, titulo);
                }
                else
                {
                    await conn.ExecuteAsync(updateSql, titulo);
                }
            }
        }

        public async Task<PagedResult<FinanceiroTitulo>> ListarTitulosAsync(int idSalao, FinanceiroTituloFiltro filtro)
        {
            var normalizedFilter = filtro ?? new FinanceiroTituloFiltro();
            normalizedFilter.PageIndex = normalizedFilter.PageIndex <= 0 ? 1 : normalizedFilter.PageIndex;
            normalizedFilter.PageSize = normalizedFilter.PageSize <= 0 ? 15 : normalizedFilter.PageSize;
            var offset = (normalizedFilter.PageIndex - 1) * normalizedFilter.PageSize;

            const string baseSql = @"
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
LEFT JOIN CorteCor_Agendamento A ON A.IdAgendamento = T.IdAgendamento
LEFT JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
WHERE T.IdSalao = @IdSalao
  AND (@Tipo IS NULL OR T.Tipo = @Tipo)
  AND (@Status IS NULL OR T.Status = @Status)
  AND (@IdPlano IS NULL OR T.IdPlano = @IdPlano)
  AND (@IdConta IS NULL OR T.IdConta = @IdConta)
  AND (
        @Pesquisa IS NULL
        OR T.Descricao LIKE @Pesquisa
        OR COALESCE(P.Nome, '') LIKE COALESCE(@Pesquisa, '')
        OR COALESCE(T.Documento, '') LIKE COALESCE(@Pesquisa, '')
      )
  AND (@DataInicio IS NULL OR T.DataVencimento >= @DataInicio)
  AND (@DataFim IS NULL OR T.DataVencimento <= @DataFim)
  AND (@SomenteVencidos = 0 OR (T.Status IN ('Aberto', 'Vencido') AND T.DataVencimento < CURDATE()))";

            const string selectSql = @"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    PC.Descricao AS NomePlano,
    CC.Nome AS NomeConta,
    S.Nome AS NomeServico,
    T.Origem AS CategoriaFluxo
" + baseSql + @"
ORDER BY
    CASE WHEN T.Status = 'Vencido' THEN 0 WHEN T.Status = 'Aberto' THEN 1 ELSE 2 END,
    T.DataVencimento,
    T.Descricao
LIMIT @PageSize OFFSET @Offset;";

            const string countSql = "SELECT COUNT(1) " + baseSql + ";";

            using var conn = GetConnection();
            var parameters = CriarParametrosFiltro(idSalao, normalizedFilter, offset);
            var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
            var items = (await conn.QueryAsync<FinanceiroTitulo>(selectSql, parameters)).ToList();

            foreach (var item in items.Where(i => i.Status == FinanceiroStatusTitulo.Aberto && i.DataVencimento.Date < DateTime.Today))
            {
                item.Status = FinanceiroStatusTitulo.Vencido;
            }

            return new PagedResult<FinanceiroTitulo>
            {
                Items = items,
                TotalCount = total,
                PageIndex = normalizedFilter.PageIndex,
                PageSize = normalizedFilter.PageSize
            };
        }

        public async Task<FinanceiroTitulo?> ObterTituloAsync(int idSalao, Guid idTitulo)
        {
            const string sql = @"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    PC.Descricao AS NomePlano,
    CC.Nome AS NomeConta
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
WHERE T.IdSalao = @IdSalao
  AND T.IdTitulo = @IdTitulo
LIMIT 1;";

            using var conn = GetConnection();
            var titulo = await conn.QueryFirstOrDefaultAsync<FinanceiroTitulo>(sql, new { IdSalao = idSalao, IdTitulo = idTitulo });
            if (titulo != null && titulo.Status == FinanceiroStatusTitulo.Aberto && titulo.DataVencimento.Date < DateTime.Today)
            {
                titulo.Status = FinanceiroStatusTitulo.Vencido;
            }

            return titulo;
        }

        public async Task<List<FinanceiroTitulo>> ListarTitulosPorVendaAsync(int idSalao, int idVendaProduto)
        {
            const string sql = @"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    PC.Descricao AS NomePlano,
    CC.Nome AS NomeConta
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
WHERE T.IdSalao = @IdSalao
  AND T.IdVendaProduto = @IdVendaProduto
ORDER BY T.DataCriacao DESC;";

            using var conn = GetConnection();
            return (await conn.QueryAsync<FinanceiroTitulo>(sql, new { IdSalao = idSalao, IdVendaProduto = idVendaProduto })).ToList();
        }

        public async Task<Guid> SalvarTituloAsync(FinanceiroTitulo titulo)
        {
            const string insertSql = @"
INSERT INTO CorteCor_FinanceiroTitulo
    (IdTitulo, IdSalao, Tipo, Origem, IdPessoa, IdAgendamento, IdVendaProduto, IdPagamento, IdPlano, IdConta,
     Descricao, Documento, Status, ValorOriginal, ValorLiquidado, ValorAberto, DataCompetencia,
     DataVencimento, DataLiquidacao, Conciliado, Observacoes, DataCriacao, DataAtualizacao)
VALUES
    (@IdTitulo, @IdSalao, @Tipo, @Origem, @IdPessoa, @IdAgendamento, @IdVendaProduto, @IdPagamento, @IdPlano, @IdConta,
     @Descricao, @Documento, @Status, @ValorOriginal, @ValorLiquidado, @ValorAberto, @DataCompetencia,
     @DataVencimento, @DataLiquidacao, @Conciliado, @Observacoes, NOW(), NOW());";

            const string updateSql = @"
UPDATE CorteCor_FinanceiroTitulo
SET Tipo = @Tipo,
    Origem = @Origem,
    IdPessoa = @IdPessoa,
    IdAgendamento = @IdAgendamento,
    IdVendaProduto = @IdVendaProduto,
    IdPlano = @IdPlano,
    IdConta = @IdConta,
    Descricao = @Descricao,
    Documento = @Documento,
    Status = @Status,
    ValorOriginal = @ValorOriginal,
    ValorLiquidado = @ValorLiquidado,
    ValorAberto = @ValorAberto,
    DataCompetencia = @DataCompetencia,
    DataVencimento = @DataVencimento,
    DataLiquidacao = @DataLiquidacao,
    Conciliado = @Conciliado,
    Observacoes = @Observacoes,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdTitulo = @IdTitulo;";

            using var conn = GetConnection();
            titulo.IdTitulo = titulo.IdTitulo == Guid.Empty ? Guid.NewGuid() : titulo.IdTitulo;
            if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM CorteCor_FinanceiroTitulo WHERE IdSalao = @IdSalao AND IdTitulo = @IdTitulo;", new { titulo.IdSalao, titulo.IdTitulo }) > 0)
            {
                await conn.ExecuteAsync(updateSql, titulo);
            }
            else
            {
                await conn.ExecuteAsync(insertSql, titulo);
            }

            return titulo.IdTitulo;
        }

        public async Task AtualizarStatusTituloAsync(int idSalao, Guid idTitulo, string status, DateTime? dataLiquidacao, decimal? valorLiquidado, bool? conciliado)
        {
            const string sql = @"
UPDATE CorteCor_FinanceiroTitulo
SET Status = @Status,
    DataLiquidacao = @DataLiquidacao,
    ValorLiquidado = @ValorLiquidado,
    ValorAberto = @ValorAberto,
    Conciliado = COALESCE(@Conciliado, Conciliado),
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdTitulo = @IdTitulo;";

            using var conn = GetConnection();
            var atual = await conn.QueryFirstOrDefaultAsync<FinanceiroTitulo>(
                "SELECT * FROM CorteCor_FinanceiroTitulo WHERE IdSalao = @IdSalao AND IdTitulo = @IdTitulo LIMIT 1;",
                new { IdSalao = idSalao, IdTitulo = idTitulo });

            if (atual == null)
            {
                return;
            }

            var valorLiquidadoFinal = status == FinanceiroStatusTitulo.Liquidado
                ? (valorLiquidado ?? atual.ValorOriginal)
                : 0m;
            var valorAbertoFinal = status == FinanceiroStatusTitulo.Liquidado || status == FinanceiroStatusTitulo.Cancelado
                ? 0m
                : atual.ValorOriginal - valorLiquidadoFinal;

            await conn.ExecuteAsync(sql, new
            {
                IdSalao = idSalao,
                IdTitulo = idTitulo,
                Status = status,
                DataLiquidacao = status == FinanceiroStatusTitulo.Liquidado ? (DateTime?)(dataLiquidacao ?? DateTime.Now) : null,
                ValorLiquidado = valorLiquidadoFinal,
                ValorAberto = valorAbertoFinal < 0 ? 0 : valorAbertoFinal,
                Conciliado = conciliado
            });
        }

        public async Task AtualizarValoresTituloAsync(int idSalao, Guid idTitulo, decimal valorOriginal, decimal valorLiquidado, decimal valorAberto, string status, DateTime? dataLiquidacao, bool conciliado, string? observacoes)
        {
            const string sql = @"
UPDATE CorteCor_FinanceiroTitulo
SET ValorOriginal = @ValorOriginal,
    ValorLiquidado = @ValorLiquidado,
    ValorAberto = @ValorAberto,
    Status = @Status,
    DataLiquidacao = @DataLiquidacao,
    Conciliado = @Conciliado,
    Observacoes = @Observacoes,
    DataAtualizacao = NOW()
WHERE IdSalao = @IdSalao
  AND IdTitulo = @IdTitulo;";

            using var conn = GetConnection();
            await conn.ExecuteAsync(sql, new
            {
                IdSalao = idSalao,
                IdTitulo = idTitulo,
                ValorOriginal = valorOriginal,
                ValorLiquidado = valorLiquidado,
                ValorAberto = valorAberto,
                Status = status,
                DataLiquidacao = dataLiquidacao,
                Conciliado = conciliado,
                Observacoes = observacoes
            });
        }

        public async Task<List<PlanoContas>> ListarPlanoContasAsync(int idSalao)
        {
            const string sql = @"
SELECT IdPlano, IdSalao, Codigo, Descricao, Tipo, Ativo
FROM CorteCor_PlanoContas
WHERE IdSalao = @IdSalao
ORDER BY Codigo, Descricao;";

            using var conn = GetConnection();
            return (await conn.QueryAsync<PlanoContas>(sql, new { IdSalao = idSalao })).ToList();
        }

        public async Task SavePlanoContasAsync(PlanoContas plano)
        {
            const string insertSql = @"
INSERT INTO CorteCor_PlanoContas (IdSalao, Codigo, Descricao, Tipo, Ativo)
VALUES (@IdSalao, @Codigo, @Descricao, @Tipo, @Ativo);";

            const string updateSql = @"
UPDATE CorteCor_PlanoContas
SET Codigo = @Codigo,
    Descricao = @Descricao,
    Tipo = @Tipo,
    Ativo = @Ativo
WHERE IdPlano = @IdPlano
  AND IdSalao = @IdSalao;";

            using var conn = GetConnection();
            if (plano.IdPlano > 0)
            {
                await conn.ExecuteAsync(updateSql, plano);
            }
            else
            {
                await conn.ExecuteAsync(insertSql, plano);
            }
        }

        public async Task<List<ContaCaixa>> ListarContasCaixaAsync(int idSalao)
        {
            const string sql = @"
SELECT IdConta, IdSalao, Nome, Tipo, Banco, Agencia, Conta, SaldoInicial, Ativo
FROM CorteCor_ContaCaixa
WHERE IdSalao = @IdSalao
ORDER BY Nome;";

            using var conn = GetConnection();
            return (await conn.QueryAsync<ContaCaixa>(sql, new { IdSalao = idSalao })).ToList();
        }

        public async Task SaveContaCaixaAsync(ContaCaixa conta)
        {
            const string insertSql = @"
INSERT INTO CorteCor_ContaCaixa (IdSalao, Nome, Tipo, Banco, Agencia, Conta, SaldoInicial, Ativo)
VALUES (@IdSalao, @Nome, @Tipo, @Banco, @Agencia, @Conta, @SaldoInicial, @Ativo);";

            const string updateSql = @"
UPDATE CorteCor_ContaCaixa
SET Nome = @Nome,
    Tipo = @Tipo,
    Banco = @Banco,
    Agencia = @Agencia,
    Conta = @Conta,
    SaldoInicial = @SaldoInicial,
    Ativo = @Ativo
WHERE IdConta = @IdConta
  AND IdSalao = @IdSalao;";

            using var conn = GetConnection();
            if (conta.IdConta > 0)
            {
                await conn.ExecuteAsync(updateSql, conta);
            }
            else
            {
                await conn.ExecuteAsync(insertSql, conta);
            }
        }

        public async Task<FinanceiroDashboardResumo> ObterDashboardAsync(int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            using var conn = GetConnection();
            var resumo = new FinanceiroDashboardResumo();

            const string kpiSql = @"
SELECT
    COALESCE(SUM(CASE WHEN Tipo = 'Receber' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY) THEN ValorLiquidado ELSE 0 END), 0) AS ReceitasLiquidadas,
    COALESCE(SUM(CASE WHEN Tipo = 'Pagar' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY) THEN ValorLiquidado ELSE 0 END), 0) AS DespesasLiquidadas,
    COALESCE(SUM(CASE WHEN Tipo = 'Receber' AND Status IN ('Aberto', 'Vencido') THEN ValorAberto ELSE 0 END), 0) AS AReceberAberto,
    COALESCE(SUM(CASE WHEN Tipo = 'Pagar' AND Status IN ('Aberto', 'Vencido') THEN ValorAberto ELSE 0 END), 0) AS APagarAberto,
    COALESCE(SUM(CASE WHEN Tipo = 'Receber' AND Status = 'Vencido' THEN ValorAberto ELSE 0 END), 0) AS ReceitasVencidas,
    COALESCE(SUM(CASE WHEN Tipo = 'Pagar' AND Status = 'Vencido' THEN ValorAberto ELSE 0 END), 0) AS DespesasVencidas,
    COALESCE(SUM(CASE WHEN Tipo = 'Receber' AND Status IN ('Aberto', 'Vencido') THEN 1 ELSE 0 END), 0) +
    COALESCE(SUM(CASE WHEN Tipo = 'Pagar' AND Status IN ('Aberto', 'Vencido') THEN 1 ELSE 0 END), 0) AS QuantidadeTitulosAbertos,
    COALESCE(SUM(CASE WHEN Status = 'Vencido' THEN 1 ELSE 0 END), 0) AS QuantidadeTitulosVencidos,
    COALESCE(AVG(CASE WHEN Tipo = 'Receber' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY) THEN ValorLiquidado END), 0) AS TicketMedioRecebido
FROM CorteCor_FinanceiroTitulo
WHERE IdSalao = @IdSalao;";

            var kpis = await conn.QueryFirstAsync<FinanceiroDashboardKpiRow>(kpiSql, new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date });
            resumo.ReceitasLiquidadas = kpis.ReceitasLiquidadas;
            resumo.DespesasLiquidadas = kpis.DespesasLiquidadas;
            resumo.SaldoOperacional = kpis.ReceitasLiquidadas - kpis.DespesasLiquidadas;
            resumo.AReceberAberto = kpis.AReceberAberto;
            resumo.APagarAberto = kpis.APagarAberto;
            resumo.ReceitasVencidas = kpis.ReceitasVencidas;
            resumo.DespesasVencidas = kpis.DespesasVencidas;
            resumo.QuantidadeTitulosAbertos = kpis.QuantidadeTitulosAbertos;
            resumo.QuantidadeTitulosVencidos = kpis.QuantidadeTitulosVencidos;
            resumo.TicketMedioRecebido = kpis.TicketMedioRecebido;

            const string saldoBaseSql = @"
SELECT
    COALESCE((SELECT SUM(COALESCE(SaldoInicial, 0)) FROM CorteCor_ContaCaixa WHERE IdSalao = @IdSalao AND Ativo = 1), 0)
    + COALESCE((SELECT SUM(CASE WHEN Tipo = 'Receber' AND Status = 'Liquidado' THEN ValorLiquidado WHEN Tipo = 'Pagar' AND Status = 'Liquidado' THEN -ValorLiquidado ELSE 0 END)
              FROM CorteCor_FinanceiroTitulo WHERE IdSalao = @IdSalao), 0);";
            var saldoAtual = await conn.ExecuteScalarAsync<decimal>(saldoBaseSql, new { IdSalao = idSalao });
            resumo.SaldoProjetado = saldoAtual + resumo.AReceberAberto - resumo.APagarAberto;

resumo.FluxoCaixa = (await ObterFluxoCaixaAsync(conn, idSalao, dataInicio.Date.AddDays(-15), dataFim.Date.AddDays(15))).ToList();
resumo.DreGerencial = (await ObterDreAsync(conn, idSalao, dataInicio.Date, dataFim.Date)).ToList();
resumo.ReceitasPorForma = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    COALESCE(NULLIF(P.Tipo, ''), T.Origem) AS Nome,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pagamento P ON P.IdPagamento = T.IdPagamento
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Receber'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(NULLIF(P.Tipo, ''), T.Origem)
ORDER BY SUM(T.ValorLiquidado) DESC
LIMIT 8;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            resumo.DespesasPorPlano = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    COALESCE(PC.Descricao, 'Sem plano') AS Nome,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Pagar'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(PC.Descricao, 'Sem plano')
ORDER BY SUM(T.ValorLiquidado) DESC
LIMIT 8;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            resumo.TopClientes = (await conn.QueryAsync<FinanceiroClienteResumo>(@"
SELECT
    COALESCE(P.Nome, 'Cliente nao identificado') AS NomeCliente,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Receber'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(P.Nome, 'Cliente nao identificado')
ORDER BY SUM(T.ValorLiquidado) DESC
LIMIT 10;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            resumo.TitulosCriticos = (await conn.QueryAsync<FinanceiroTitulo>(@"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    PC.Descricao AS NomePlano,
    CC.Nome AS NomeConta
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
WHERE T.IdSalao = @IdSalao
  AND (
        (T.Status = 'Vencido')
        OR (T.Status = 'Aberto' AND T.DataVencimento <= DATE_ADD(CURDATE(), INTERVAL 7 DAY))
      )
ORDER BY
    CASE WHEN T.Status = 'Vencido' THEN 0 ELSE 1 END,
    T.DataVencimento,
    T.ValorAberto DESC
LIMIT 10;", new { IdSalao = idSalao })).ToList();

            return resumo;
        }

        public async Task<FinanceiroRelatorioResumo> ObterRelatoriosAsync(int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            using var conn = GetConnection();
            var relatorio = new FinanceiroRelatorioResumo();

            relatorio.Titulos = (await conn.QueryAsync<FinanceiroTitulo>(@"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    PC.Descricao AS NomePlano,
    CC.Nome AS NomeConta,
    S.Nome AS NomeServico
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
LEFT JOIN CorteCor_Agendamento A ON A.IdAgendamento = T.IdAgendamento
LEFT JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
WHERE T.IdSalao = @IdSalao
  AND T.DataVencimento >= @DataInicio
  AND T.DataVencimento < DATE_ADD(@DataFim, INTERVAL 1 DAY)
ORDER BY T.DataVencimento, T.Descricao;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            relatorio.ReceitasPorPlano = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    COALESCE(PC.Descricao, 'Sem plano') AS Nome,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Receber'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(PC.Descricao, 'Sem plano')
ORDER BY SUM(T.ValorLiquidado) DESC;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            relatorio.DespesasPorPlano = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    COALESCE(PC.Descricao, 'Sem plano') AS Nome,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Pagar'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(PC.Descricao, 'Sem plano')
ORDER BY SUM(T.ValorLiquidado) DESC;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

relatorio.ReceitasPorForma = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    COALESCE(NULLIF(P.Tipo, ''), T.Origem) AS Nome,
    SUM(T.ValorLiquidado) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
LEFT JOIN CorteCor_Pagamento P ON P.IdPagamento = T.IdPagamento
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Receber'
  AND T.Status = 'Liquidado'
  AND T.DataLiquidacao >= @DataInicio
  AND T.DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY COALESCE(NULLIF(P.Tipo, ''), T.Origem)
ORDER BY SUM(T.ValorLiquidado) DESC;", new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();

            relatorio.InadimplenciaPorFaixa = (await conn.QueryAsync<FinanceiroResumoCategoria>(@"
SELECT
    CASE
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 1 AND 7 THEN '1 a 7 dias'
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 8 AND 30 THEN '8 a 30 dias'
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 31 AND 60 THEN '31 a 60 dias'
        ELSE '61+ dias'
    END AS Nome,
    SUM(T.ValorAberto) AS Valor,
    COUNT(1) AS Quantidade
FROM CorteCor_FinanceiroTitulo T
WHERE T.IdSalao = @IdSalao
  AND T.Tipo = 'Receber'
  AND T.Status = 'Vencido'
GROUP BY
    CASE
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 1 AND 7 THEN '1 a 7 dias'
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 8 AND 30 THEN '8 a 30 dias'
        WHEN DATEDIFF(CURDATE(), T.DataVencimento) BETWEEN 31 AND 60 THEN '31 a 60 dias'
        ELSE '61+ dias'
    END
ORDER BY MIN(DATEDIFF(CURDATE(), T.DataVencimento));", new { IdSalao = idSalao })).ToList();

            relatorio.FluxoProjetado = (await ObterFluxoProjetadoAsync(conn, idSalao, DateTime.Today.AddDays(-7), DateTime.Today.AddDays(30))).ToList();
            return relatorio;
        }

        private static DynamicParameters CriarParametrosFiltro(int idSalao, FinanceiroTituloFiltro filtro, int offset)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@IdSalao", idSalao);
            parameters.Add("@Tipo", string.IsNullOrWhiteSpace(filtro.Tipo) ? null : filtro.Tipo.Trim());
            parameters.Add("@Status", string.IsNullOrWhiteSpace(filtro.Status) ? null : filtro.Status.Trim());
            parameters.Add("@IdPlano", filtro.IdPlano);
            parameters.Add("@IdConta", filtro.IdConta);
            parameters.Add("@Pesquisa", string.IsNullOrWhiteSpace(filtro.Pesquisa) ? null : $"%{filtro.Pesquisa.Trim()}%");
            parameters.Add("@DataInicio", filtro.DataInicio?.Date);
            parameters.Add("@DataFim", filtro.DataFim?.Date);
            parameters.Add("@SomenteVencidos", filtro.SomenteVencidos);
            parameters.Add("@Offset", offset);
            parameters.Add("@PageSize", filtro.PageSize);
            return parameters;
        }

        private static string MapearStatusTitulo(string? statusPagamento, DateTime dataReferencia, DateTime? pagoEm)
        {
            var status = (statusPagamento ?? string.Empty).Trim();
            if (status.Equals("Pago", StringComparison.OrdinalIgnoreCase) || status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                return FinanceiroStatusTitulo.Liquidado;
            }

            if (status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase)
                || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
                || status.Equals("rejected", StringComparison.OrdinalIgnoreCase)
                || status.Equals("failure", StringComparison.OrdinalIgnoreCase))
            {
                return FinanceiroStatusTitulo.Cancelado;
            }

            if (!pagoEm.HasValue && dataReferencia.Date < DateTime.Today)
            {
                return FinanceiroStatusTitulo.Vencido;
            }

            return FinanceiroStatusTitulo.Aberto;
        }

        private static async Task<IEnumerable<FinanceiroFluxoCaixaItem>> ObterFluxoCaixaAsync(IDbConnection conn, int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            const string fluxoSql = @"
SELECT
    DataReferencia AS [Data],
    SUM(Entradas) AS Entradas,
    SUM(Saidas) AS Saidas
FROM
(
    SELECT DATE(DataLiquidacao) AS DataReferencia, ValorLiquidado AS Entradas, 0.00 AS Saidas
    FROM CorteCor_FinanceiroTitulo
    WHERE IdSalao = @IdSalao AND Tipo = 'Receber' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)

    UNION ALL

    SELECT DATE(DataLiquidacao) AS DataReferencia, 0.00 AS Entradas, ValorLiquidado AS Saidas
    FROM CorteCor_FinanceiroTitulo
    WHERE IdSalao = @IdSalao AND Tipo = 'Pagar' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
) Fluxo
GROUP BY DataReferencia
ORDER BY DataReferencia;";

            const string saldoAnteriorSql = @"
SELECT
    COALESCE((SELECT SUM(COALESCE(SaldoInicial, 0)) FROM CorteCor_ContaCaixa WHERE IdSalao = @IdSalao AND Ativo = 1), 0)
    + COALESCE(SUM(CASE WHEN Tipo = 'Receber' AND Status = 'Liquidado' THEN ValorLiquidado WHEN Tipo = 'Pagar' AND Status = 'Liquidado' THEN -ValorLiquidado ELSE 0 END), 0)
FROM CorteCor_FinanceiroTitulo
WHERE IdSalao = @IdSalao
  AND DataLiquidacao < @DataInicio;";

            var movimentos = (await conn.QueryAsync<FinanceiroFluxoCaixaItem>(fluxoSql, new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();
            var saldo = await conn.ExecuteScalarAsync<decimal>(saldoAnteriorSql, new { IdSalao = idSalao, DataInicio = dataInicio.Date });
            var lookup = movimentos.ToDictionary(m => m.Data.Date);
            var fluxo = new List<FinanceiroFluxoCaixaItem>();

            for (var data = dataInicio.Date; data <= dataFim.Date; data = data.AddDays(1))
            {
                lookup.TryGetValue(data, out var item);
                saldo += (item?.Entradas ?? 0m) - (item?.Saidas ?? 0m);
                fluxo.Add(new FinanceiroFluxoCaixaItem
                {
                    Data = data,
                    Entradas = item?.Entradas ?? 0m,
                    Saidas = item?.Saidas ?? 0m,
                    SaldoAcumulado = saldo
                });
            }

            return fluxo;
        }

        private static async Task<IEnumerable<FinanceiroFluxoCaixaItem>> ObterFluxoProjetadoAsync(IDbConnection conn, int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            const string sql = @"
SELECT
    DATE(DataVencimento) AS [Data],
    SUM(CASE WHEN Tipo = 'Receber' AND Status IN ('Aberto', 'Vencido') THEN ValorAberto ELSE 0 END) AS Entradas,
    SUM(CASE WHEN Tipo = 'Pagar' AND Status IN ('Aberto', 'Vencido') THEN ValorAberto ELSE 0 END) AS Saidas
FROM CorteCor_FinanceiroTitulo
WHERE IdSalao = @IdSalao
  AND DataVencimento >= @DataInicio
  AND DataVencimento < DATE_ADD(@DataFim, INTERVAL 1 DAY)
GROUP BY DATE(DataVencimento)
ORDER BY DATE(DataVencimento);";

            var movimentos = (await conn.QueryAsync<FinanceiroFluxoCaixaItem>(sql, new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date })).ToList();
            var saldo = 0m;
            var lookup = movimentos.ToDictionary(m => m.Data.Date);
            var fluxo = new List<FinanceiroFluxoCaixaItem>();

            for (var data = dataInicio.Date; data <= dataFim.Date; data = data.AddDays(1))
            {
                lookup.TryGetValue(data, out var item);
                saldo += (item?.Entradas ?? 0m) - (item?.Saidas ?? 0m);
                fluxo.Add(new FinanceiroFluxoCaixaItem
                {
                    Data = data,
                    Entradas = item?.Entradas ?? 0m,
                    Saidas = item?.Saidas ?? 0m,
                    SaldoAcumulado = saldo
                });
            }

            return fluxo;
        }

        private static async Task<IEnumerable<FinanceiroDreLinha>> ObterDreAsync(IDbConnection conn, int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            const string sql = @"
SELECT
    SUM(CASE WHEN Tipo = 'Receber' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY) THEN ValorLiquidado ELSE 0 END) AS Receitas,
    SUM(CASE WHEN Tipo = 'Pagar' AND Status = 'Liquidado' AND DataLiquidacao >= @DataInicio AND DataLiquidacao < DATE_ADD(@DataFim, INTERVAL 1 DAY) THEN ValorLiquidado ELSE 0 END) AS Despesas
FROM CorteCor_FinanceiroTitulo
WHERE IdSalao = @IdSalao;";

            var total = await conn.QueryFirstAsync<FinanceiroDreTotalRow>(sql, new { IdSalao = idSalao, DataInicio = dataInicio.Date, DataFim = dataFim.Date });
            return new List<FinanceiroDreLinha>
            {
                new() { Grupo = "Receita realizada", Valor = total.Receitas },
                new() { Grupo = "Despesa realizada", Valor = total.Despesas },
                new() { Grupo = "Resultado operacional", Valor = total.Receitas - total.Despesas }
            };
        }

        private sealed class FinanceiroPagamentoSyncRow
        {
            public Guid IdPagamento { get; set; }
            public int IdAgendamento { get; set; }
            public int IdPessoa { get; set; }
            public string? NomeServico { get; set; }
            public string? NomePessoa { get; set; }
            public string? Status { get; set; }
            public decimal Valor { get; set; }
            public string? Descricao { get; set; }
            public string? Tipo { get; set; }
            public DateTime DataReferencia { get; set; }
            public DateTime? PagoEm { get; set; }
            public DateTime CriadoEm { get; set; }
        }

        private sealed class FinanceiroDashboardKpiRow
        {
            public decimal ReceitasLiquidadas { get; set; }
            public decimal DespesasLiquidadas { get; set; }
            public decimal AReceberAberto { get; set; }
            public decimal APagarAberto { get; set; }
            public decimal ReceitasVencidas { get; set; }
            public decimal DespesasVencidas { get; set; }
            public int QuantidadeTitulosAbertos { get; set; }
            public int QuantidadeTitulosVencidos { get; set; }
            public decimal TicketMedioRecebido { get; set; }
        }

        private sealed class FinanceiroDreTotalRow
        {
            public decimal Receitas { get; set; }
            public decimal Despesas { get; set; }
        }
    }
}
