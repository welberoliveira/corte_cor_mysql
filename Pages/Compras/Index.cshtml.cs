using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Compras;

[Authorize(Policy = "UsuarioPolicy")]
public class IndexModel : PageModel
{
    private readonly CompraService _compraService;

    public IndexModel(CompraService compraService)
    {
        _compraService = compraService;
    }

    public PagedResult<Compra> Compras { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string? q { get; set; }
    [BindProperty(SupportsGet = true)] public string? status { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? dataInicio { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? dataFim { get; set; }
    [BindProperty(SupportsGet = true)] public int p { get; set; } = 1;

    [TempData] public string? FlashMessage { get; set; }
    [TempData] public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        Compras = await _compraService.ListarComprasAsync(ObterIdSalao(), new CompraFiltro
        {
            Pesquisa = q,
            Status = status,
            DataInicio = dataInicio,
            DataFim = dataFim,
            PageIndex = p > 0 ? p : 1,
            PageSize = 15
        });
    }

    public async Task<IActionResult> OnPostCancelarAsync(int idCompra, string? justificativa)
    {
        try
        {
            var resultado = await _compraService.CancelarCompraAsync(ObterIdSalao(), idCompra, ObterUsuarioOperador(), justificativa);
            FlashMessage = resultado.EstoqueAjustado
                ? $"Compra #{resultado.IdCompra} cancelada. Conta a pagar cancelada e estoque ajustado automaticamente ({resultado.QuantidadeMovimentosEstoque} movimento(s))."
                : $"Compra #{resultado.IdCompra} cancelada. Conta a pagar cancelada.";
            FlashType = "warning";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, status, dataInicio, dataFim, p });
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;
}
