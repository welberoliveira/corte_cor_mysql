using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class RelatoriosModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public RelatoriosModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public FinanceiroRelatorioResumo Relatorio { get; private set; } = new();
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public async Task OnGetAsync(DateTime? dataInicio, DateTime? dataFim)
        {
            DataInicio = dataInicio?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DataFim = dataFim?.Date ?? DateTime.Today;
            Relatorio = await _financeiroService.ObterRelatoriosAsync(ObterIdSalao(), DataInicio, DataFim);
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
