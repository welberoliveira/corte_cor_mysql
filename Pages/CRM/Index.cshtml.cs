using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public List<CrmEtapaFunil> Etapas { get; private set; } = new();
        public List<CrmOportunidade> OportunidadesKanban { get; private set; } = new();

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
            Etapas = _crmService.ListarEtapas(idSalao);
            OportunidadesKanban = _crmService.ListarOportunidades(idSalao, null, CrmStatusOportunidade.Aberta);
            Clientes = _crmService.ListarClientes(idSalao, q, p, 10);
            return Page();
        }

        public IActionResult OnPostMoverOportunidade(int idOportunidade, int idEtapa)
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return new JsonResult(new { success = false, message = "Sessão inválida. Faça login novamente." })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }

                _crmService.MoverOportunidade(idSalao, idOportunidade, idEtapa);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
