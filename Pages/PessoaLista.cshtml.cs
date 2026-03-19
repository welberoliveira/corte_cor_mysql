using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PessoaListaModel : PageModel
    {
        private readonly PessoaHandler _pessoaHandler;

        public PessoaListaModel(PessoaHandler pessoaHandler)
        {
            _pessoaHandler = pessoaHandler;
        }

        public PagedResult<Pessoa> Pessoas { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        public void OnGet()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Pessoas = new PagedResult<Pessoa> { PageIndex = 1, PageSize = 10 };
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                return;
            }

            Pessoas = _pessoaHandler.ListarPaginadoPorSalao(idSalao, q, p, 10);
        }

        public IActionResult OnPost(int id, string action, string? q, int p = 1)
        {
            if (action == "alterar")
            {
                return Redirect($"{HttpContext.Request.PathBase}/PessoaCadastro?id={id}");
            }

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                OnGet();
                return Page();
            }

            if (action == "excluir")
            {
                try
                {
                    _pessoaHandler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Pessoa excluida com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception)
                {
                    Mensagem = "Nao foi possivel excluir esta pessoa porque ela esta associada a outros registros.";
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
