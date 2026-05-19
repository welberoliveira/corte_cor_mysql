using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CampanhaCadastroModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;

        public CampanhaCadastroModel(CrmService crmService, PessoaHandler pessoaHandler)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
        }

        public List<Pessoa> Clientes { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? IdCampanha { get; set; }

        [BindProperty]
        public CrmCampanha CampanhaInput { get; set; } = new()
        {
            Canal = CrmCanal.Email,
            Segmento = CrmSegmentoCampanha.TodosClientes,
            Status = "Rascunho",
            DiasInatividade = 60
        };

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            if (IdCampanha.HasValue && IdCampanha.Value > 0)
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                var campanha = _crmService.ObterCampanha(idSalao, IdCampanha.Value);
                if (campanha == null)
                {
                    FlashMessage = "Campanha não encontrada.";
                    FlashType = "warning";
                    return RedirectToPage("/CRM/Campanhas");
                }

                CampanhaInput = campanha;
            }

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

                var editando = CampanhaInput.IdCampanha > 0;
                var idCampanha = _crmService.SalvarCampanha(idSalao, CampanhaInput);
                FlashMessage = editando ? "Campanha atualizada com sucesso." : "Campanha salva com sucesso.";
                FlashType = "success";
                return RedirectToPage("/CRM/Campanhas", new { IdCampanhaView = idCampanha });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return Carregar();
            }
        }

        private IActionResult Carregar()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Clientes = _pessoaHandler.ListarPaginadoPorSalao(idSalao, null, 1, 500).Items;
            CampanhaInput.Canal = string.IsNullOrWhiteSpace(CampanhaInput.Canal) ? CrmCanal.Email : CampanhaInput.Canal;
            CampanhaInput.Segmento = string.IsNullOrWhiteSpace(CampanhaInput.Segmento) ? CrmSegmentoCampanha.TodosClientes : CampanhaInput.Segmento;
            CampanhaInput.Status = string.IsNullOrWhiteSpace(CampanhaInput.Status) ? "Rascunho" : CampanhaInput.Status;
            CampanhaInput.DiasInatividade ??= 60;
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
