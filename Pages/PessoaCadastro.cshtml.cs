using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PessoaCadastroModel : PageModel
    {
        public Pessoa Pessoa { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                var handler = new PessoaHandler();
                Pessoa = handler.Listar().FirstOrDefault(p => p.IdPessoa == id.Value);
                ButtonText = "Atualizar";
            }
        }

        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            DateTime? dataNascimento = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["dataNascimento"]))
                dataNascimento = DateTime.Parse(Request.Form["dataNascimento"]);

            var Pessoa = new Pessoa
            {
                IdPessoa = id,
                Nome = Request.Form["nome"],
                Telefone = Request.Form["telefone"],
                Email = Request.Form["email"],
                DataNascimento = dataNascimento,
                IdSalao = idSalao
            };

            var handler = new PessoaHandler();

            if (id > 0)
            {
                handler.Atualizar(Pessoa);
                Mensagem = "Pessoa atualizada com sucesso!";
            }
            else
            {
                id = handler.CadastrarPessoa(Pessoa);
                Mensagem = "Pessoa cadastrada com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null);
        }
    }
}
