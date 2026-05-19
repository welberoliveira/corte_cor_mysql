using System.Globalization;
using System.Text;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro;

[Authorize(Policy = "UsuarioPolicy")]
public class ExportacaoContabilModel : PageModel
{
    private static readonly CultureInfo CulturaBrasil = new("pt-BR");
    private readonly FinanceiroService _financeiroService;

    public ExportacaoContabilModel(FinanceiroService financeiroService)
    {
        _financeiroService = financeiroService;
    }

    public PagedResult<FinanceiroTitulo> Titulos { get; private set; } = new();
    public List<PlanoContas> GruposPlano { get; private set; } = new();
    public List<PlanoContas> ContasPlano { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? DataInicio { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DataFim { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdGrupoPlano { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdPlano { get; set; }

    [BindProperty(SupportsGet = true)]
    public int p { get; set; } = 1;

    public async Task OnGetAsync()
    {
        await CarregarAsync(100);
    }

    public async Task<IActionResult> OnGetExportarCsvAsync()
    {
        await CarregarAsync(10000);
        var csv = MontarCsv(Titulos.Items);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var nomeArquivo = $"exportacao-contabil-{DateTime.Today:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", nomeArquivo);
    }

    public async Task<JsonResult> OnGetContasFilhasAsync(int idGrupo, string? tipo)
    {
        var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupo, tipo ?? Tipo);
        return new JsonResult(contas.Select(conta => new
        {
            id = conta.IdPlano,
            rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
        }));
    }

    private async Task CarregarAsync(int pageSize)
    {
        var hoje = DateTime.Today;
        DataInicio ??= new DateTime(hoje.Year, hoje.Month, 1);
        DataFim ??= hoje;

        var idSalao = ObterIdSalao();
        GruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(idSalao, Tipo);
        if (!IdGrupoPlano.HasValue && IdPlano.HasValue)
        {
            IdGrupoPlano = (await _financeiroService.ObterGrupoNivel2DoPlanoAsync(idSalao, IdPlano))?.IdPlano;
        }

        ContasPlano = IdGrupoPlano.HasValue && IdGrupoPlano > 0
            ? await _financeiroService.ListarContasAnaliticasPorGrupoAsync(idSalao, IdGrupoPlano.Value, Tipo)
            : new List<PlanoContas>();

        Titulos = await _financeiroService.ListarTitulosAsync(idSalao, new FinanceiroTituloFiltro
        {
            Tipo = Tipo,
            Status = Status,
            IdGrupoPlano = IdGrupoPlano,
            IdPlano = IdPlano,
            DataInicio = DataInicio,
            DataFim = DataFim,
            PageIndex = Math.Max(1, p),
            PageSize = pageSize
        });
    }

    public string FormatarMoeda(decimal valor) =>
        valor.ToString("C", CulturaBrasil);

    private static string MontarCsv(IEnumerable<FinanceiroTitulo> titulos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DataCompetencia;DataVencimento;DataLiquidacao;Tipo;Status;Origem;PlanoContas;Pessoa;Documento;Descricao;ValorOriginal;ValorLiquidado;ValorAberto;IdVendaProduto;IdPagamento;IdAgendamento;ContaCaixa;Conciliado");
        foreach (var titulo in titulos)
        {
            var colunas = new[]
            {
                titulo.DataCompetencia.ToString("yyyy-MM-dd"),
                titulo.DataVencimento.ToString("yyyy-MM-dd"),
                titulo.DataLiquidacao?.ToString("yyyy-MM-dd") ?? string.Empty,
                titulo.Tipo,
                titulo.Status,
                titulo.Origem,
                titulo.NomePlano ?? string.Empty,
                titulo.NomePessoa ?? string.Empty,
                titulo.Documento ?? string.Empty,
                titulo.Descricao,
                titulo.ValorOriginal.ToString("0.00", CultureInfo.InvariantCulture),
                titulo.ValorLiquidado.ToString("0.00", CultureInfo.InvariantCulture),
                titulo.ValorAberto.ToString("0.00", CultureInfo.InvariantCulture),
                titulo.IdVendaProduto?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                titulo.IdPagamento?.ToString() ?? string.Empty,
                titulo.IdAgendamento?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                titulo.NomeConta ?? string.Empty,
                titulo.Conciliado ? "Sim" : "Nao"
            };
            sb.AppendLine(string.Join(';', colunas.Select(EscaparCsv)));
        }

        return sb.ToString();
    }

    private static string EscaparCsv(string valor)
    {
        var texto = valor.Replace("\"", "\"\"");
        return texto.Contains(';') || texto.Contains('\n') || texto.Contains('\r') || texto.Contains('"')
            ? $"\"{texto}\""
            : texto;
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
}
