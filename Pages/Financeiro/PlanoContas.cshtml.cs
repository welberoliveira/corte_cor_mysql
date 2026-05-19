using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PlanoContasModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public PlanoContasModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public List<PlanoContas> Planos { get; private set; } = new();
        public List<string> TiposConta { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Pesquisa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TipoConta { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Ativo { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? AceitaLancamento { get; set; }

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            var planos = await _financeiroService.ListarPlanoContasAsync(ObterIdSalao());

            TiposConta = planos
                .Select(p => string.IsNullOrWhiteSpace(p.TipoConta) ? (p.Tipo == "R" ? "Receita" : "Despesa") : p.TipoConta)
                .Where(tipo => !string.IsNullOrWhiteSpace(tipo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tipo => tipo)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Pesquisa))
            {
                var termo = Pesquisa.Trim();
                planos = planos
                    .Where(p =>
                        (p.Codigo?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        p.NomeExibicao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        (p.GrupoDRE?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(TipoConta))
            {
                planos = planos
                    .Where(p =>
                    {
                        var tipo = string.IsNullOrWhiteSpace(p.TipoConta) ? (p.Tipo == "R" ? "Receita" : "Despesa") : p.TipoConta;
                        return string.Equals(tipo, TipoConta, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
            }

            if (Ativo.HasValue)
            {
                planos = planos.Where(p => p.Ativo == Ativo.Value).ToList();
            }

            if (AceitaLancamento.HasValue)
            {
                planos = planos.Where(p => p.AceitaLancamento == AceitaLancamento.Value).ToList();
            }

            Planos = planos;
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
