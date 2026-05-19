using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Pedidos;

[Authorize(Policy = "UsuarioPolicy")]
public class IndexModel : PageModel
{
    private readonly PedidoService _pedidoService;

    public IndexModel(PedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    public PedidoContexto Contexto { get; private set; } = new();
    public PedidoPainelResumo PainelResumo { get; private set; } = new();

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
    public bool somenteVigentes { get; set; }

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

    public async Task<IActionResult> OnPostConverterAsync(int idPedido, bool emitirNotaFiscalServico = false)
    {
        try
        {
            var resultado = await _pedidoService.ConverterEmVendaAsync(ObterIdSalao(), new PedidoConversaoInput
            {
                IdPedido = idPedido,
                RecebidoNaHora = false,
                EmitirNotaFiscalServico = emitirNotaFiscalServico
            }, ObterUsuarioOperador());

            FlashMessage = resultado.Mensagem;
            FlashType = resultado.MensagemTipo;
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, idPessoaFiltro, statusFiltro, dataInicio, dataFim, somenteVigentes, p });
    }

    public async Task<IActionResult> OnPostCancelarAsync(int idPedido)
    {
        try
        {
            await _pedidoService.CancelarPedidoAsync(ObterIdSalao(), idPedido, ObterUsuarioOperador(), $"Pedido {idPedido} cancelado pelo operador.");
            FlashMessage = "Pedido cancelado com sucesso.";
            FlashType = "warning";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { q, idPessoaFiltro, statusFiltro, dataInicio, dataFim, somenteVigentes, p });
    }

    public string ObterUrlNotas(Pedido pedido)
    {
        if (!pedido.IdVendaProduto.HasValue)
        {
            return Url.Page("/NotaFiscalLista") ?? "/NotaFiscalLista";
        }

        return Url.Page("/NotaFiscalLista", new { idVendaProduto = pedido.IdVendaProduto.Value })
               ?? $"/NotaFiscalLista?idVendaProduto={pedido.IdVendaProduto.Value}";
    }

    private async Task CarregarAsync()
    {
        Contexto = await _pedidoService.ObterContextoAsync(ObterIdSalao(), new PedidoFiltro
        {
            Pesquisa = q,
            IdPessoa = idPessoaFiltro,
            Status = statusFiltro,
            DataInicio = dataInicio,
            DataFim = dataFim,
            SomenteVigentes = somenteVigentes,
            PageIndex = p > 0 ? p : 1,
            PageSize = 10
        });

        PainelResumo = new PedidoPainelResumo
        {
            PedidosListados = Contexto.PedidosRecentes.Items.Count,
            PedidosVigentes = Contexto.PedidosRecentes.Items.Count(i => i.Status == PedidoStatus.Aberto),
            PedidosVencendoHoje = Contexto.PedidosRecentes.Items.Count(i => i.Status == PedidoStatus.Aberto && i.ValidoAte.Date == DateTime.Today),
            PedidosConvertidos = Contexto.PedidosRecentes.Items.Count(i => i.Status == PedidoStatus.Convertido),
            ValorListagem = Contexto.PedidosRecentes.Items.Sum(i => i.ValorTotal)
        };
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;
}
