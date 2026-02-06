using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioServicoCadastroModel : PageModel
    {
        public List<Funcionario> Funcionarios { get; set; }
        public List<Servico> Servicos { get; set; }

        // Para manter selecionado após post/edição
        public int? IdFuncionarioSelecionado { get; set; }
        public List<int> ServicosSelecionadosIds { get; set; } = new List<int>();

        public string Mensagem { get; set; }

        public void OnGet(int? idFuncionario)
        {
            var funcionarioHandler = new FuncionarioHandler();
            var servicoHandler = new ServicoHandler();

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            Funcionarios = funcionarioHandler.ListarPorSalao(idSalao) ?? new List<Funcionario>();
            Servicos = servicoHandler.Listar() ?? new List<Servico>();

            if (idFuncionario.HasValue)
            {
                IdFuncionarioSelecionado = idFuncionario.Value;

                // Carrega os serviços já relacionados (tabela N:N)
                var fsHandler = new FuncionarioServicoHandler();
                var relacoes = fsHandler.ListarPorFuncionario(idFuncionario.Value) ?? new List<FuncionarioServico>();

                ServicosSelecionadosIds = relacoes.Select(r => r.IdServico).ToList();
            }
        }

        public void OnPost()
        {
            int idFuncionario = 0;
            int.TryParse(Request.Form["idFuncionario"], out idFuncionario);

            // checkbox: mesma key repetida => pega todos os values
            var idsSelecionados = new List<int>();
            var values = Request.Form["servicosSelecionados"].ToArray();

            foreach (var v in values)
            {
                if (int.TryParse(v, out var idServico))
                    idsSelecionados.Add(idServico);
            }

            var fsHandler = new FuncionarioServicoHandler();

            // Estratégia simples: limpa e regrava (ok, porque você quer salvar "o conjunto")
            fsHandler.ExcluirPorFuncionario(idFuncionario);

            foreach (var idServico in idsSelecionados.Distinct())
            {
                fsHandler.Cadastrar(new FuncionarioServico
                {
                    IdFuncionario = idFuncionario,
                    IdServico = idServico
                });
            }

            Mensagem = "Serviços vinculados ao funcionário com sucesso!";

            // Recarrega mantendo seleção
            OnGet(idFuncionario);
        }
    }
}
