using System.Security.Claims;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

    public string ProdutosJson { get; private set; } = "[]";
    public string ServicosJson { get; private set; } = "[]";

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<IActionResult> OnPostSalvarAsync(string payloadJson, DateTime validoAte)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                throw new InvalidOperationException("O pedido não possui itens para registro.");
            }

            var input = JsonSerializer.Deserialize<PedidoCheckoutInput>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Não foi possível interpretar os dados do pedido.");

            input.ValidoAte = validoAte.Date;

            var resultado = await _pedidoService.CriarPedidoAsync(ObterIdSalao(), input, ObterUsuarioOperador());
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

    public async Task<IActionResult> OnPostConverterAsync(int idPedido, bool emitirNotaFiscalServico = false)
    {
        try
        {
            var resultado = await _pedidoService.ConverterEmVendaAsync(ObterIdSalao(), new PedidoConversaoInput
            {
                IdPedido = idPedido,
                RecebidoNaHora = true,
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

        ProdutosJson = JsonSerializer.Serialize(Contexto.Produtos.Select(p => new CatalogoDto
        {
            Id = p.IdProduto,
            Nome = p.Nome,
            Valor = p.PrecoVenda,
            Tipo = VendaProdutoTipoItem.Produto,
            Codigo = p.CodigoProprio,
            EstoqueAtual = p.EstoqueAtual ?? 0m,
            ControlaEstoque = p.ControlarEstoque
        }), JsonCamelCase);

        ServicosJson = JsonSerializer.Serialize(Contexto.Servicos.Select(s => new CatalogoDto
        {
            Id = s.IdServico,
            Nome = s.Nome,
            Valor = s.Preco,
            Tipo = VendaProdutoTipoItem.Servico,
            Codigo = s.CodigoTributacaoMunicipio
        }), JsonCamelCase);
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;

    private sealed class CatalogoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public decimal EstoqueAtual { get; set; }
        public bool ControlaEstoque { get; set; }
    }
}
