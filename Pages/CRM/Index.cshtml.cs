using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class IndexModel : PageModel
    {
        private readonly CrmService _crmService;

        public IndexModel(CrmService crmService)
        {
            _crmService = crmService;
        }

        public CrmDashboardResumo Dashboard { get; private set; } = new();
        public PagedResult<CrmClienteResumo> Clientes { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public IActionResult OnGet()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Dashboard = _crmService.ObterDashboard(idSalao);
            Clientes = _crmService.ListarClientes(idSalao, q, p, 10);
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
