using System.Security.Claims;
using CorteCor.Handlers;
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
    private readonly ProdutoHandler _produtoHandler;

    public IndexModel(VendaService vendaService, ProdutoHandler produtoHandler)
    {
        _vendaService = vendaService;
        _produtoHandler = produtoHandler;
    }

    public EstoqueResumo Resumo { get; private set; } = new();
    public PagedResult<ProdutoEstoquePosicao> PosicaoEstoque { get; private set; } = new();
    public PagedResult<MovimentoEstoque> Movimentos { get; private set; } = new();
    public List<Produto> ProdutosAjuste { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? movQ { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? movTipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool somenteBaixo { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? movDataInicio { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? movDataFim { get; set; }

    [BindProperty(SupportsGet = true)]
    public int posPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int movPage { get; set; } = 1;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostAjustarAsync(int idProduto, decimal quantidade, string tipoMovimento, string? observacao)
    {
        try
        {
                    await _vendaService.RegistrarAjusteEstoqueAsync(ObterIdSalao(), idProduto, quantidade, tipoMovimento, observacao, ObterUsuarioOperador());
            FlashMessage = "Ajuste de estoque registrado com sucesso.";
            FlashType = "success";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, movQ, movTipo, movDataInicio, movDataFim, somenteBaixo, posPage, movPage });
    }

    private async Task CarregarAsync()
    {
        var idSalao = ObterIdSalao();
        Resumo = await _vendaService.ObterResumoEstoqueAsync(idSalao);
        PosicaoEstoque = await _vendaService.ListarPosicaoEstoqueAsync(idSalao, q, somenteBaixo, posPage, 12);
        Movimentos = await _vendaService.ListarMovimentosAsync(idSalao, new EstoqueMovimentoFiltro
        {
            Pesquisa = movQ,
            TipoMovimento = movTipo,
            DataInicio = movDataInicio,
            DataFim = movDataFim,
            PageIndex = movPage,
            PageSize = 12
        });

        ProdutosAjuste = _produtoHandler.ListarPorSalao(idSalao)
            ?.Where(p => p.ControlarEstoque && !p.Arquivado)
            .OrderBy(p => p.Nome)
            .ToList() ?? new List<Produto>();
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;
}
