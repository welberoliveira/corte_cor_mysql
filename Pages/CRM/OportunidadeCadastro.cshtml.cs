using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class OportunidadeCadastroModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;

        public OportunidadeCadastroModel(CrmService crmService, PessoaHandler pessoaHandler)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
        }

        public List<Pessoa> Clientes { get; private set; } = new();
        public List<CrmEtapaFunil> Etapas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty]
        public CrmOportunidade OportunidadeInput { get; set; } = new();

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            var result = Carregar();
            OportunidadeInput = new CrmOportunidade
            {
                IdPessoa = IdPessoa.GetValueOrDefault(),
                IdEtapa = Etapas.FirstOrDefault()?.IdEtapa ?? 0,
                Status = CrmStatusOportunidade.Aberta,
                Probabilidade = 50,
                PrevisaoFechamento = DateTime.Today.AddDays(30)
            };

            return result;
        }

        public IActionResult OnPostSalvar()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                _crmService.SalvarOportunidade(idSalao, OportunidadeInput);
                FlashMessage = "Oportunidade criada com sucesso.";
                FlashType = "success";
                return RedirectToPage("/CRM/Oportunidades", new { IdPessoa = OportunidadeInput.IdPessoa });
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
            Etapas = _crmService.ListarEtapas(idSalao);
            OportunidadeInput.Status = string.IsNullOrWhiteSpace(OportunidadeInput.Status) ? CrmStatusOportunidade.Aberta : OportunidadeInput.Status;
            OportunidadeInput.Probabilidade = OportunidadeInput.Probabilidade == 0 ? 50 : OportunidadeInput.Probabilidade;
            OportunidadeInput.IdEtapa = OportunidadeInput.IdEtapa == 0 ? Etapas.FirstOrDefault()?.IdEtapa ?? 0 : OportunidadeInput.IdEtapa;
            OportunidadeInput.PrevisaoFechamento ??= DateTime.Today.AddDays(30);
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
