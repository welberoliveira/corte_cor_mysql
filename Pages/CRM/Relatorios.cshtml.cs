using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public List<Usuario> Responsaveis { get; private set; } = new();
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdUsuarioResponsavel { get; set; }

        public void OnGet(DateTime? dataInicio, DateTime? dataFim)
        {
            var idSalao = ObterIdSalao();
            DataInicio = dataInicio?.Date ?? DateTime.Today.AddDays(-30);
            DataFim = dataFim?.Date ?? DateTime.Today;
            Responsaveis = _crmService.ListarResponsaveis(idSalao);
            Relatorio = _crmService.ObterRelatorios(idSalao, DataInicio, DataFim, IdUsuarioResponsavel);
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
