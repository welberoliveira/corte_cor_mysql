using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class TarefaCadastroModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;

        public TarefaCadastroModel(CrmService crmService, PessoaHandler pessoaHandler)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
        }

        public List<Pessoa> Clientes { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdTarefa { get; set; }

        [BindProperty]
        public CrmTarefa TarefaInput { get; set; } = new();

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            if (IdTarefa.HasValue && IdTarefa.Value > 0)
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                var tarefa = _crmService.ObterTarefa(idSalao, IdTarefa.Value);
                if (tarefa == null)
                {
                    FlashMessage = "Tarefa não encontrada.";
                    FlashType = "warning";
                    return RedirectToPage("/CRM/Tarefas", new { IdPessoa });
                }

                TarefaInput = tarefa;
                IdPessoa = tarefa.IdPessoa;
                return Carregar();
            }

            TarefaInput = new CrmTarefa
            {
                IdPessoa = IdPessoa,
                Prioridade = "Media",
                Status = CrmStatusTarefa.Aberta,
                CanalSugerido = "Telefone",
                DataVencimento = DateTime.Now.AddDays(1),
                IdUsuarioResponsavel = ObterIdUsuario()
            };

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

                TarefaInput.IdUsuarioResponsavel ??= ObterIdUsuario();
                var editando = TarefaInput.IdTarefa > 0;
                _crmService.SalvarTarefa(idSalao, TarefaInput);
                FlashMessage = editando ? "Tarefa atualizada com sucesso." : "Tarefa criada com sucesso.";
                FlashType = "success";
                return RedirectToPage("/CRM/Tarefas", new { IdPessoa = TarefaInput.IdPessoa });
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
            TarefaInput.Prioridade = string.IsNullOrWhiteSpace(TarefaInput.Prioridade) ? "Media" : TarefaInput.Prioridade;
            TarefaInput.Status = string.IsNullOrWhiteSpace(TarefaInput.Status) ? CrmStatusTarefa.Aberta : TarefaInput.Status;
            TarefaInput.CanalSugerido = string.IsNullOrWhiteSpace(TarefaInput.CanalSugerido) ? "Telefone" : TarefaInput.CanalSugerido;
            TarefaInput.DataVencimento = TarefaInput.DataVencimento == default ? DateTime.Now.AddDays(1) : TarefaInput.DataVencimento;
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
