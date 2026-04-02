using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Estoque;

[Authorize(Policy = "UsuarioPolicy")]
public class MovimentacoesModel : PageModel
{
    private readonly VendaService _vendaService;
    private readonly ProdutoHandler _produtoHandler;

    public MovimentacoesModel(VendaService vendaService, ProdutoHandler produtoHandler)
    {
        _vendaService = vendaService;
        _produtoHandler = produtoHandler;
    }

    public PagedResult<MovimentoEstoque> Movimentos { get; private set; } = new();
    public List<Produto> ProdutosFiltro { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? movQ { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? idProduto { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? movTipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? movDataInicio { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? movDataFim { get; set; }

    [BindProperty(SupportsGet = true)]
    public int movPage { get; set; } = 1;

    public Produto? ProdutoSelecionado { get; private set; }

    public async Task OnGetAsync()
    {
        var idSalao = ObterIdSalao();
        ProdutosFiltro = _produtoHandler.ListarPorSalao(idSalao)
            ?.Where(p => p.ControlarEstoque && !p.Arquivado)
            .OrderBy(p => p.Nome)
            .ToList() ?? new List<Produto>();

        ProdutoSelecionado = idProduto.HasValue && idProduto.Value > 0
            ? ProdutosFiltro.FirstOrDefault(p => p.IdProduto == idProduto.Value)
            : null;

        Movimentos = await _vendaService.ListarMovimentosAsync(idSalao, new EstoqueMovimentoFiltro
        {
            Pesquisa = movQ,
            IdProduto = idProduto,
            TipoMovimento = movTipo,
            DataInicio = movDataInicio,
            DataFim = movDataFim,
            PageIndex = movPage,
            PageSize = 12
        });
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
}
