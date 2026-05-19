using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;

namespace CorteCor.Services
{
    public class FinanceiroService
    {
        private readonly IFinanceiroModuloHandler _financeiroHandler;
        private readonly IMemoryCache _cache;
        private readonly CompraHandler _compraHandler;

        public FinanceiroService(IFinanceiroModuloHandler financeiroHandler, IMemoryCache cache, CompraHandler compraHandler)
        {
            _financeiroHandler = financeiroHandler;
            _cache = cache;
            _compraHandler = compraHandler;
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
                    new PlanoContas { IdSalao = idSalao, Codigo = "1.1", Descricao = "Receitas de Servicos", Nome = "Receitas de Servicos", Tipo = "R", Nivel = 2, TipoConta = "Receita", NaturezaSaldo = "Credora", AceitaLancamento = true, Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "1.2", Descricao = "Outras Receitas", Nome = "Outras Receitas", Tipo = "R", Nivel = 2, TipoConta = "Outras Receitas", NaturezaSaldo = "Credora", AceitaLancamento = true, Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.1", Descricao = "Despesas Operacionais", Nome = "Despesas Operacionais", Tipo = "D", Nivel = 2, TipoConta = "Despesa", NaturezaSaldo = "Devedora", AceitaLancamento = true, Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.2", Descricao = "Impostos e Taxas", Nome = "Impostos e Taxas", Tipo = "D", Nivel = 2, TipoConta = "Despesa", NaturezaSaldo = "Devedora", AceitaLancamento = true, Ativo = true },
                    new PlanoContas { IdSalao = idSalao, Codigo = "2.3", Descricao = "Marketing e CRM", Nome = "Marketing e CRM", Tipo = "D", Nivel = 2, TipoConta = "Despesa", NaturezaSaldo = "Devedora", AceitaLancamento = true, Ativo = true }
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

        public async Task SincronizarTitulosPagamentoAsync(int idSalao)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);
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

        public async Task<FinanceiroDreResumo> ObterDreAsync(int idSalao, DateTime dataInicio, DateTime dataFim, string tipoPeriodo)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);

            var tipoNormalizado = string.Equals(tipoPeriodo, "Anual", StringComparison.OrdinalIgnoreCase) ? "Anual" : "Mensal";
            var meses = tipoNormalizado == "Anual"
                ? Enumerable.Range(1, 12).ToList()
                : new List<int> { dataInicio.Month };

            var movimentos = await _financeiroHandler.ObterMovimentosDreAsync(idSalao, dataInicio.Date, dataFim.Date);
            var categorias = new[]
            {
                "Receita Bruta",
                "Deduções da Receita",
                "Custos",
                "Despesas Comerciais",
                "Despesas Administrativas",
                "Despesas com Pessoal",
                "Despesas Operacionais Gerais",
                "Receitas Financeiras",
                "Despesas Financeiras",
                "Outras Receitas Operacionais",
                "Outras Despesas Operacionais",
                "IRPJ e CSLL",
                "Participações"
            };

            var totaisPorCategoria = categorias.ToDictionary(
                categoria => categoria,
                _ => meses.ToDictionary(mes => mes, _ => 0m),
                StringComparer.OrdinalIgnoreCase);

            var detalhesPorCategoria = categorias.ToDictionary(
                categoria => categoria,
                _ => new Dictionary<string, FinanceiroDreLinhaDemonstrativo>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            foreach (var movimento in movimentos)
            {
                var categoria = ClassificarCategoriaDre(movimento);
                if (!totaisPorCategoria.ContainsKey(categoria))
                {
                    totaisPorCategoria[categoria] = meses.ToDictionary(mes => mes, _ => 0m);
                    detalhesPorCategoria[categoria] = new Dictionary<string, FinanceiroDreLinhaDemonstrativo>(StringComparer.OrdinalIgnoreCase);
                }

                var mes = meses.Contains(movimento.Mes) ? movimento.Mes : dataInicio.Month;
                var valor = AplicarSinalDre(movimento, categoria);
                totaisPorCategoria[categoria][mes] += valor;

                var codigo = movimento.Codigo?.Trim();
                var descricao = string.IsNullOrWhiteSpace(codigo)
                    ? movimento.NomePlano
                    : $"{codigo} - {movimento.NomePlano}";
                var chave = $"{movimento.IdPlano?.ToString() ?? "sem-plano"}|{descricao}";
                if (!detalhesPorCategoria[categoria].TryGetValue(chave, out var linhaDetalhe))
                {
                    linhaDetalhe = new FinanceiroDreLinhaDemonstrativo
                    {
                        Descricao = descricao,
                        Nivel = 1,
                        ValoresPorMes = meses.ToDictionary(item => item, _ => 0m)
                    };
                    detalhesPorCategoria[categoria][chave] = linhaDetalhe;
                }

                linhaDetalhe.ValoresPorMes[mes] += valor;
            }

            foreach (var detalheCategoria in detalhesPorCategoria.Values.SelectMany(item => item.Values))
            {
                detalheCategoria.Total = meses.Sum(mes => detalheCategoria.ObterValor(mes));
            }

            Dictionary<int, decimal> ValoresCategoria(string categoria)
            {
                return totaisPorCategoria.TryGetValue(categoria, out var valores)
                    ? meses.ToDictionary(mes => mes, mes => valores.TryGetValue(mes, out var valor) ? valor : 0m)
                    : meses.ToDictionary(mes => mes, _ => 0m);
            }

            Dictionary<int, decimal> Somar(params string[] categoriasSomadas)
            {
                return meses.ToDictionary(
                    mes => mes,
                    mes => categoriasSomadas.Sum(categoria => ValoresCategoria(categoria).TryGetValue(mes, out var valor) ? valor : 0m));
            }

            FinanceiroDreLinhaDemonstrativo CriarLinha(
                string descricao,
                Dictionary<int, decimal> valores,
                int nivel = 0,
                bool destaque = false,
                bool subtotal = false,
                bool resultadoFinal = false)
            {
                return new FinanceiroDreLinhaDemonstrativo
                {
                    Descricao = descricao,
                    Nivel = nivel,
                    Destaque = destaque,
                    Subtotal = subtotal,
                    ResultadoFinal = resultadoFinal,
                    ValoresPorMes = valores,
                    Total = meses.Sum(mes => valores.TryGetValue(mes, out var valor) ? valor : 0m)
                };
            }

            void AdicionarCategoria(List<FinanceiroDreLinhaDemonstrativo> linhas, string descricao, string categoria)
            {
                linhas.Add(CriarLinha(descricao, ValoresCategoria(categoria), destaque: true));
                if (detalhesPorCategoria.TryGetValue(categoria, out var detalhes))
                {
                    foreach (var detalhe in detalhes.Values.Where(linha => linha.Total != 0m).OrderBy(linha => linha.Descricao))
                    {
                        linhas.Add(detalhe);
                    }
                }
            }

            var receitaLiquida = Somar("Receita Bruta", "Deduções da Receita");
            var lucroBruto = Somar("Receita Bruta", "Deduções da Receita", "Custos");
            var despesasOperacionais = Somar("Despesas Comerciais", "Despesas Administrativas", "Despesas com Pessoal", "Despesas Operacionais Gerais");
            var resultadoOperacional = Somar("Receita Bruta", "Deduções da Receita", "Custos", "Despesas Comerciais", "Despesas Administrativas", "Despesas com Pessoal", "Despesas Operacionais Gerais");
            var resultadoFinanceiro = Somar("Receitas Financeiras", "Despesas Financeiras");
            var resultadoAntesTributos = Somar("Receita Bruta", "Deduções da Receita", "Custos", "Despesas Comerciais", "Despesas Administrativas", "Despesas com Pessoal", "Despesas Operacionais Gerais", "Receitas Financeiras", "Despesas Financeiras", "Outras Receitas Operacionais", "Outras Despesas Operacionais");
            var resultadoLiquido = Somar("Receita Bruta", "Deduções da Receita", "Custos", "Despesas Comerciais", "Despesas Administrativas", "Despesas com Pessoal", "Despesas Operacionais Gerais", "Receitas Financeiras", "Despesas Financeiras", "Outras Receitas Operacionais", "Outras Despesas Operacionais", "IRPJ e CSLL", "Participações");

            var linhasDre = new List<FinanceiroDreLinhaDemonstrativo>();
            AdicionarCategoria(linhasDre, "(+) Receita Bruta Operacional", "Receita Bruta");
            AdicionarCategoria(linhasDre, "(-) Deduções da Receita Bruta", "Deduções da Receita");
            linhasDre.Add(CriarLinha("(=) Receita Líquida", receitaLiquida, destaque: true, subtotal: true));
            AdicionarCategoria(linhasDre, "(-) Custos", "Custos");
            linhasDre.Add(CriarLinha("(=) Lucro Bruto", lucroBruto, destaque: true, subtotal: true));
            AdicionarCategoria(linhasDre, "(-) Despesas Comerciais / Vendas", "Despesas Comerciais");
            AdicionarCategoria(linhasDre, "(-) Despesas Administrativas", "Despesas Administrativas");
            AdicionarCategoria(linhasDre, "(-) Despesas com Pessoal", "Despesas com Pessoal");
            AdicionarCategoria(linhasDre, "(-) Despesas Operacionais Gerais", "Despesas Operacionais Gerais");
            linhasDre.Add(CriarLinha("Total de Despesas Operacionais", despesasOperacionais, destaque: true, subtotal: true));
            linhasDre.Add(CriarLinha("(=) Resultado Operacional", resultadoOperacional, destaque: true, subtotal: true));
            AdicionarCategoria(linhasDre, "(+) Receitas Financeiras", "Receitas Financeiras");
            AdicionarCategoria(linhasDre, "(-) Despesas Financeiras", "Despesas Financeiras");
            linhasDre.Add(CriarLinha("(=) Resultado Financeiro", resultadoFinanceiro, destaque: true, subtotal: true));
            AdicionarCategoria(linhasDre, "(+) Outras Receitas Operacionais", "Outras Receitas Operacionais");
            AdicionarCategoria(linhasDre, "(-) Outras Despesas Operacionais", "Outras Despesas Operacionais");
            linhasDre.Add(CriarLinha("(=) Resultado antes de IRPJ/CSLL", resultadoAntesTributos, destaque: true, subtotal: true));
            AdicionarCategoria(linhasDre, "(-) IRPJ e CSLL", "IRPJ e CSLL");
            AdicionarCategoria(linhasDre, "(-) Participações", "Participações");
            linhasDre.Add(CriarLinha("(=) Resultado Líquido do Exercício", resultadoLiquido, destaque: true, subtotal: true, resultadoFinal: true));

            return new FinanceiroDreResumo
            {
                TipoPeriodo = tipoNormalizado,
                DataInicio = dataInicio.Date,
                DataFim = dataFim.Date,
                Meses = meses,
                Linhas = linhasDre,
                ReceitaLiquida = meses.Sum(mes => receitaLiquida[mes]),
                LucroBruto = meses.Sum(mes => lucroBruto[mes]),
                ResultadoOperacional = meses.Sum(mes => resultadoOperacional[mes]),
                ResultadoAntesTributos = meses.Sum(mes => resultadoAntesTributos[mes]),
                ResultadoLiquido = meses.Sum(mes => resultadoLiquido[mes])
            };
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

        public async Task<List<Guid>> SalvarTituloComRecorrenciaAsync(int idSalao, FinanceiroTitulo titulo)
        {
            var recorrencia = NormalizarRecorrencia(titulo.Recorrencia);
            var novoTitulo = titulo.IdTitulo == Guid.Empty;
            if (!novoTitulo || !string.Equals(recorrencia, RecorrenciaTipo.Mensal, StringComparison.OrdinalIgnoreCase))
            {
                titulo.Recorrencia = recorrencia;
                return new List<Guid> { await SalvarTituloAsync(idSalao, titulo) };
            }

            var hoje = DateTime.Today;
            var agora = DateTime.Now;
            var ano = hoje.Year;
            var liquidacaoBase = titulo.DataLiquidacao.HasValue ||
                string.Equals(titulo.Status, FinanceiroStatusTitulo.Liquidado, StringComparison.OrdinalIgnoreCase)
                    ? agora
                    : (DateTime?)null;
            var ids = new List<Guid>();

            for (var mes = hoje.Month; mes <= 12; mes++)
            {
                var recorrente = CopiarTituloParaMes(titulo, ano, mes, liquidacaoBase);
                ids.Add(await SalvarTituloAsync(idSalao, recorrente));
            }

            return ids;
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
            titulo.Recorrencia = NormalizarRecorrencia(titulo.Recorrencia);

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

            if (titulo.IdPlano.HasValue)
            {
                var plano = (await _financeiroHandler.ListarPlanoContasAsync(idSalao))
                    .FirstOrDefault(p => p.IdPlano == titulo.IdPlano.Value);

                if (plano == null || !plano.Ativo)
                {
                    throw new InvalidOperationException("Selecione uma conta do plano de contas ativa.");
                }

                if (!plano.AceitaLancamento)
                {
                    throw new InvalidOperationException("Selecione uma conta analitica do plano de contas. Contas agrupadoras nao aceitam lancamento.");
                }

                var tipoPlanoEsperado = MapearTipoPlanoPorTitulo(titulo.Tipo);
                if (!string.IsNullOrWhiteSpace(tipoPlanoEsperado) &&
                    !string.Equals(plano.Tipo, tipoPlanoEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    var tipoMensagem = string.Equals(titulo.Tipo, FinanceiroTipoTitulo.Receber, StringComparison.OrdinalIgnoreCase)
                        ? "recebimento"
                        : "pagamento";
                    throw new InvalidOperationException($"Selecione uma conta do plano de contas de {tipoMensagem}.");
                }
            }
            else if (string.Equals(titulo.Origem, FinanceiroOrigemTitulo.Manual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Selecione uma conta analitica do plano de contas.");
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

        public async Task<string> CancelarTituloAsync(int idSalao, Guid idTitulo)
        {
            var titulo = await _financeiroHandler.ObterTituloAsync(idSalao, idTitulo)
                ?? throw new InvalidOperationException("Lancamento financeiro nao encontrado.");

            if (string.Equals(titulo.Origem, FinanceiroOrigemTitulo.Compra, StringComparison.OrdinalIgnoreCase))
            {
                var resultadoCompra = await _compraHandler.CancelarCompraPorTituloAsync(
                    idSalao,
                    idTitulo,
                    null,
                    "Cancelamento da conta a pagar vinculada a compra.");

                if (resultadoCompra.CompraLocalizada)
                {
                    return resultadoCompra.EstoqueAjustado
                        ? "Titulo cancelado. A compra vinculada foi cancelada e o estoque foi ajustado automaticamente."
                        : "Titulo cancelado. A compra vinculada ja estava cancelada ou nao havia estoque a ajustar.";
                }
            }

            await _financeiroHandler.AtualizarStatusTituloAsync(idSalao, idTitulo, FinanceiroStatusTitulo.Cancelado, null, 0m, false);
            return "Titulo cancelado.";
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

        public async Task<List<PlanoContas>> ListarGruposPlanoContasAsync(int idSalao)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            return await _financeiroHandler.ListarGruposPlanoContasAsync(idSalao);
        }

        public async Task<List<PlanoContas>> ListarGruposPlanoContasAsync(int idSalao, string? tipoTitulo)
        {
            var grupos = await ListarGruposPlanoContasAsync(idSalao);
            return FiltrarPlanosPorTipoTitulo(grupos, tipoTitulo);
        }

        public async Task<List<PlanoContas>> ListarContasAnaliticasPorGrupoAsync(int idSalao, int idGrupoPlano)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            return await _financeiroHandler.ListarContasAnaliticasPorGrupoAsync(idSalao, idGrupoPlano);
        }

        public async Task<List<PlanoContas>> ListarContasAnaliticasPorGrupoAsync(int idSalao, int idGrupoPlano, string? tipoTitulo)
        {
            var contas = await ListarContasAnaliticasPorGrupoAsync(idSalao, idGrupoPlano);
            return FiltrarPlanosPorTipoTitulo(contas, tipoTitulo);
        }

        public async Task<PlanoContas?> ObterGrupoNivel2DoPlanoAsync(int idSalao, int? idPlano)
        {
            if (!idPlano.HasValue || idPlano <= 0)
            {
                return null;
            }

            var planos = await _financeiroHandler.ListarPlanoContasAsync(idSalao);
            var porId = planos.ToDictionary(p => p.IdPlano);
            if (!porId.TryGetValue(idPlano.Value, out var atual))
            {
                return null;
            }

            while (atual.IdPlanoPai.HasValue && porId.TryGetValue(atual.IdPlanoPai.Value, out var pai))
            {
                atual = pai;
                if (atual.Nivel == 2)
                {
                    return atual;
                }
            }

            var codigo = atual.Codigo ?? string.Empty;
            var partes = codigo.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2)
            {
                return null;
            }

            var codigoGrupo = $"{partes[0]}.{partes[1]}";
            return planos.FirstOrDefault(p => string.Equals(p.Codigo, codigoGrupo, StringComparison.OrdinalIgnoreCase) && p.Nivel == 2);
        }

        public async Task SavePlanoContasAsync(int idSalao, PlanoContas plano)
        {
            var planos = await _financeiroHandler.ListarPlanoContasAsync(idSalao);
            plano.IdSalao = idSalao;
            plano.Codigo = plano.Codigo?.Trim();
            plano.Descricao = string.IsNullOrWhiteSpace(plano.Descricao) ? plano.Nome?.Trim() ?? string.Empty : plano.Descricao.Trim();
            plano.Nome = string.IsNullOrWhiteSpace(plano.Nome) ? plano.Descricao : plano.Nome.Trim();
            plano.Nivel = CalcularNivelPlano(plano.Codigo);
            plano.Tipo = string.Equals(plano.Tipo, "R", StringComparison.OrdinalIgnoreCase) ? "R" : "D";
            plano.TipoConta = string.IsNullOrWhiteSpace(plano.TipoConta) ? InferirTipoConta(plano.Codigo) : plano.TipoConta.Trim();
            plano.NaturezaSaldo = string.IsNullOrWhiteSpace(plano.NaturezaSaldo) ? InferirNaturezaSaldo(plano.Codigo) : plano.NaturezaSaldo.Trim();
            plano.GrupoDRE = string.IsNullOrWhiteSpace(plano.GrupoDRE) ? null : plano.GrupoDRE.Trim();
            plano.IdPlanoPai = plano.IdPlanoPai.GetValueOrDefault() > 0 ? plano.IdPlanoPai : null;

            if (string.IsNullOrWhiteSpace(plano.Codigo))
            {
                throw new InvalidOperationException("Informe o código do plano de contas.");
            }

            if (string.IsNullOrWhiteSpace(plano.Descricao))
            {
                throw new InvalidOperationException("Informe a descrição do plano de contas.");
            }

            if (planos.Any(p => p.IdPlano != plano.IdPlano && string.Equals(p.Codigo, plano.Codigo, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Já existe um plano de contas cadastrado com este código.");
            }

            if (!plano.IdPlanoPai.HasValue)
            {
                var codigoPai = ObterCodigoPai(plano.Codigo);
                var paiPorCodigo = planos.FirstOrDefault(p => string.Equals(p.Codigo, codigoPai, StringComparison.OrdinalIgnoreCase));
                plano.IdPlanoPai = paiPorCodigo?.IdPlano;
            }

            if (plano.IdPlanoPai.HasValue)
            {
                var pai = planos.FirstOrDefault(p => p.IdPlano == plano.IdPlanoPai.Value)
                    ?? throw new InvalidOperationException("A conta pai selecionada não foi encontrada.");

                if (pai.IdPlano == plano.IdPlano)
                {
                    throw new InvalidOperationException("A conta não pode ser pai dela mesma.");
                }

                if (!string.IsNullOrWhiteSpace(pai.Codigo) &&
                    !string.IsNullOrWhiteSpace(plano.Codigo) &&
                    !plano.Codigo.StartsWith($"{pai.Codigo}.", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("O código da conta deve ser descendente do código da conta pai selecionada.");
                }
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

        public async Task<FinanceiroFluxoCaixaResumo> ObterFluxoCaixaAsync(int idSalao, DateTime dataInicio, DateTime dataFim, string? visao, bool projetado)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            await _financeiroHandler.SincronizarTitulosPagamentoAsync(idSalao);

            if (dataFim.Date < dataInicio.Date)
            {
                (dataInicio, dataFim) = (dataFim, dataInicio);
            }

            var visaoNormalizada = string.Equals(visao, "Ano", StringComparison.OrdinalIgnoreCase)
                ? "Ano"
                : string.Equals(visao, "Dia", StringComparison.OrdinalIgnoreCase)
                    ? "Dia"
                    : "Mes";

            var linhasDiarias = await _financeiroHandler.ObterFluxoCaixaAsync(idSalao, dataInicio.Date, dataFim.Date, projetado);
            var linhas = visaoNormalizada == "Ano"
                ? ConsolidarFluxoPorMes(linhasDiarias)
                : linhasDiarias;

            return new FinanceiroFluxoCaixaResumo
            {
                Visao = visaoNormalizada,
                DataInicio = dataInicio.Date,
                DataFim = dataFim.Date,
                Linhas = linhas
            };
        }

        public async Task<PlanoContas?> ObterPlanoReceitaPadraoVendaAsync(int idSalao)
        {
            await GarantirEstruturaBaseAsync(idSalao);
            var planos = await _financeiroHandler.ListarPlanoContasAsync(idSalao);
            return planos
                .Where(plano => plano.Ativo && plano.AceitaLancamento && string.Equals(plano.Tipo, "R", StringComparison.OrdinalIgnoreCase))
                .OrderBy(plano => ObterPrioridadePlanoReceitaVenda(plano.Codigo))
                .ThenBy(plano => plano.Codigo)
                .FirstOrDefault();
        }

        private static string ClassificarCategoriaDre(FinanceiroDreMovimento movimento)
        {
            var codigo = movimento.Codigo?.Trim() ?? string.Empty;
            if (codigo == "4.1" || codigo.StartsWith("4.1.", StringComparison.OrdinalIgnoreCase))
            {
                return "Receita Bruta";
            }

            if (codigo == "5.1" || codigo.StartsWith("5.1.", StringComparison.OrdinalIgnoreCase) ||
                codigo == "5.2" || codigo.StartsWith("5.2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Deduções da Receita";
            }

            if (codigo == "6.1" || codigo.StartsWith("6.1.", StringComparison.OrdinalIgnoreCase) ||
                codigo == "6.2" || codigo.StartsWith("6.2.", StringComparison.OrdinalIgnoreCase) ||
                codigo == "6.3" || codigo.StartsWith("6.3.", StringComparison.OrdinalIgnoreCase))
            {
                return "Custos";
            }

            if (codigo == "7.1" || codigo.StartsWith("7.1.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesas Comerciais";
            }

            if (codigo == "7.2" || codigo.StartsWith("7.2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesas Administrativas";
            }

            if (codigo == "7.3" || codigo.StartsWith("7.3.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesas com Pessoal";
            }

            if (codigo == "7.4" || codigo.StartsWith("7.4.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesas Operacionais Gerais";
            }

            if (codigo == "8.1" || codigo.StartsWith("8.1.", StringComparison.OrdinalIgnoreCase))
            {
                return "Receitas Financeiras";
            }

            if (codigo == "8.2" || codigo.StartsWith("8.2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesas Financeiras";
            }

            if (codigo == "9.1" || codigo.StartsWith("9.1.", StringComparison.OrdinalIgnoreCase))
            {
                return "Outras Receitas Operacionais";
            }

            if (codigo == "9.2" || codigo.StartsWith("9.2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Outras Despesas Operacionais";
            }

            if (codigo == "10.1" || codigo.StartsWith("10.1.", StringComparison.OrdinalIgnoreCase))
            {
                return "IRPJ e CSLL";
            }

            if (codigo == "10.2" || codigo.StartsWith("10.2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Participações";
            }

            var grupo = NormalizarTextoDre(movimento.GrupoDRE);
            if (grupo.Contains("receita bruta"))
            {
                return "Receita Bruta";
            }

            if (grupo.Contains("deducoes") || grupo.Contains("cancelamentos"))
            {
                return "Deduções da Receita";
            }

            if (grupo.Contains("custo"))
            {
                return "Custos";
            }

            if (grupo.Contains("comerciais"))
            {
                return "Despesas Comerciais";
            }

            if (grupo.Contains("administrativas"))
            {
                return "Despesas Administrativas";
            }

            if (grupo.Contains("pessoal"))
            {
                return "Despesas com Pessoal";
            }

            if (grupo.Contains("operacionais gerais"))
            {
                return "Despesas Operacionais Gerais";
            }

            if (grupo.Contains("resultado financeiro"))
            {
                var tipoConta = NormalizarTextoDre(movimento.TipoConta);
                return tipoConta.Contains("receita") ? "Receitas Financeiras" : "Despesas Financeiras";
            }

            if (grupo.Contains("outras receitas"))
            {
                return "Outras Receitas Operacionais";
            }

            if (grupo.Contains("outras despesas"))
            {
                return "Outras Despesas Operacionais";
            }

            if (grupo.Contains("irpj") || grupo.Contains("csll") || grupo.Contains("tributo"))
            {
                return "IRPJ e CSLL";
            }

            if (grupo.Contains("participacoes"))
            {
                return "Participações";
            }

            return string.Equals(movimento.Tipo, FinanceiroTipoTitulo.Receber, StringComparison.OrdinalIgnoreCase)
                ? "Receita Bruta"
                : "Despesas Operacionais Gerais";
        }

        private static decimal AplicarSinalDre(FinanceiroDreMovimento movimento, string categoria)
        {
            var valor = Math.Abs(movimento.Valor);
            return categoria is "Receita Bruta" or "Receitas Financeiras" or "Outras Receitas Operacionais"
                ? valor
                : -valor;
        }

        private static string NormalizarTextoDre(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizado.Length);
            foreach (var caractere in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(caractere);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
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

        private static string NormalizarRecorrencia(string? recorrencia)
        {
            return string.Equals(recorrencia?.Trim(), RecorrenciaTipo.Mensal, StringComparison.OrdinalIgnoreCase)
                ? RecorrenciaTipo.Mensal
                : RecorrenciaTipo.Nenhuma;
        }

        private static FinanceiroTitulo CopiarTituloParaMes(
            FinanceiroTitulo titulo,
            int ano,
            int mes,
            DateTime? liquidacaoBase)
        {
            var dataRecorrencia = AjustarDataParaMes(DateTime.Today, ano, mes);
            return new FinanceiroTitulo
            {
                IdTitulo = Guid.NewGuid(),
                IdSalao = titulo.IdSalao,
                Tipo = titulo.Tipo,
                Origem = titulo.Origem,
                IdPessoa = titulo.IdPessoa,
                IdAgendamento = titulo.IdAgendamento,
                IdVendaProduto = titulo.IdVendaProduto,
                IdPagamento = titulo.IdPagamento,
                IdPlano = titulo.IdPlano,
                IdConta = titulo.IdConta,
                Descricao = titulo.Descricao,
                Documento = titulo.Documento,
                Status = titulo.Status,
                Recorrencia = RecorrenciaTipo.Mensal,
                ValorOriginal = titulo.ValorOriginal,
                ValorLiquidado = titulo.ValorLiquidado,
                ValorAberto = titulo.ValorAberto,
                DataCompetencia = dataRecorrencia,
                DataVencimento = dataRecorrencia,
                DataLiquidacao = liquidacaoBase.HasValue ? AjustarDataHoraParaMes(liquidacaoBase.Value, ano, mes) : null,
                Conciliado = titulo.Conciliado,
                Observacoes = titulo.Observacoes
            };
        }

        private static DateTime AjustarDataParaMes(DateTime dataBase, int ano, int mes)
        {
            var dia = Math.Min(dataBase.Day, DateTime.DaysInMonth(ano, mes));
            return new DateTime(ano, mes, dia);
        }

        private static DateTime AjustarDataHoraParaMes(DateTime dataBase, int ano, int mes)
        {
            var data = AjustarDataParaMes(dataBase, ano, mes);
            return data.Date.Add(dataBase.TimeOfDay);
        }

        private static List<PlanoContas> FiltrarPlanosPorTipoTitulo(IEnumerable<PlanoContas> planos, string? tipoTitulo)
        {
            var tipoPlano = MapearTipoPlanoPorTitulo(tipoTitulo);
            return string.IsNullOrWhiteSpace(tipoPlano)
                ? planos.ToList()
                : planos.Where(plano => string.Equals(plano.Tipo, tipoPlano, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private static string? MapearTipoPlanoPorTitulo(string? tipoTitulo)
        {
            var tipo = tipoTitulo?.Trim();
            if (string.IsNullOrWhiteSpace(tipo))
            {
                return null;
            }

            if (tipo.Equals(FinanceiroTipoTitulo.Receber, StringComparison.OrdinalIgnoreCase) ||
                tipo.Equals("R", StringComparison.OrdinalIgnoreCase))
            {
                return "R";
            }

            if (tipo.Equals(FinanceiroTipoTitulo.Pagar, StringComparison.OrdinalIgnoreCase) ||
                tipo.Equals("D", StringComparison.OrdinalIgnoreCase))
            {
                return "D";
            }

            return null;
        }

        private static List<FinanceiroFluxoCaixaItem> ConsolidarFluxoPorMes(IReadOnlyCollection<FinanceiroFluxoCaixaItem> linhasDiarias)
        {
            var saldo = linhasDiarias.FirstOrDefault()?.SaldoAcumulado ?? 0m;
            var grupos = linhasDiarias
                .GroupBy(linha => new DateTime(linha.Data.Year, linha.Data.Month, 1))
                .OrderBy(grupo => grupo.Key)
                .ToList();

            var resultado = new List<FinanceiroFluxoCaixaItem>();
            foreach (var grupo in grupos)
            {
                var entradas = grupo.Sum(linha => linha.Entradas);
                var saidas = grupo.Sum(linha => linha.Saidas);
                saldo = grupo.Last().SaldoAcumulado;
                resultado.Add(new FinanceiroFluxoCaixaItem
                {
                    Data = grupo.Key,
                    Entradas = entradas,
                    Saidas = saidas,
                    SaldoAcumulado = saldo
                });
            }

            return resultado;
        }

        private static int ObterPrioridadePlanoReceitaVenda(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return 99;
            }

            if (codigo.StartsWith("4.1.01", StringComparison.OrdinalIgnoreCase) ||
                codigo.StartsWith("4.1.02", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return codigo.StartsWith("4.1", StringComparison.OrdinalIgnoreCase) ? 1 : 10;
        }

        private static int CalcularNivelPlano(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return 1;
            }

            return codigo.Count(c => c == '.') + 1;
        }

        private static string? ObterCodigoPai(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            var ultimoPonto = codigo.LastIndexOf('.');
            return ultimoPonto <= 0 ? null : codigo[..ultimoPonto];
        }

        private static string? InferirTipoConta(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            if (codigo.StartsWith("10.1", StringComparison.OrdinalIgnoreCase))
            {
                return "Tributo sobre Lucro";
            }

            if (codigo.StartsWith("10.2", StringComparison.OrdinalIgnoreCase))
            {
                return "Participacao";
            }

            if (codigo == "1" || codigo.StartsWith("1.", StringComparison.OrdinalIgnoreCase))
            {
                return "Ativo";
            }

            if (codigo == "2" || codigo.StartsWith("2.", StringComparison.OrdinalIgnoreCase))
            {
                return "Passivo";
            }

            if (codigo == "3" || codigo.StartsWith("3.", StringComparison.OrdinalIgnoreCase))
            {
                return "Patrimonio Liquido";
            }

            if (codigo == "4" || codigo.StartsWith("4.", StringComparison.OrdinalIgnoreCase))
            {
                return "Receita";
            }

            if (codigo == "5" || codigo.StartsWith("5.", StringComparison.OrdinalIgnoreCase))
            {
                return "Deducao da Receita";
            }

            if (codigo == "6" || codigo.StartsWith("6.", StringComparison.OrdinalIgnoreCase))
            {
                return "Custo";
            }

            if (codigo == "7" || codigo.StartsWith("7.", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesa";
            }

            if (codigo.StartsWith("8.1", StringComparison.OrdinalIgnoreCase))
            {
                return "Receita Financeira";
            }

            if (codigo.StartsWith("8.2", StringComparison.OrdinalIgnoreCase))
            {
                return "Despesa Financeira";
            }

            if (codigo.StartsWith("9.1", StringComparison.OrdinalIgnoreCase))
            {
                return "Outras Receitas";
            }

            if (codigo.StartsWith("9.2", StringComparison.OrdinalIgnoreCase))
            {
                return "Outras Despesas";
            }

            return null;
        }

        private static string InferirNaturezaSaldo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return "Devedora";
            }

            return codigo == "2" ||
                   codigo.StartsWith("2.", StringComparison.OrdinalIgnoreCase) ||
                   codigo == "3" ||
                   codigo.StartsWith("3.", StringComparison.OrdinalIgnoreCase) ||
                   codigo == "4" ||
                   codigo.StartsWith("4.", StringComparison.OrdinalIgnoreCase) ||
                   codigo.StartsWith("8.1", StringComparison.OrdinalIgnoreCase) ||
                   codigo.StartsWith("9.1", StringComparison.OrdinalIgnoreCase)
                ? "Credora"
                : "Devedora";
        }
    }
}
