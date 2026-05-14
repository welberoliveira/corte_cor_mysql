using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class SalaoCadastroModel : PageModel
    {
        public Salao Salao { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                var handler = new SalaoHandler();
                Salao = handler.Listar().FirstOrDefault(p => p.IdSalao == id.Value);
                ButtonText = "Atualizar";
            }
        }

        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            var Salao = new Salao
            {
                IdSalao = id,
                Nome = Request.Form["nome"],
                Responsavel = Request.Form["responsavel"],
                Email = Request.Form["email"],
                Telefone = Request.Form["telefone"],
                Endereco = Request.Form["endereco"],
                CNPJ = Request.Form["cnpj"],
                Status = "Ativo",
                DataCadastro = DateTime.Parse(Request.Form["dataCadastro"]),
                Observacao = Request.Form["observacao"],
                LimiteEnvioEmail = int.Parse(Request.Form["limiteEnvioEmail"]),
                LimiteEnvioSMS = int.Parse(Request.Form["limiteEnvioSMS"]),
                LimiteEnvioWhatsapp = int.Parse(Request.Form["limiteEnvioWhatsapp"])
            };

            var handler = new SalaoHandler();

            if (id > 0)
            {
                handler.Atualizar(Salao);
                Mensagem = "Empresa atualizada com sucesso!";
            }
            else
            {
                id = handler.CadastrarSalao(Salao);
                Mensagem = "Empresa cadastrada com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null); // Recarrega os dados atualizados
        }
    }
}


