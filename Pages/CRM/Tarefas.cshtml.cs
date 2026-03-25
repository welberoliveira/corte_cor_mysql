using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class TarefasModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;

        public TarefasModel(CrmService crmService, PessoaHandler pessoaHandler)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
        }

        public PagedResult<CrmTarefa> Tarefas { get; private set; } = new();
        public List<Pessoa> Clientes { get; private set; } = new();

        [BindProperty]
        public CrmTarefa NovaTarefa { get; set; } = new()
        {
            Prioridade = "Media",
            Status = CrmStatusTarefa.Aberta,
            DataVencimento = DateTime.Now.AddDays(1)
        };

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

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

                NovaTarefa.IdUsuarioResponsavel = ObterIdUsuario();
                _crmService.SalvarTarefa(idSalao, NovaTarefa);
                FlashMessage = "Tarefa CRM salva com sucesso.";
                FlashType = "success";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { IdPessoa, Status, p = 1 });
        }

        public IActionResult OnPostAtualizarStatus(int idTarefa, string status)
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                _crmService.AtualizarStatusTarefa(idSalao, idTarefa, status);
                FlashMessage = "Status da tarefa atualizado.";
                FlashType = "success";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { IdPessoa, Status, p });
        }

        private IActionResult Carregar()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Tarefas = _crmService.ListarTarefas(idSalao, IdPessoa, Status, ObterIdUsuario(), p, 12);
            Clientes = _pessoaHandler.ListarPaginadoPorSalao(idSalao, null, 1, 500).Items;
            NovaTarefa = new CrmTarefa
            {
                IdPessoa = IdPessoa,
                Prioridade = "Media",
                Status = CrmStatusTarefa.Aberta,
                DataVencimento = DateTime.Now.AddDays(1)
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
