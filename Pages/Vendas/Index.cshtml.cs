using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Vendas;

[Authorize(Policy = "UsuarioPolicy")]
public class IndexModel : PageModel
{
    private readonly VendaService _vendaService;

    public IndexModel(VendaService vendaService)
    {
        _vendaService = vendaService;
    }

    public VendaCheckoutContexto Contexto { get; private set; } = new();
    public VendaPainelResumo PainelResumo { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? idPessoaFiltro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? statusFiltro { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? dataInicio { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? dataFim { get; set; }

    [BindProperty(SupportsGet = true)]
    public int p { get; set; } = 1;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostEmitirNotaAsync(int idVendaProduto)
    {
        try
        {
            var resultado = await _vendaService.EmitirNotaServicoAsync(ObterIdSalao(), idVendaProduto, ObterUsuarioOperador());
            FlashMessage = resultado.Mensagem;
            FlashType = resultado.MensagemTipo;
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, idPessoaFiltro, statusFiltro, dataInicio, dataFim, p });
    }

    public async Task<IActionResult> OnPostCancelarVendaAsync(int idVendaProduto)
    {
        try
        {
            await _vendaService.CancelarVendaAsync(ObterIdSalao(), idVendaProduto, ObterUsuarioOperador(), $"Venda {idVendaProduto} cancelada pelo operador.");
            FlashMessage = "Venda cancelada com sucesso.";
            FlashType = "warning";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, idPessoaFiltro, statusFiltro, dataInicio, dataFim, p });
    }

    public string ObterUrlNotas(int idVendaProduto)
    {
        return Url.Page("/NotaFiscalLista", new { idVendaProduto }) ?? $"/NotaFiscalLista?idVendaProduto={idVendaProduto}";
    }

    public string ObterUrlDetalhes(int idVendaProduto)
    {
        return Url.Page("/Vendas/Detalhes", new { idVendaProduto }) ?? $"/Vendas/Detalhes?idVendaProduto={idVendaProduto}";
    }

    public string ObterUrlCadastroOuVisualizacao(int idVendaProduto)
    {
        return Url.Page("/Vendas/Novo", new { idVendaProduto }) ?? $"/Vendas/Novo?idVendaProduto={idVendaProduto}";
    }

    public bool PermiteAlteracao(string? status) => VendaService.PermiteAlteracao(status);

    public string ObterRotuloAcaoPrincipal(string? status) => PermiteAlteracao(status) ? "Alterar" : "Visualizar";

    private async Task CarregarAsync()
    {
        Contexto = await _vendaService.ObterContextoAsync(ObterIdSalao(), new VendaProdutoFiltro
        {
            Pesquisa = q,
            IdPessoa = idPessoaFiltro,
            Status = statusFiltro,
            DataInicio = dataInicio,
            DataFim = dataFim,
            PageIndex = p > 0 ? p : 1,
            PageSize = 10
        });

        PainelResumo = new VendaPainelResumo
        {
            VendasListadas = Contexto.VendasRecentes.Items.Count,
            TotalListagem = Contexto.VendasRecentes.Items.Sum(v => v.ValorTotal),
            ComNotaAutorizada = Contexto.VendasRecentes.Items.Count(v => string.Equals(v.StatusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase)),
            PendentesFiscais = Contexto.VendasRecentes.Items.Count(v =>
                v.SubtotalServicos > 0 &&
                v.Status != VendaProdutoStatus.Cancelada &&
                !string.Equals(v.StatusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase))
        };
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;

    public sealed class VendaPainelResumo
    {
        public int VendasListadas { get; set; }
        public decimal TotalListagem { get; set; }
        public int ComNotaAutorizada { get; set; }
        public int PendentesFiscais { get; set; }
    }
}
