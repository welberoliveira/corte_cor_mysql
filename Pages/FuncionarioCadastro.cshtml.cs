using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioCadastroModel : PageModel
    {
        private readonly FuncionarioHandler _funcionarioHandler;

        public Funcionario Funcionario { get; set; } = new Funcionario();
        public string ButtonText { get; set; } = "Cadastrar";
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";

        public FuncionarioCadastroModel(FuncionarioHandler funcionarioHandler)
        {
            _funcionarioHandler = funcionarioHandler;
        }

        public void OnGet(int? id)
        {
            if (!TryObterIdSalao(out var idSalao))
            {
            Mensagem = "Não foi possível identificar o salão atual.";
                MensagemTipo = "danger";
                return;
            }

            if (!id.HasValue)
            {
                return;
            }

            Funcionario = _funcionarioHandler.ObterPorIdESalao(id.Value, idSalao) ?? new Funcionario();
            ButtonText = Funcionario.IdFuncionario > 0 ? "Atualizar" : "Cadastrar";
        }

        private static bool GetBool(string key, Microsoft.AspNetCore.Http.IFormCollection form)
        {
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
            if (!TryObterIdSalao(out var idSalao))
            {
            Mensagem = "Não foi possível identificar o salão atual.";
                MensagemTipo = "danger";
                return;
            }

            int.TryParse(Request.Form["id"], out var id);
            var form = Request.Form;
            var funcionario = new Funcionario
            {
                IdFuncionario = id,
                Nome = form["nome"].ToString().Trim(),
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
                dom_fim = GetTime("dom_fim", form)
            };

            if (!ValidarFuncionario(funcionario))
            {
                Funcionario = funcionario;
                ButtonText = funcionario.IdFuncionario > 0 ? "Atualizar" : "Cadastrar";
                return;
            }

            if (id > 0)
            {
                _funcionarioHandler.Atualizar(funcionario);
                Mensagem = "Funcionario atualizado com sucesso.";
            }
            else
            {
                funcionario.IdFuncionario = _funcionarioHandler.CadastrarFuncionario(funcionario);
                Mensagem = "Funcionario cadastrado com sucesso.";
            }

            MensagemTipo = "success";
            Funcionario = funcionario;
            ButtonText = "Atualizar";
        }

        private bool ValidarFuncionario(Funcionario funcionario)
        {
            if (string.IsNullOrWhiteSpace(funcionario.Nome))
            {
                Mensagem = "Informe o nome do funcionario.";
                MensagemTipo = "warning";
                return false;
            }

            var possuiDiaSelecionado = funcionario.seg || funcionario.ter || funcionario.qua || funcionario.qui || funcionario.sex || funcionario.sab || funcionario.dom;
            if (!possuiDiaSelecionado)
            {
                Mensagem = "Selecione pelo menos um dia de atendimento.";
                MensagemTipo = "warning";
                return false;
            }

            foreach (var erro in ValidarFaixa("segunda-feira", funcionario.seg, funcionario.seg_ini, funcionario.seg_fim)
                .Concat(ValidarFaixa("terca-feira", funcionario.ter, funcionario.ter_ini, funcionario.ter_fim))
                .Concat(ValidarFaixa("quarta-feira", funcionario.qua, funcionario.qua_ini, funcionario.qua_fim))
                .Concat(ValidarFaixa("quinta-feira", funcionario.qui, funcionario.qui_ini, funcionario.qui_fim))
                .Concat(ValidarFaixa("sexta-feira", funcionario.sex, funcionario.sex_ini, funcionario.sex_fim))
                .Concat(ValidarFaixa("sabado", funcionario.sab, funcionario.sab_ini, funcionario.sab_fim))
                .Concat(ValidarFaixa("domingo", funcionario.dom, funcionario.dom_ini, funcionario.dom_fim)))
            {
                Mensagem = erro;
                MensagemTipo = "warning";
                return false;
            }

            return true;
        }

        private static IEnumerable<string> ValidarFaixa(string dia, bool ativo, TimeSpan? inicio, TimeSpan? fim)
        {
            if (!ativo)
            {
                yield break;
            }

            if (!inicio.HasValue || !fim.HasValue)
            {
                yield return $"Preencha inicio e fim para {dia}.";
                yield break;
            }

            if (inicio.Value >= fim.Value)
            {
                yield return $"O horario de inicio deve ser menor que o horario de fim em {dia}.";
            }
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
