using System.Security.Claims;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages;

[Authorize(Policy = "UsuarioPolicy")]
public class DashboardModel : PageModel
{
    private readonly IDatabaseHandler _databaseHandler;
    private readonly CrmService _crmService;
    private readonly FinanceiroService _financeiroService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        IDatabaseHandler databaseHandler,
        CrmService crmService,
        FinanceiroService financeiroService,
        ILogger<DashboardModel> logger)
    {
        _databaseHandler = databaseHandler;
        _crmService = crmService;
        _financeiroService = financeiroService;
        _logger = logger;
    }

    public string NomeUsuario { get; private set; } = "Usuário";
    public string NomeEmpresa { get; private set; } = "Empresa";
    public string Saudacao { get; private set; } = "Olá";
    public string PeriodoAtual { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await CarregarContextoBasicoAsync();
    }

    public async Task<IActionResult> OnGetDataAsync()
    {
        var idSalao = ObterIdSalao();
        if (idSalao <= 0)
        {
            return BadRequest(new { message = "Não foi possível identificar a empresa do usuário." });
        }

        try
        {
            return await CarregarDadosDashboardAsync(idSalao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar dados do dashboard para o salao {IdSalao}.", idSalao);
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return new JsonResult(new DashboardErrorPayload
            {
                Message = "Nao foi possivel carregar o dashboard no momento. Tente novamente em instantes."
            });
        }
    }

    private async Task<IActionResult> CarregarDadosDashboardAsync(int idSalao)
    {
        await CarregarContextoBasicoAsync();

        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);
        var inicioSerie = inicioMes.AddMonths(-5);

        var avisos = new List<string>();
        var crmDashboard = new CrmDashboardResumo();
        var financeiroDashboard = new FinanceiroDashboardResumo();
        var aggregate = new DashboardAggregateRow();
        var serieCompleta = CriarSerieVendasVazia(inicioSerie);
        var agendamentosPorStatus = new List<DashboardCategoryRow>();

        try
        {
            crmDashboard = _crmService.ObterDashboard(idSalao);
        }
        catch (Exception ex) when (MySqlOperationalResilience.IsMaxUserConnections(ex))
        {
            avisos.Add("crm-indisponivel");
            _logger.LogWarning(ex, "Dashboard CRM operando em modo degradado para o salão {IdSalao}.", idSalao);
        }

        try
        {
            financeiroDashboard = await _financeiroService.ObterDashboardAsync(idSalao, inicioMes, hoje);
        }
        catch (Exception ex) when (MySqlOperationalResilience.IsMaxUserConnections(ex))
        {
            avisos.Add("financeiro-indisponivel");
            _logger.LogWarning(ex, "Dashboard financeiro operando em modo degradado para o salão {IdSalao}.", idSalao);
        }

        try
        {
            using var conn = _databaseHandler.GetConnection();

            aggregate = await conn.QueryFirstAsync<DashboardAggregateRow>(
                @"
SELECT
    (
        SELECT COUNT(1)
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND ISNULL(IsCliente, 0) = 1
          AND ISNULL(Excluido, 0) = 0
    ) AS TotalClientes,
    (
        SELECT COUNT(1)
        FROM CorteCor_Agendamento
        WHERE ISNULL(Excluido, 0) = 0
          AND EXISTS (
              SELECT 1
              FROM CorteCor_Servico S
              WHERE S.IdServico = CorteCor_Agendamento.IdServico
                AND S.IdSalao = @IdSalao
          )
          AND CAST(DataHora AS date) = @Hoje
    ) AS AgendamentosHoje,
    (
        SELECT COUNT(1)
        FROM CorteCor_Agendamento
        WHERE ISNULL(Excluido, 0) = 0
          AND EXISTS (
              SELECT 1
              FROM CorteCor_Servico S
              WHERE S.IdServico = CorteCor_Agendamento.IdServico
                AND S.IdSalao = @IdSalao
          )
          AND DataHora >= @InicioMes
          AND DataHora < @InicioProximoMes
    ) AS AgendamentosMes,
    (
        SELECT ISNULL(SUM(ValorTotal), 0)
        FROM CorteCor_VendaProduto
        WHERE IdSalao = @IdSalao
          AND Status <> @StatusCancelada
          AND DataVenda >= @InicioMes
          AND DataVenda < @InicioProximoMes
    ) AS ValorVendasMes,
    (
        SELECT COUNT(1)
        FROM CorteCor_VendaProduto
        WHERE IdSalao = @IdSalao
          AND Status <> @StatusCancelada
          AND DataVenda >= @InicioMes
          AND DataVenda < @InicioProximoMes
    ) AS QuantidadeVendasMes,
    (
        SELECT COUNT(1)
        FROM CorteCor_Pedido
        WHERE IdSalao = @IdSalao
          AND Status IN (@PedidoAberto, @PedidoVencido)
    ) AS PedidosAbertos,
    (
        SELECT COUNT(1)
        FROM CorteCor_Produto
        WHERE IdSalao = @IdSalao
          AND ISNULL(Excluido, 0) = 0
          AND ISNULL(Arquivado, 0) = 0
          AND ISNULL(ControlarEstoque, 0) = 1
          AND ISNULL(EstoqueAtual, 0) <= ISNULL(EstoqueMinimo, 0)
    ) AS EstoqueBaixo,
    (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status = @NotaAutorizada
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    ) AS NotasAutorizadasMes,
    (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status NOT IN (@NotaAutorizada, @NotaRejeitada, @NotaCancelada)
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    ) AS NotasPendentesMes,
    (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status = @NotaRejeitada
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    ) AS NotasRejeitadasMes;",
                new
                {
                    IdSalao = idSalao,
                    Hoje = hoje,
                    InicioMes = inicioMes,
                    InicioProximoMes = inicioProximoMes,
                    StatusCancelada = VendaProdutoStatus.Cancelada,
                    PedidoAberto = PedidoStatus.Aberto,
                    PedidoVencido = PedidoStatus.Vencido,
                    NotaAutorizada = NotaFiscalStatus.Autorizada,
                    NotaRejeitada = NotaFiscalStatus.Rejeitada,
                    NotaCancelada = NotaFiscalStatus.Cancelada
                });

            var vendasMensais = (await conn.QueryAsync<DashboardSerieSqlRow>(
                @"
SELECT
    Vendas.Mes,
    COALESCE(SUM(ValorTotal), 0) AS Valor,
    COUNT(1) AS Quantidade
FROM (
    SELECT
        STR_TO_DATE(CONCAT(DATE_FORMAT(DataVenda, '%Y-%m'), '-01'), '%Y-%m-%d') AS Mes,
        ValorTotal
    FROM CorteCor_VendaProduto
    WHERE IdSalao = @IdSalao
      AND Status <> @StatusCancelada
      AND DataVenda >= @InicioSerie
) Vendas
GROUP BY Vendas.Mes
ORDER BY Vendas.Mes;",
                new
                {
                    IdSalao = idSalao,
                    StatusCancelada = VendaProdutoStatus.Cancelada,
                    InicioSerie = inicioSerie
                })).ToList();

            agendamentosPorStatus = (await conn.QueryAsync<DashboardCategoryRow>(
                @"
SELECT
    COALESCE(NULLIF(Status, ''), 'Sem status') AS Label,
    COUNT(1) AS Value
FROM CorteCor_Agendamento
WHERE ISNULL(Excluido, 0) = 0
  AND EXISTS (
      SELECT 1
      FROM CorteCor_Servico S
      WHERE S.IdServico = CorteCor_Agendamento.IdServico
        AND S.IdSalao = @IdSalao
  )
  AND DataHora >= @InicioMes
  AND DataHora < @InicioProximoMes
GROUP BY COALESCE(NULLIF(Status, ''), 'Sem status')
ORDER BY Value DESC;",
                new
                {
                    IdSalao = idSalao,
                    InicioMes = inicioMes,
                    InicioProximoMes = inicioProximoMes
                })).ToList();

            serieCompleta = Enumerable.Range(0, 6)
                .Select(offset =>
                {
                    var mes = inicioSerie.AddMonths(offset);
                    var valor = vendasMensais.FirstOrDefault(v => v.Mes.Year == mes.Year && v.Mes.Month == mes.Month);
                    return new DashboardSerieItem
                    {
                        Label = mes.ToString("MM/yyyy"),
                        Value = valor?.Valor ?? 0m,
                        SecondaryValue = valor?.Quantidade ?? 0
                    };
                })
                .ToList();
        }
        catch (Exception ex) when (MySqlOperationalResilience.IsMaxUserConnections(ex))
        {
            avisos.Add("operacional-indisponivel");
            _logger.LogWarning(ex, "Dashboard operacional em modo degradado para o salão {IdSalao}.", idSalao);
        }

        var funil = crmDashboard.Funil
            .Select(item => new DashboardCategoryRow
            {
                Label = item.NomeEtapa,
                Value = item.Quantidade,
                SecondaryValue = item.ValorTotal
            })
            .ToList();

        var financeiroComparativo = new List<DashboardCategoryRow>
        {
            new() { Label = "Receitas", Value = financeiroDashboard.ReceitasLiquidadas },
            new() { Label = "Despesas", Value = financeiroDashboard.DespesasLiquidadas },
            new() { Label = "A receber", Value = financeiroDashboard.AReceberAberto }
        };

        var proximasAcoes = crmDashboard.ProximasTarefas
            .OrderBy(t => t.DataVencimento)
            .Take(6)
            .Select(t => new DashboardActionItem
            {
                Title = t.Titulo,
                Subtitle = string.IsNullOrWhiteSpace(t.NomePessoa) ? "Sem cliente vinculado" : t.NomePessoa!,
                Meta = $"Vence em {t.DataVencimento:dd/MM/yyyy}"
            })
            .ToList();

        return new JsonResult(new DashboardPayload
        {
            NomeEmpresa = NomeEmpresa,
            PeriodoAtual = PeriodoAtual,
            TotalClientes = aggregate.TotalClientes,
            ClientesInativos = crmDashboard.ClientesInativos,
            AniversariantesMes = crmDashboard.AniversariantesMes,
            AgendamentosHoje = aggregate.AgendamentosHoje,
            AgendamentosMes = aggregate.AgendamentosMes,
            ValorVendasMes = aggregate.ValorVendasMes,
            QuantidadeVendasMes = aggregate.QuantidadeVendasMes,
            PedidosAbertos = aggregate.PedidosAbertos,
            EstoqueBaixo = aggregate.EstoqueBaixo,
            NotasAutorizadasMes = aggregate.NotasAutorizadasMes,
            NotasPendentesMes = aggregate.NotasPendentesMes,
            NotasRejeitadasMes = aggregate.NotasRejeitadasMes,
            ReceitasLiquidadas = financeiroDashboard.ReceitasLiquidadas,
            DespesasLiquidadas = financeiroDashboard.DespesasLiquidadas,
            SaldoOperacional = financeiroDashboard.SaldoOperacional,
            AReceberAberto = financeiroDashboard.AReceberAberto,
            APagarAberto = financeiroDashboard.APagarAberto,
            ReceitasVencidas = financeiroDashboard.ReceitasVencidas,
            DespesasVencidas = financeiroDashboard.DespesasVencidas,
            QuantidadeTitulosVencidos = financeiroDashboard.QuantidadeTitulosVencidos,
            TarefasAbertas = crmDashboard.TarefasAbertas,
            VendasMensais = serieCompleta,
            AgendamentosPorStatus = agendamentosPorStatus,
            FunilCrm = funil,
            FinanceiroComparativo = financeiroComparativo,
            ProximasAcoes = proximasAcoes,
            Avisos = avisos
        });
    }

    private async Task CarregarContextoBasicoAsync()
    {
        var idSalao = ObterIdSalao();
        var email = User.Identity?.Name ?? string.Empty;

        Saudacao = ObterSaudacao(DateTime.Now.Hour);
        PeriodoAtual = $"Período de {new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1):dd/MM/yyyy} até {DateTime.Today:dd/MM/yyyy}";

        try
        {
            using var conn = _databaseHandler.GetConnection();

            NomeUsuario = await conn.ExecuteScalarAsync<string?>(
                "SELECT TOP 1 Nome FROM CorteCor_Usuario WHERE Email = @Email;",
                new { Email = email }) ?? "Usuário";

            NomeEmpresa = await conn.ExecuteScalarAsync<string?>(
                "SELECT TOP 1 Nome FROM CorteCor_Salao WHERE IdSalao = @IdSalao;",
                new { IdSalao = idSalao }) ?? "Empresa";
        }
        catch (Exception ex) when (MySqlOperationalResilience.IsMaxUserConnections(ex))
        {
            NomeUsuario = "Usuário";
            NomeEmpresa = "Empresa";
            _logger.LogWarning(ex, "Contexto básico do dashboard operando em modo degradado para o salão {IdSalao}.", idSalao);
        }
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private static string ObterSaudacao(int hora) =>
        hora switch
        {
            >= 5 and < 12 => "Bom dia",
            >= 12 and < 18 => "Boa tarde",
            _ => "Boa noite"
        };

    private static List<DashboardSerieItem> CriarSerieVendasVazia(DateTime inicioSerie) =>
        Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var mes = inicioSerie.AddMonths(offset);
                return new DashboardSerieItem
                {
                    Label = mes.ToString("MM/yyyy"),
                    Value = 0m,
                    SecondaryValue = 0
                };
            })
            .ToList();

    private sealed class DashboardAggregateRow
    {
        public int TotalClientes { get; set; }
        public int AgendamentosHoje { get; set; }
        public int AgendamentosMes { get; set; }
        public decimal ValorVendasMes { get; set; }
        public int QuantidadeVendasMes { get; set; }
        public int PedidosAbertos { get; set; }
        public int EstoqueBaixo { get; set; }
        public int NotasAutorizadasMes { get; set; }
        public int NotasPendentesMes { get; set; }
        public int NotasRejeitadasMes { get; set; }
    }

    private sealed class DashboardSerieSqlRow
    {
        public DateTime Mes { get; set; }
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
    }

    public sealed class DashboardPayload
    {
        public string NomeEmpresa { get; set; } = string.Empty;
        public string PeriodoAtual { get; set; } = string.Empty;
        public int TotalClientes { get; set; }
        public int ClientesInativos { get; set; }
        public int AniversariantesMes { get; set; }
        public int AgendamentosHoje { get; set; }
        public int AgendamentosMes { get; set; }
        public decimal ValorVendasMes { get; set; }
        public int QuantidadeVendasMes { get; set; }
        public int PedidosAbertos { get; set; }
        public int EstoqueBaixo { get; set; }
        public int NotasAutorizadasMes { get; set; }
        public int NotasPendentesMes { get; set; }
        public int NotasRejeitadasMes { get; set; }
        public decimal ReceitasLiquidadas { get; set; }
        public decimal DespesasLiquidadas { get; set; }
        public decimal SaldoOperacional { get; set; }
        public decimal AReceberAberto { get; set; }
        public decimal APagarAberto { get; set; }
        public decimal ReceitasVencidas { get; set; }
        public decimal DespesasVencidas { get; set; }
        public int QuantidadeTitulosVencidos { get; set; }
        public int TarefasAbertas { get; set; }
        public List<DashboardSerieItem> VendasMensais { get; set; } = new();
        public List<DashboardCategoryRow> AgendamentosPorStatus { get; set; } = new();
        public List<DashboardCategoryRow> FunilCrm { get; set; } = new();
        public List<DashboardCategoryRow> FinanceiroComparativo { get; set; } = new();
        public List<DashboardActionItem> ProximasAcoes { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }

    public sealed class DashboardErrorPayload
    {
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DashboardSerieItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int SecondaryValue { get; set; }
    }

    public sealed class DashboardCategoryRow
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal SecondaryValue { get; set; }
    }

    public sealed class DashboardActionItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
    }
}
