using System.Security.Claims;
using System.Text.Json;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Vendas;

[Authorize(Policy = "UsuarioPolicy")]
public class DetalhesModel : PageModel
{
    private readonly VendaService _vendaService;

    public DetalhesModel(VendaService vendaService)
    {
        _vendaService = vendaService;
    }

    [BindProperty(SupportsGet = true)]
    public int idVendaProduto { get; set; }

    public VendaDetalheContexto Contexto { get; private set; } = new();
    public string ProdutosJson { get; private set; } = "[]";
    public string ServicosJson { get; private set; } = "[]";

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    private static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IActionResult> OnGetAsync()
    {
        if (idVendaProduto <= 0)
        {
            return RedirectToPage("/Vendas/Index");
        }

        try
        {
            await CarregarAsync();
            return Page();
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            return RedirectToPage("/Vendas/Index");
        }
    }

    public async Task<IActionResult> OnPostProcessarAsync(int idVendaProduto, string payloadJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                throw new InvalidOperationException("Não foi possível interpretar os dados do pós-venda.");
            }

            var input = JsonSerializer.Deserialize<VendaPosVendaInput>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Não foi possível interpretar os dados do pós-venda.");

            var resultado = await _vendaService.ProcessarPosVendaAsync(ObterIdSalao(), idVendaProduto, input, ObterUsuarioOperador());
            FlashMessage = string.IsNullOrWhiteSpace(resultado.ResumoFinanceiro)
                ? resultado.Mensagem
                : $"{resultado.Mensagem} {resultado.ResumoFinanceiro}";
            FlashType = resultado.MensagemTipo;
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { idVendaProduto });
    }

    public async Task<IActionResult> OnPostCancelarTotalAsync(int idVendaProduto, string? observacoes)
    {
        try
        {
            var resultado = await _vendaService.EstornarVendaAsync(ObterIdSalao(), idVendaProduto, ObterUsuarioOperador(), observacoes);

            FlashMessage = string.IsNullOrWhiteSpace(resultado.ResumoFinanceiro)
                ? resultado.Mensagem
                : $"{resultado.Mensagem} {resultado.ResumoFinanceiro}";
            FlashType = resultado.MensagemTipo;
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { idVendaProduto });
    }

    private async Task CarregarAsync()
    {
        Contexto = await _vendaService.ObterDetalheAsync(ObterIdSalao(), idVendaProduto);

        ProdutosJson = JsonSerializer.Serialize(Contexto.Produtos.Select(p => new CatalogoDto
        {
            Id = p.IdProduto,
            Nome = p.Nome,
            Valor = p.PrecoVenda,
            Tipo = VendaProdutoTipoItem.Produto,
            EstoqueAtual = p.EstoqueAtual ?? 0m,
            ControlaEstoque = p.ControlarEstoque
        }), JsonCamelCase);

        ServicosJson = JsonSerializer.Serialize(Contexto.Servicos.Select(s => new CatalogoDto
        {
            Id = s.IdServico,
            Nome = s.Nome,
            Valor = s.Preco,
            Tipo = VendaProdutoTipoItem.Servico
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
        public decimal EstoqueAtual { get; set; }
        public bool ControlaEstoque { get; set; }
    }
}
