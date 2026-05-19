using System.Security.Claims;
using System.Text.Json;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Vendas;

[Authorize(Policy = "UsuarioPolicy")]
public class NovoModel : PageModel
{
    private readonly VendaService _vendaService;
    private readonly FinanceiroService _financeiroService;

    public NovoModel(VendaService vendaService, FinanceiroService financeiroService)
    {
        _vendaService = vendaService;
        _financeiroService = financeiroService;
    }

    [BindProperty(SupportsGet = true)]
    public int? idVendaProduto { get; set; }

    public VendaCheckoutContexto Contexto { get; private set; } = new();
    public VendaProduto? VendaExistente { get; private set; }
    public List<PlanoContas> GruposPlano { get; private set; } = new();
    public List<PlanoContas> ContasPlano { get; private set; } = new();
    public List<ContaCaixa> ContasCaixa { get; private set; } = new();
    public int? GrupoPlanoContaId { get; private set; }
    public int? IdPlanoPadraoVenda { get; private set; }
    public string ProdutosJson { get; private set; } = "[]";
    public string ServicosJson { get; private set; } = "[]";
    public string ItensJson { get; private set; } = "[]";

    public bool ModoSomenteLeitura { get; private set; }

    public string TituloTela =>
        idVendaProduto.GetValueOrDefault() > 0
            ? (ModoSomenteLeitura ? "Visualizar Venda" : "Alterar Venda")
            : "Nova Venda";

    public string DescricaoTela =>
        idVendaProduto.GetValueOrDefault() > 0
            ? (ModoSomenteLeitura
                ? "Consulte os dados da venda selecionada sem alterar o conteúdo."
                : "Atualize os dados da venda enquanto ela ainda não estiver finalizada.")
            : "Monte a venda com produtos e serviços cadastrados e conclua a operação em uma tela dedicada.";

    private static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    public string? MensagemTela { get; private set; }
    public string MensagemTelaTipo { get; private set; } = "info";

    public async Task<IActionResult> OnGetAsync()
    {
        await CarregarAsync();
        return Page();
    }

    public async Task<JsonResult> OnGetContasFilhasAsync(int idGrupo)
    {
        var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupo, FinanceiroTipoTitulo.Receber);
        return new JsonResult(contas.Select(conta => new
        {
            id = conta.IdPlano,
            codigo = conta.Codigo,
            nome = conta.NomeExibicao,
            rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
        }));
    }

    public async Task<IActionResult> OnPostFinalizarAsync(int? idVendaProduto, string payloadJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                throw new InvalidOperationException("A venda nao possui itens para finalizacao.");
            }

            var input = JsonSerializer.Deserialize<VendaCheckoutInput>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Nao foi possivel interpretar os dados da venda.");

            VendaOperacaoResult resultado;
            var vendaId = idVendaProduto.GetValueOrDefault();
            if (vendaId > 0)
            {
                resultado = await _vendaService.AtualizarVendaEmAbertoAsync(ObterIdSalao(), vendaId, input, ObterUsuarioOperador());
            }
            else
            {
                resultado = await _vendaService.FinalizarVendaAsync(ObterIdSalao(), input, ObterUsuarioOperador());
            }

            FlashMessage = resultado.Mensagem;
            FlashType = resultado.MensagemTipo;
            return RedirectToPage("/Vendas/Index");
        }
        catch (Exception ex)
        {
            this.idVendaProduto = idVendaProduto;
            MensagemTela = ex.Message;
            MensagemTelaTipo = "danger";
            await CarregarAsync();
            return Page();
        }
    }

    private async Task CarregarAsync()
    {
        Contexto = await _vendaService.ObterContextoAsync(ObterIdSalao(), new VendaProdutoFiltro
        {
            PageIndex = 1,
            PageSize = 1
        }, incluirVendasRecentes: false);

        GruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(ObterIdSalao(), FinanceiroTipoTitulo.Receber);
        ContasCaixa = (await _financeiroService.ListarContasCaixaAsync(ObterIdSalao()))
            .Where(conta => conta.Ativo)
            .OrderBy(conta => conta.Nome)
            .ToList();

        var planoPadrao = await _financeiroService.ObterPlanoReceitaPadraoVendaAsync(ObterIdSalao());
        IdPlanoPadraoVenda = planoPadrao?.IdPlano;
        GrupoPlanoContaId = (await _financeiroService.ObterGrupoNivel2DoPlanoAsync(ObterIdSalao(), IdPlanoPadraoVenda))?.IdPlano;
        ContasPlano = GrupoPlanoContaId.HasValue
            ? await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), GrupoPlanoContaId.Value, FinanceiroTipoTitulo.Receber)
            : new List<PlanoContas>();

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

        if (idVendaProduto.GetValueOrDefault() <= 0)
        {
            VendaExistente = null;
            ModoSomenteLeitura = false;
            ItensJson = "[]";
            return;
        }

        var vendaId = idVendaProduto.GetValueOrDefault();
        var detalhe = await _vendaService.ObterDetalheAsync(ObterIdSalao(), vendaId);
        VendaExistente = detalhe.Venda ?? throw new InvalidOperationException("Venda nao encontrada.");
        ModoSomenteLeitura = !VendaService.PermiteAlteracao(VendaExistente.Status);

        ItensJson = JsonSerializer.Serialize(detalhe.Itens.Select(item => new ItemVendaDto
        {
            TipoItem = item.TipoItem,
            IdProduto = item.IdProduto,
            IdServico = item.IdServico,
            Descricao = item.Descricao,
            Quantidade = item.Quantidade,
            ValorUnitario = item.ValorUnitario
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

    private sealed class ItemVendaDto
    {
        public string TipoItem { get; set; } = VendaProdutoTipoItem.Produto;
        public int? IdProduto { get; set; }
        public int? IdServico { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
    }
}
