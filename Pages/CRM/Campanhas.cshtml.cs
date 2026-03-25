using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CampanhasModel : PageModel
    {
        private readonly CrmService _crmService;

        public CampanhasModel(CrmService crmService)
        {
            _crmService = crmService;
        }

        public PagedResult<CrmCampanha> Campanhas { get; private set; } = new();
        public List<CrmCampanhaDestino> DestinosCampanha { get; private set; } = new();

        [BindProperty]
        public CrmCampanha NovaCampanha { get; set; } = new()
        {
            Canal = CrmCanal.Email,
            Segmento = CrmSegmentoCampanha.TodosClientes,
            Status = "Rascunho",
            DiasInatividade = 60
        };

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? IdCampanhaView { get; set; }

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            return Carregar();
        }

        public IActionResult OnPostSalvar()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                var idCampanha = _crmService.SalvarCampanha(idSalao, NovaCampanha);
                FlashMessage = "Campanha salva com sucesso.";
                FlashType = "success";
                return RedirectToPage(new { IdCampanhaView = idCampanha });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEnviarAsync(int idCampanha)
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                var resultado = await _crmService.EnviarCampanhaAsync(idSalao, idCampanha, ObterIdUsuario());
                FlashMessage = $"Campanha processada. Sucesso: {resultado.Campanha.TotalSucesso} | Falha: {resultado.Campanha.TotalFalha}.";
                FlashType = resultado.Campanha.TotalFalha > 0 ? "warning" : "success";
                return RedirectToPage(new { IdCampanhaView = idCampanha });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage(new { IdCampanhaView = idCampanha });
            }
        }

        private IActionResult Carregar()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Campanhas = _crmService.ListarCampanhas(idSalao, p, 10);
            if (IdCampanhaView.HasValue && IdCampanhaView.Value > 0)
            {
                DestinosCampanha = _crmService.ListarDestinosCampanha(idSalao, IdCampanhaView.Value);
            }

            NovaCampanha = new CrmCampanha
            {
                Canal = CrmCanal.Email,
                Segmento = CrmSegmentoCampanha.TodosClientes,
                Status = "Rascunho",
                DiasInatividade = 60
            };

            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }

        private int? ObterIdUsuario()
        {
            return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var idUsuario) && idUsuario > 0
                ? idUsuario
                : null;
        }
    }
}
