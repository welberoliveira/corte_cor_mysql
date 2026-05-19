using System.Security.Claims;
using System.Text.Json;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Compras;

[Authorize(Policy = "UsuarioPolicy")]
public class NovaModel : PageModel
{
    private readonly CompraService _compraService;
    private readonly ProdutoHandler _produtoHandler;
    private readonly FinanceiroService _financeiroService;
    private static readonly JsonSerializerOptions JsonCamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public NovaModel(CompraService compraService, ProdutoHandler produtoHandler, FinanceiroService financeiroService)
    {
        _compraService = compraService;
        _produtoHandler = produtoHandler;
        _financeiroService = financeiroService;
    }

    public string ProdutosJson { get; private set; } = "[]";
    public List<PlanoContas> GruposPlano { get; private set; } = new();
    public List<PlanoContas> ContasPlano { get; private set; } = new();
    public List<ContaCaixa> ContasCaixa { get; private set; } = new();
    public int? GrupoPlanoContaId { get; private set; }

    [TempData] public string? FlashMessage { get; set; }
    [TempData] public string? FlashType { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarAsync();
    }

    public async Task<JsonResult> OnGetContasFilhasAsync(int idGrupo)
    {
        var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupo, FinanceiroTipoTitulo.Pagar);
        return new JsonResult(contas.Select(conta => new
        {
            id = conta.IdPlano,
            rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
        }));
    }

    public async Task<IActionResult> OnPostSalvarAsync(string payloadJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                throw new InvalidOperationException("A compra nao possui itens para registro.");
            }

            var input = JsonSerializer.Deserialize<CompraInput>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Nao foi possivel interpretar os dados da compra.");

            var idCompra = await _compraService.RegistrarCompraAsync(ObterIdSalao(), input, ObterUsuarioOperador());
            FlashMessage = $"Compra #{idCompra} registrada com sucesso.";
            FlashType = "success";
            return RedirectToPage("/Compras/Index");
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            await CarregarAsync();
            return Page();
        }
    }

    private async Task CarregarAsync()
    {
        var produtos = _produtoHandler.ListarPorSalao(ObterIdSalao())
            ?.Where(p => !p.Arquivado && !p.Excluido && p.ControlarEstoque)
            .OrderBy(p => p.Nome)
            .Select(p => new
            {
                id = p.IdProduto,
                nome = p.Nome,
                valor = p.PrecoCusto ?? 0m,
                estoqueAtual = p.EstoqueAtual ?? 0m
            }) ?? Enumerable.Empty<object>();

        ProdutosJson = JsonSerializer.Serialize(produtos, JsonCamelCase);
        GruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(ObterIdSalao(), FinanceiroTipoTitulo.Pagar);
        ContasCaixa = (await _financeiroService.ListarContasCaixaAsync(ObterIdSalao())).Where(c => c.Ativo).OrderBy(c => c.Nome).ToList();
        GrupoPlanoContaId = GruposPlano.FirstOrDefault(g => (g.Codigo ?? string.Empty).StartsWith("1.1.03", StringComparison.OrdinalIgnoreCase)
                                                       || (g.Codigo ?? string.Empty).StartsWith("6.", StringComparison.OrdinalIgnoreCase))?.IdPlano
                            ?? GruposPlano.FirstOrDefault()?.IdPlano;
        ContasPlano = GrupoPlanoContaId.HasValue
            ? await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), GrupoPlanoContaId.Value, FinanceiroTipoTitulo.Pagar)
            : new List<PlanoContas>();
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private string? ObterUsuarioOperador() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;
}
