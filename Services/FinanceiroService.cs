using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CorteCor.Services
{
    public class FinanceiroService
    {
        private readonly IFinanceiroModuloHandler _financeiroHandler;
        private readonly IMemoryCache _cache;

        public FinanceiroService(IFinanceiroModuloHandler financeiroHandler, IMemoryCache cache)
        {
            _financeiroHandler = financeiroHandler;
            _cache = cache;
        }

        public async Task GarantirEstruturaBaseAsync(int idSalao)
        {
            var cacheKey = $"financeiro:estrutura-base:{idSalao}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            var planos = await _financeiroHandler.ListarPlanoContasAsync(idSalao);
            if (!planos.Any())
            {
                var padroes = new[]
                {
                    new PlanoContas { IdSalao = idSalao, Codigo = "1.1", Descricao = "Receitas de Servicos", Tipo = "R", Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "1.2", Descricao = "Outras Receitas", Tipo = "R", Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.1", Descricao = "Despesas Operacionais", Tipo = "D", Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.2", Descricao = "Impostos e Taxas", Tipo = "D", Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.3", Descricao = "Marketing e CRM", Tipo = "D", Ativo = true }
                };

                foreach (var plano in padroes)
                {
                    await _financeiroHandler.SavePlanoContasAsync(plano);
                }
            }

            var contas = await _financeiroHandler.ListarContasCaixaAsync(idSalao);
            if (!contas.Any())
            {
                await _financeiroHandler.SaveContaCaixaAsync(new ContaCaixa
                {
                    IdSalao = idSalao,
                    Nome = "Caixa Principal",
                    Tipo = "Caixa",
                    SaldoInicial = 0,
                    Ativo = true
                });
            }

            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(15));
        }

        public async Task<FinanceiroDashboardResumo> ObterDashboardAsync(int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);
            return await _financeiroHandler.ObterDashboardAsync(idSalao, dataInicio, dataFim);
        }

        public async Task<FinanceiroRelatorioResumo> ObterRelatoriosAsync(int idSalao, DateTime dataInicio, DateTime dataFim)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);
            return await _financeiroHandler.ObterRelatoriosAsync(idSalao, dataInicio, dataFim);
        }

        public async Task<PagedResult<FinanceiroTitulo>> ListarTitulosAsync(int idSalao, FinanceiroTituloFiltro filtro)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);
            return await _financeiroHandler.ListarTitulosAsync(idSalao, filtro);
        }

        public async Task<FinanceiroTitulo?> ObterTituloAsync(int idSalao, Guid idTitulo)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);
            return await _financeiroHandler.ObterTituloAsync(idSalao, idTitulo);
        }

        public async Task<List<FinanceiroTitulo>> ListarTitulosPorVendaAsync(int idSalao, int idVendaProduto)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            return await _financeiroHandler.ListarTitulosPorVendaAsync(idSalao, idVendaProduto);
        }

        public async Task<Guid> SalvarTituloAsync(int idSalao, FinanceiroTitulo titulo)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            titulo.IdSalao = idSalao;
            titulo.Descricao = titulo.Descricao?.Trim() ?? string.Empty;
            titulo.Documento = titulo.Documento?.Trim();
            titulo.Observacoes = titulo.Observacoes?.Trim();
            titulo.Tipo = string.Equals(titulo.Tipo, FinanceiroTipoTitulo.Pagar, StringComparison.OrdinalIgnoreCase)
                ? FinanceiroTipoTitulo.Pagar
                : FinanceiroTipoTitulo.Receber;
            titulo.Origem = string.IsNullOrWhiteSpace(titulo.Origem) ? FinanceiroOrigemTitulo.Manual : titulo.Origem.Trim();

            if (string.IsNullOrWhiteSpace(titulo.Descricao))
            {
                throw new InvalidOperationException("Informe a descrição do lançamento financeiro.");
            }

            if (titulo.ValorOriginal <= 0)
            {
                throw new InvalidOperationException("Informe um valor maior que zero para o lançamento.");
            }

            if (titulo.DataCompetencia == default)
            {
                titulo.DataCompetencia = DateTime.Today;
            }

            if (titulo.DataVencimento == default)
            {
                titulo.DataVencimento = titulo.DataCompetencia;
            }

            if (titulo.IdConta <= 0)
            {
                titulo.IdConta = null;
            }

            if (titulo.IdPlano <= 0)
            {
                titulo.IdPlano = null;
            }

            titulo.Status = NormalizarStatusTitulo(titulo.Status, titulo.DataVencimento, titulo.DataLiquidacao);
            titulo.ValorLiquidado = titulo.Status == FinanceiroStatusTitulo.Liquidado ? (titulo.ValorLiquidado > 0 ? titulo.ValorLiquidado : titulo.ValorOriginal) : 0m;
            titulo.ValorAberto = titulo.Status == FinanceiroStatusTitulo.Liquidado || titulo.Status == FinanceiroStatusTitulo.Cancelado
                ? 0m
                : titulo.ValorOriginal - titulo.ValorLiquidado;
            titulo.DataLiquidacao = titulo.Status == FinanceiroStatusTitulo.Liquidado
                ? titulo.DataLiquidacao ?? DateTime.Now
                : null;
            titulo.Conciliado = titulo.Conciliado && titulo.Status == FinanceiroStatusTitulo.Liquidado;

            return await _financeiroHandler.SalvarTituloAsync(titulo);
        }

        public async Task LiquidarTituloAsync(int idSalao, Guid idTitulo, decimal? valorLiquidado, DateTime? dataLiquidacao, bool conciliado)
        {
            await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, idTitulo, FinanceiroStatusTitulo.Liquidado, dataLiquidacao ?? DateTime.Now, valorLiquidado, conciliado);
        }

        public async Task ReabrirTituloAsync(int idSalao, Guid idTitulo)
        {
            var titulo = await _financeiroHandler.ObterTituloAsync(idSalao, idTitulo)
                ?? throw new InvalidOperationException("Lançamento financeiro não encontrado.");
            var status = titulo.DataVencimento.Date < DateTime.Today ? FinanceiroStatusTitulo.Vencido : FinanceiroStatusTitulo.Aberto;
            await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, idTitulo, status, null, 0m, false);
        }

        public async Task CancelarTituloAsync(int idSalao, Guid idTitulo)
        {
            await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, idTitulo, FinanceiroStatusTitulo.Cancelado, null, 0m, false);
        }

        public async Task AlternarConciliacaoAsync(int idSalao, Guid idTitulo, bool conciliado)
        {
            var titulo = await _financeiroHandler.ObterTituloAsync(idSalao, idTitulo)
                ?? throw new InvalidOperationException("Lançamento financeiro não encontrado.");
            await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, idTitulo, titulo.Status, titulo.DataLiquidacao, titulo.ValorLiquidado, conciliado);
        }

        public async Task CancelarTitulosDaVendaAsync(int idSalao, int idVendaProduto)
        {
            var titulos = await _financeiroHandler.ListarTitulosPorVendaAsync(idSalao, idVendaProduto);
            foreach (var titulo in titulos.Where(t => !string.Equals(t.Status, FinanceiroStatusTitulo.Cancelado, StringComparison.OrdinalIgnoreCase)))
            {
                await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, titulo.IdTitulo, FinanceiroStatusTitulo.Cancelado, null, 0m, false);
            }
        }

        public async Task<string> AplicarAjustePosVendaAsync(int idSalao, int idVendaProduto, int? idPessoa, decimal diferencaFinanceira, string descricaoBase, string? observacoes)
        {
            await GarantirEstruturaBaseAsync(idSalao);

            if (diferencaFinanceira == 0m)
            {
                return "Nenhum ajuste financeiro foi necessário.";
            }

            if (diferencaFinanceira > 0m)
            {
                await SalvarTituloAsync(idSalao, new FinanceiroTitulo
                {
                    Tipo = FinanceiroTipoTitulo.Receber,
                    Origem = FinanceiroOrigemTitulo.PosVenda,
                    IdPessoa = idPessoa,
                    IdVendaProduto = idVendaProduto,
                    Descricao = descricaoBase,
                    Documento = $"PV-{idVendaProduto}-{DateTime.Now:yyyyMMddHHmmss}",
                    Status = FinanceiroStatusTitulo.Aberto,
                    ValorOriginal = diferencaFinanceira,
                    ValorLiquidado = 0m,
                    ValorAberto = diferencaFinanceira,
                    DataCompetencia = DateTime.Today,
                    DataVencimento = DateTime.Today,
                    Observacoes = observacoes
                });

                return $"Foi criado um título a receber de R$ {diferencaFinanceira:N2} para complementar a pós-venda.";
            }

            var creditoRestante = Math.Abs(diferencaFinanceira);
            var titulos = await _financeiroHandler.ListarTitulosPorVendaAsync(idSalao, idVendaProduto);
            var titulosReceber = titulos
                .Where(t => string.Equals(t.Tipo, FinanceiroTipoTitulo.Receber, StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(t.Status, FinanceiroStatusTitulo.Cancelado, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => string.Equals(t.Status, FinanceiroStatusTitulo.Liquidado, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(t => t.DataVencimento)
                .ToList();

            foreach (var titulo in titulosReceber.Where(t => creditoRestante > 0m && t.ValorAberto > 0m))
            {
                var abatimento = Math.Min(creditoRestante, titulo.ValorAberto);
                if (abatimento <= 0m)
                {
                    continue;
                }

                creditoRestante -= abatimento;
                var novoValorOriginal = Math.Max(0m, titulo.ValorOriginal - abatimento);
                var novoValorAberto = Math.Max(0m, titulo.ValorAberto - abatimento);
                var novoValorLiquidado = titulo.ValorLiquidado;
                var novoStatus = novoValorAberto > 0m
                    ? (titulo.DataVencimento.Date < DateTime.Today ? FinanceiroStatusTitulo.Vencido : FinanceiroStatusTitulo.Aberto)
                    : (novoValorLiquidado > 0m ? FinanceiroStatusTitulo.Liquidado : FinanceiroStatusTitulo.Cancelado);

                await _financeiroHandler.AtualizarValoresTituloAsync(
                    idSalao,
                    titulo.IdTitulo,
                    novoValorOriginal,
                    novoValorLiquidado,
                    novoValorAberto,
                    novoStatus,
                    novoStatus == FinanceiroStatusTitulo.Liquidado ? titulo.DataLiquidacao : null,
                    novoStatus == FinanceiroStatusTitulo.Liquidado && titulo.Conciliado,
                    observacoes);
            }

            if (creditoRestante > 0m)
            {
                await SalvarTituloAsync(idSalao, new FinanceiroTitulo
                {
                    Tipo = FinanceiroTipoTitulo.Pagar,
                    Origem = FinanceiroOrigemTitulo.PosVenda,
                    IdPessoa = idPessoa,
                    IdVendaProduto = idVendaProduto,
                    Descricao = descricaoBase,
                    Documento = $"PVC-{idVendaProduto}-{DateTime.Now:yyyyMMddHHmmss}",
                    Status = FinanceiroStatusTitulo.Aberto,
                    ValorOriginal = creditoRestante,
                    ValorLiquidado = 0m,
                    ValorAberto = creditoRestante,
                    DataCompetencia = DateTime.Today,
                    DataVencimento = DateTime.Today,
                    Observacoes = observacoes
                });

                return $"Os títulos a receber foram abatidos e ficou um crédito de R$ {creditoRestante:N2} registrado no financeiro.";
            }

            return "Os títulos da venda foram ajustados no financeiro.";
        }

        public async Task<List<PlanoContas>> ListarPlanoContasAsync(int idSalao)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            return await _financeiroHandler.ListarPlanoContasAsync(idSalao);
        }

        public async Task SavePlanoContasAsync(int idSalao, PlanoContas plano)
        {
            plano.IdSalao = idSalao;
            plano.Codigo = plano.Codigo?.Trim();
            plano.Descricao = plano.Descricao?.Trim() ?? string.Empty;
            plano.Tipo = string.Equals(plano.Tipo, "D", StringComparison.OrdinalIgnoreCase) ? "D" : "R";
            if (string.IsNullOrWhiteSpace(plano.Descricao))
            {
                throw new InvalidOperationException("Informe a descrição do plano de contas.");
            }

            await _financeiroHandler.SavePlanoContasAsync(plano);
        }

        public async Task<List<ContaCaixa>> ListarContasCaixaAsync(int idSalao)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            return await _financeiroHandler.ListarContasCaixaAsync(idSalao);
        }

        public async Task SaveContaCaixaAsync(int idSalao, ContaCaixa conta)
        {
            conta.IdSalao = idSalao;
            conta.Nome = conta.Nome?.Trim() ?? string.Empty;
            conta.Tipo = string.IsNullOrWhiteSpace(conta.Tipo) ? "Caixa" : conta.Tipo.Trim();
            conta.Banco = conta.Banco?.Trim();
            conta.Agencia = conta.Agencia?.Trim();
            conta.Conta = conta.Conta?.Trim();
            if (string.IsNullOrWhiteSpace(conta.Nome))
            {
                throw new InvalidOperationException("Informe o nome da conta caixa.");
            }

            await _financeiroHandler.SaveContaCaixaAsync(conta);
        }

        private static string NormalizarStatusTitulo(string? status, DateTime dataVencimento, DateTime? dataLiquidacao)
        {
            if (string.Equals(status, FinanceiroStatusTitulo.Cancelado, StringComparison.OrdinalIgnoreCase))
            {
                return FinanceiroStatusTitulo.Cancelado;
            }

            if (string.Equals(status, FinanceiroStatusTitulo.Liquidado, StringComparison.OrdinalIgnoreCase) || dataLiquidacao.HasValue)
            {
                return FinanceiroStatusTitulo.Liquidado;
            }

            return dataVencimento.Date < DateTime.Today ? FinanceiroStatusTitulo.Vencido : FinanceiroStatusTitulo.Aberto;
        }
    }
}
