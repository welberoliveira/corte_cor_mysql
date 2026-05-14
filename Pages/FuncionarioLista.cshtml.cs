using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioListaModel : PageModel
    {
        private readonly FuncionarioHandler _funcionarioHandler;

        public PagedResult<Funcionario> Funcionarios { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public FuncionarioListaModel(FuncionarioHandler funcionarioHandler)
        {
            _funcionarioHandler = funcionarioHandler;
        }

        public void OnGet()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Funcionarios = new PagedResult<Funcionario> { PageIndex = 1, PageSize = 10 };
            Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                return;
            }

            Funcionarios = _funcionarioHandler.ListarPaginadoPorSalao(idSalao, q, p, 10);
        }

        public IActionResult OnPost(int id, string action, string? q, int p = 1)
        {
            if (action == "alterar")
            {
                return Redirect($"{HttpContext.Request.PathBase}/FuncionarioCadastro?id={id}");
            }

            if (!TryObterIdSalao(out var idSalao))
            {
            Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                OnGet();
                return Page();
            }

            if (action == "excluir")
            {
                try
                {
                    _funcionarioHandler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Funcionario excluido com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception)
                {
            Mensagem = "Não foi possível excluir este funcionário porque ele está associado a outros registros.";
                    MensagemTipo = "warning";
                }
            }

            this.q = q;
            this.p = p;
            OnGet();
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}

