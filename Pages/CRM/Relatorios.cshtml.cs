using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class RelatoriosModel : PageModel
    {
        private readonly CrmService _crmService;

        public RelatoriosModel(CrmService crmService)
        {
            _crmService = crmService;
        }

        public CrmRelatorioResumo Relatorio { get; private set; } = new();
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public void OnGet(DateTime? dataInicio, DateTime? dataFim)
        {
            DataInicio = dataInicio?.Date ?? DateTime.Today.AddDays(-30);
            DataFim = dataFim?.Date ?? DateTime.Today;
            Relatorio = _crmService.ObterRelatorios(ObterIdSalao(), DataInicio, DataFim);
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
