using System.Security.Claims;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages;

[Authorize(Policy = "UsuarioPolicy")]
public class DashboardModel : PageModel
{
    private readonly IDatabaseHandler _databaseHandler;
    private readonly CrmService _crmService;
    private readonly FinanceiroService _financeiroService;

    public DashboardModel(
        IDatabaseHandler databaseHandler,
        CrmService crmService,
        FinanceiroService financeiroService)
    {
        _databaseHandler = databaseHandler;
        _crmService = crmService;
        _financeiroService = financeiroService;
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

        await CarregarContextoBasicoAsync();

        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);
        var inicioSerie = inicioMes.AddMonths(-5);

        var crmDashboard = _crmService.ObterDashboard(idSalao);
        var financeiroDashboard = await _financeiroService.ObterDashboardAsync(idSalao, inicioMes, hoje);

        using var conn = _databaseHandler.GetConnection();

        var aggregate = await conn.QueryFirstAsync<DashboardAggregateRow>(
            @"
SELECT
    TotalClientes = (
        SELECT COUNT(1)
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND ISNULL(IsCliente, 0) = 1
          AND ISNULL(Excluido, 0) = 0
    ),
    AgendamentosHoje = (
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
    ),
    AgendamentosMes = (
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
    ),
    ValorVendasMes = (
        SELECT ISNULL(SUM(ValorTotal), 0)
        FROM CorteCor_VendaProduto
        WHERE IdSalao = @IdSalao
          AND Status <> @StatusCancelada
          AND DataVenda >= @InicioMes
          AND DataVenda < @InicioProximoMes
    ),
    QuantidadeVendasMes = (
        SELECT COUNT(1)
        FROM CorteCor_VendaProduto
        WHERE IdSalao = @IdSalao
          AND Status <> @StatusCancelada
          AND DataVenda >= @InicioMes
          AND DataVenda < @InicioProximoMes
    ),
    PedidosAbertos = (
        SELECT COUNT(1)
        FROM CorteCor_Pedido
        WHERE IdSalao = @IdSalao
          AND Status IN (@PedidoAberto, @PedidoVencido)
    ),
    EstoqueBaixo = (
        SELECT COUNT(1)
        FROM CorteCor_Produto
        WHERE IdSalao = @IdSalao
          AND ISNULL(Excluido, 0) = 0
          AND ISNULL(Arquivado, 0) = 0
          AND ISNULL(ControlarEstoque, 0) = 1
          AND ISNULL(EstoqueAtual, 0) <= ISNULL(EstoqueMinimo, 0)
    ),
    NotasAutorizadasMes = (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status = @NotaAutorizada
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    ),
    NotasPendentesMes = (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status NOT IN (@NotaAutorizada, @NotaRejeitada, @NotaCancelada)
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    ),
    NotasRejeitadasMes = (
        SELECT COUNT(1)
        FROM CorteCor_NotaFiscal
        WHERE IdSalao = @IdSalao
          AND Status = @NotaRejeitada
          AND DataEmissao >= @InicioMes
          AND DataEmissao < @InicioProximoMes
    );",
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
    Mes = DATEFROMPARTS(YEAR(DataVenda), MONTH(DataVenda), 1),
    Valor = SUM(ValorTotal),
    Quantidade = COUNT(1)
FROM CorteCor_VendaProduto
WHERE IdSalao = @IdSalao
  AND Status <> @StatusCancelada
  AND DataVenda >= @InicioSerie
GROUP BY YEAR(DataVenda), MONTH(DataVenda)
ORDER BY Mes;",
            new
            {
                IdSalao = idSalao,
                StatusCancelada = VendaProdutoStatus.Cancelada,
                InicioSerie = inicioSerie
            })).ToList();

        var agendamentosPorStatus = (await conn.QueryAsync<DashboardCategoryRow>(
            @"
SELECT
    Label = ISNULL(NULLIF(Status, ''), 'Sem status'),
    Value = COUNT(1)
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
GROUP BY ISNULL(NULLIF(Status, ''), 'Sem status')
ORDER BY Value DESC;",
            new
            {
                IdSalao = idSalao,
                InicioMes = inicioMes,
                InicioProximoMes = inicioProximoMes
            })).ToList();

        var serieCompleta = Enumerable.Range(0, 6)
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
            ProximasAcoes = proximasAcoes
        });
    }

    private async Task CarregarContextoBasicoAsync()
    {
        using var conn = _databaseHandler.GetConnection();
        var idSalao = ObterIdSalao();
        var email = User.Identity?.Name ?? string.Empty;

        NomeUsuario = await conn.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 Nome FROM CorteCor_Usuario WHERE Email = @Email;",
            new { Email = email }) ?? "Usuário";

        NomeEmpresa = await conn.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 Nome FROM CorteCor_Salao WHERE IdSalao = @IdSalao;",
            new { IdSalao = idSalao }) ?? "Empresa";

        Saudacao = ObterSaudacao(DateTime.Now.Hour);
        PeriodoAtual = $"Período de {new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1):dd/MM/yyyy} até {DateTime.Today:dd/MM/yyyy}";
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
