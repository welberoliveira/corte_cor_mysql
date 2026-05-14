using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class TarefasModel : PageModel
    {
        private readonly CrmService _crmService;
        private readonly PessoaHandler _pessoaHandler;
        private readonly ILogger<TarefasModel> _logger;

        public TarefasModel(CrmService crmService, PessoaHandler pessoaHandler, ILogger<TarefasModel> logger)
        {
            _crmService = crmService;
            _pessoaHandler = pessoaHandler;
            _logger = logger;
        }

        public PagedResult<CrmTarefa> Tarefas { get; private set; } = new();
        public List<Pessoa> Clientes { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PesquisaDescricao { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataVencimentoInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataVencimentoFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            try
            {
                return Carregar();
            }
            catch (Exception ex)
            {
                return ExibirErroCarregamento(ex);
            }
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
                _logger.LogError(ex, "Erro ao atualizar status da tarefa CRM.");
                FlashMessage = $"Não foi possível atualizar a tarefa no momento. Detalhe técnico retornado pelo servidor: {SanitizarDetalheTecnico(ex)}";
                FlashType = "danger";
            }

            return RedirectToPage(new { IdPessoa, Status, PesquisaDescricao, DataVencimentoInicio, DataVencimentoFim, p });
        }

        private IActionResult Carregar()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            Tarefas = _crmService.ListarTarefas(idSalao, IdPessoa, Status, ObterIdUsuario(), p, 12, PesquisaDescricao, DataVencimentoInicio, DataVencimentoFim);
            Clientes = _pessoaHandler.ListarPaginadoPorSalao(idSalao, null, 1, 500).Items;
            return Page();
        }

        private IActionResult ExibirErroCarregamento(Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar tarefas CRM.");
            Tarefas = new PagedResult<CrmTarefa>();
            Clientes = new List<Pessoa>();
            FlashMessage = $"Não foi possível carregar as tarefas no momento. Detalhe técnico retornado pelo servidor: {SanitizarDetalheTecnico(ex)}";
            FlashType = "danger";
            return Page();
        }

        private static string SanitizarDetalheTecnico(Exception ex)
        {
            var detalhe = ex.GetBaseException().Message ?? ex.Message;
            detalhe = detalhe.Replace("\r", " ").Replace("\n", " ").Trim();
            return detalhe.Length <= 500 ? detalhe : detalhe[..500] + "...";
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
