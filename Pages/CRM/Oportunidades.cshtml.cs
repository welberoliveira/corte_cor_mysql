using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class OportunidadesModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;

        public OportunidadesModel(CrmService crmService, PessoaHandler pessoaHandler)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
        }

        public List<CrmEtapaFunil> Etapas { get; private set; } = new();
        public List<CrmOportunidade> Oportunidades { get; private set; } = new();
        public List<Pessoa> Clientes { get; private set; } = new();

        [BindProperty]
        public CrmOportunidade NovaOportunidade { get; set; } = new()
        {
            Status = CrmStatusOportunidade.Aberta,
            Probabilidade = 50,
            PrevisaoFechamento = DateTime.Today.AddDays(30)
        };

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

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

                _crmService.SalvarOportunidade(idSalao, NovaOportunidade);
                FlashMessage = "Oportunidade salva com sucesso.";
                FlashType = "success";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { IdPessoa, Status });
        }

        public IActionResult OnPostMover(int idOportunidade, int idEtapa)
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                _crmService.MoverOportunidade(idSalao, idOportunidade, idEtapa);
                FlashMessage = "Oportunidade atualizada.";
                FlashType = "success";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { IdPessoa, Status });
        }

        private IActionResult Carregar()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Etapas = _crmService.ListarEtapas(idSalao);
            Oportunidades = _crmService.ListarOportunidades(idSalao, IdPessoa, Status);
            Clientes = _pessoaHandler.ListarPaginadoPorSalao(idSalao, null, 1, 500).Items;
            NovaOportunidade = new CrmOportunidade
            {
                IdPessoa = IdPessoa ?? 0,
                IdEtapa = Etapas.FirstOrDefault()?.IdEtapa ?? 0,
                Status = CrmStatusOportunidade.Aberta,
                Probabilidade = 50,
                PrevisaoFechamento = DateTime.Today.AddDays(30)
            };
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
