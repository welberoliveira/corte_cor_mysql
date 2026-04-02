using System.Security.Claims;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Estoque;

[Authorize(Policy = "UsuarioPolicy")]
public class AjusteManualModel : PageModel
{
    private readonly VendaService _vendaService;
    private readonly ProdutoHandler _produtoHandler;

    public AjusteManualModel(VendaService vendaService, ProdutoHandler produtoHandler)
    {
        _vendaService = vendaService;
        _produtoHandler = produtoHandler;
    }

    public List<Produto> ProdutosAjuste { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? idProduto { get; set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public void OnGet()
    {
        CarregarProdutos();
    }

    public async Task<IActionResult> OnPostAsync(int idProduto, decimal quantidade, string tipoMovimento, string? observacao)
    {
        try
        {
            await _vendaService.RegistrarAjusteEstoqueAsync(ObterIdSalao(), idProduto, quantidade, tipoMovimento, observacao, ObterUsuarioOperador());
            FlashMessage = "Ajuste de estoque registrado com sucesso.";
            FlashType = "success";
            return RedirectToPage(new { idProduto });
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            this.idProduto = idProduto;
            CarregarProdutos();
            return Page();
        }
    }

    private void CarregarProdutos()
    {
        ProdutosAjuste = _produtoHandler.ListarPorSalao(ObterIdSalao())
            ?.Where(p => p.ControlarEstoque && !p.Arquivado)
            .OrderBy(p => p.Nome)
            .ToList() ?? new List<Produto>();
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;
}
