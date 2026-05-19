using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace CorteCor.Pages.Financeiro;

[Authorize(Policy = "UsuarioPolicy")]
public class FluxoCaixaModel : PageModel
{
    private static readonly CultureInfo CulturaBrasil = new("pt-BR");
    private readonly FinanceiroService _financeiroService;

    public FluxoCaixaModel(FinanceiroService financeiroService)
    {
        _financeiroService = financeiroService;
    }

    public FinanceiroFluxoCaixaResumo Fluxo { get; private set; } = new();
    public List<int> AnosDisponiveis { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Visao { get; set; } = "Mes";

    [BindProperty(SupportsGet = true)]
    public DateTime? Dia { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Mes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Ano { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Projetado { get; set; } = true;

    public async Task OnGetAsync()
    {
        var hoje = DateTime.Today;
        Visao = string.Equals(Visao, "Dia", StringComparison.OrdinalIgnoreCase)
            ? "Dia"
            : string.Equals(Visao, "Ano", StringComparison.OrdinalIgnoreCase)
                ? "Ano"
                : "Mes";
        Ano = Ano > 0 ? Ano : hoje.Year;
        Mes = Mes is >= 1 and <= 12 ? Mes : hoje.Month;
        Dia ??= hoje;
        AnosDisponiveis = Enumerable.Range(hoje.Year - 5, 7).Reverse().ToList();
        if (!AnosDisponiveis.Contains(Ano))
        {
            AnosDisponiveis.Insert(0, Ano);
            AnosDisponiveis = AnosDisponiveis.Distinct().OrderByDescending(ano => ano).ToList();
        }

        var (inicio, fim) = ObterPeriodo();
        Fluxo = await _financeiroService.ObterFluxoCaixaAsync(ObterIdSalao(), inicio, fim, Visao, Projetado);
    }

    public string RotuloData(DateTime data)
    {
        return Visao == "Ano"
            ? $"{CulturaBrasil.DateTimeFormat.GetMonthName(data.Month)} / {data.Year}"
            : data.ToString("dd/MM/yyyy", CulturaBrasil);
    }

    public string FormatarMoeda(decimal valor)
    {
        var texto = Math.Abs(valor).ToString("N2", CulturaBrasil);
        return valor < 0 ? $"(R$ {texto})" : $"R$ {texto}";
    }

    public string ClasseValor(decimal valor) => valor < 0 ? "text-danger" : valor > 0 ? "text-success" : "text-muted";

    private (DateTime Inicio, DateTime Fim) ObterPeriodo()
    {
        if (Visao == "Dia")
        {
            var dia = Dia?.Date ?? DateTime.Today;
            return (dia, dia);
        }

        if (Visao == "Ano")
        {
            return (new DateTime(Ano, 1, 1), new DateTime(Ano, 12, 31));
        }

        var inicioMes = new DateTime(Ano, Mes, 1);
        return (inicioMes, inicioMes.AddMonths(1).AddDays(-1));
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
}
