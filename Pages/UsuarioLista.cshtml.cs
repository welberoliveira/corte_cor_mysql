using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class UsuarioListaModel : PageModel
    {
        public List<Usuario>? Usuarios { get; set; } = new();
        public string Mensagem { get; set; }
        public string StatusFilter { get; set; } = "Ativo";

        public string NomeClientes { get; set; }
        public string NomeCliente { get; set; }

        public List<Salao> Saloes { get; set; } = new();

        public void OnGet(string statusFilter = "Ativo")
        {
            var SalaoHandler = new SalaoHandler();
            Saloes = SalaoHandler.Listar();

            try
            {
                StatusFilter = statusFilter;
            var handler = new UsuarioHandler();
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);
            var allUsuarios = handler.ListarPorSalao(idSalao);
            Usuarios = StatusFilter == "Ativo" ? allUsuarios.Where(m => m.Status == "Ativo").ToList() : allUsuarios;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void OnPost()
        {
            var handler = new UsuarioHandler();

            int id = int.Parse(Request.Form["id"]);
            var action = Request.Form["action"];

            if (action == "ativar")
            {
                handler.AtivarDesativar(id, true);
                Mensagem = $"Usuário ativado com sucesso.";
            }
            else if (action == "desativar")
            {
                handler.AtivarDesativar(id, false);
                Mensagem = $"Usuário desativado com sucesso.";
            }
            else if (action == "excluir")
            {
                try
                {
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
                    Mensagem = $"Usuário excluído com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir esse registro porque ele está associado a um ou mais eventos";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/UsuarioCadastro?id={id}");
            }

            OnGet();
        }
    }
}

