using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class IndexModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public IndexModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public FinanceiroDashboardResumo Dashboard { get; private set; } = new();

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public async Task OnGetAsync(DateTime? dataInicio, DateTime? dataFim)
        {
            DataInicio = dataInicio?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DataFim = dataFim?.Date ?? DateTime.Today;
            Dashboard = await _financeiroService.ObterDashboardAsync(ObterIdSalao(), DataInicio, DataFim);
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
