using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PlanoContaCadastroModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public PlanoContaCadastroModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        [BindProperty]
        public PlanoContas PlanoInput { get; set; } = new()
        {
            Ativo = true,
            Tipo = "D",
            NaturezaSaldo = "Devedora",
            AceitaLancamento = true
        };

        public List<PlanoContas> PlanosPai { get; private set; } = new();

        public IReadOnlyList<string> TiposConta { get; } = new[]
        {
            "Ativo",
            "Passivo",
            "Patrimonio Liquido",
            "Receita",
            "Deducao da Receita",
            "Custo",
            "Despesa",
            "Receita Financeira",
            "Despesa Financeira",
            "Outras Receitas",
            "Outras Despesas",
            "Tributo sobre Lucro",
            "Participacao"
        };

        public IReadOnlyList<string> GruposDre { get; } = new[]
        {
            "Receita Bruta",
            "Deducoes da Receita",
            "Custos",
            "Despesas Comerciais",
            "Despesas Administrativas",
            "Despesas com Pessoal",
            "Despesas Operacionais Gerais",
            "Resultado Financeiro",
            "Outras Receitas Operacionais",
            "Outras Despesas Operacionais",
            "IRPJ e CSLL",
            "Participacoes"
        };

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        public async Task<IActionResult> OnPostSalvarAsync()
        {
            try
            {
                await _financeiroService.SavePlanoContasAsync(ObterIdSalao(), PlanoInput);
                FlashMessage = "Plano de contas salvo com sucesso.";
                FlashType = "success";
                return RedirectToPage("/Financeiro/PlanoContas");
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                await CarregarAsync(carregarPlano: false);
                return Page();
            }
        }

        private async Task CarregarAsync(bool carregarPlano = true)
        {
            var planos = await _financeiroService.ListarPlanoContasAsync(ObterIdSalao());
            var idPlano = id ?? (PlanoInput.IdPlano > 0 ? PlanoInput.IdPlano : null);

            if (carregarPlano && idPlano.HasValue)
            {
                PlanoInput = planos.FirstOrDefault(p => p.IdPlano == idPlano.Value) ?? PlanoInput;
            }

            PlanosPai = planos
                .Where(p => p.Ativo)
                .Where(p => p.IdPlano != PlanoInput.IdPlano)
                .Where(p => string.IsNullOrWhiteSpace(PlanoInput.Codigo)
                    || string.IsNullOrWhiteSpace(p.Codigo)
                    || !p.Codigo.StartsWith($"{PlanoInput.Codigo}.", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Codigo)
                .ThenBy(p => p.NomeExibicao)
                .ToList();
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
