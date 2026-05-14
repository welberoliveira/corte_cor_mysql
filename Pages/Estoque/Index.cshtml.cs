using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Estoque;

[Authorize(Policy = "UsuarioPolicy")]
public class IndexModel : PageModel
{
    private readonly VendaService _vendaService;

    public IndexModel(VendaService vendaService)
    {
        _vendaService = vendaService;
    }

    public EstoqueResumo Resumo { get; private set; } = new();
    public PagedResult<ProdutoEstoquePosicao> PosicaoEstoque { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool somenteBaixo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int posPage { get; set; } = 1;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        var idSalao = ObterIdSalao();
        Resumo = await _vendaService.ObterResumoEstoqueAsync(idSalao);
        PosicaoEstoque = await _vendaService.ListarPosicaoEstoqueAsync(idSalao, q, somenteBaixo, posPage, 12);
    }

    public string ObterUrlMovimentacoes(int idProduto) =>
        Url.Page("/Estoque/Movimentacoes", new { idProduto }) ?? $"/Estoque/Movimentacoes?idProduto={idProduto}";

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
}
