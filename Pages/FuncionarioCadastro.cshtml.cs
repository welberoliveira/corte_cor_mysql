using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioCadastroModel : PageModel
    {
        public Funcionario Funcionario { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                var handler = new FuncionarioHandler();
                Funcionario = handler.ListarPorSalao(idSalao).FirstOrDefault(p => p.IdFuncionario == id.Value);
                ButtonText = "Atualizar";
            }
        }

        private static bool GetBool(string key, Microsoft.AspNetCore.Http.IFormCollection form)
        {
            // checkbox não marcado não vem no form
            var v = form[key].ToString();
            return v == "true" || v == "on" || v == "1";
        }

        private static TimeSpan? GetTime(string key, Microsoft.AspNetCore.Http.IFormCollection form)
        {
            var v = form[key].ToString();
            if (string.IsNullOrWhiteSpace(v)) return null;
            return TimeSpan.Parse(v);
        }

        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var form = Request.Form;

            var funcionario = new Funcionario
            {
                IdFuncionario = id,
                Nome = form["nome"],
                IdSalao = idSalao,

                seg = GetBool("seg", form),
                seg_ini = GetTime("seg_ini", form),
                seg_fim = GetTime("seg_fim", form),

                ter = GetBool("ter", form),
                ter_ini = GetTime("ter_ini", form),
                ter_fim = GetTime("ter_fim", form),

                qua = GetBool("qua", form),
                qua_ini = GetTime("qua_ini", form),
                qua_fim = GetTime("qua_fim", form),

                qui = GetBool("qui", form),
                qui_ini = GetTime("qui_ini", form),
                qui_fim = GetTime("qui_fim", form),

                sex = GetBool("sex", form),
                sex_ini = GetTime("sex_ini", form),
                sex_fim = GetTime("sex_fim", form),

                sab = GetBool("sab", form),
                sab_ini = GetTime("sab_ini", form),
                sab_fim = GetTime("sab_fim", form),

                dom = GetBool("dom", form),
                dom_ini = GetTime("dom_ini", form),
                dom_fim = GetTime("dom_fim", form),
            };

            var handler = new FuncionarioHandler();

            if (id > 0)
            {
                handler.Atualizar(funcionario);
                Mensagem = "Funcionário atualizado com sucesso!";
            }
            else
            {
                id = handler.CadastrarFuncionario(funcionario);
                Mensagem = "Funcionário cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null);
        }
    }
}
