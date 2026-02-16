using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CorteCor;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    public class AgendamentosListaModel : PageModel
    {
        private readonly AgendamentoHandler _handler;
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly FuncionarioHandler _funcionarioHandler;

        public List<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
        public string Mensagem { get; set; }

        // Cache local para nao consultar banco excessivamente no loop, 
        // ou poderia fazer JOIN na query. Para simplicidade e consistencia com o projeto atual,
        // vou carregar listas de apoio ou buscar por ID se o cache nao for viavel.
        // Dado o volume, o ideal seria JOIN no Handler. Mas vou seguir o padrao simples.
        
        private Dictionary<int, string> _servicosCache = new Dictionary<int, string>();
        private Dictionary<int, string> _pessoasCache = new Dictionary<int, string>();
        private Dictionary<int, string> _funcionariosCache = new Dictionary<int, string>();

        public AgendamentosListaModel(AgendamentoHandler handler, 
                                      ServicoHandler servicoHandler,
                                      PessoaHandler pessoaHandler,
                                      FuncionarioHandler funcionarioHandler)
        {
            _handler = handler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _funcionarioHandler = funcionarioHandler;
        }

        public void OnGet()
        {
            try
            {
                var idSalaoClaim = User.FindFirst("IdSalao");
                if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
                {
                    Agendamentos = _handler.ListarTodos(idSalao);
                    CarregarDadosApoio(idSalao);
                }
                else
                {
                    Mensagem = "Erro ao identificar o salão do usuário.";
                }
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao carregar agendamentos: {ex.Message}";
            }
        }

        private void CarregarDadosApoio(int idSalao)
        {
            // Otimizacao basica: carregar todos do salao para dicionarios
            var servicos = _servicoHandler.Listar().Where(s => s.IdSalao == idSalao).ToList();
            foreach(var s in servicos) _servicosCache[s.IdServico] = s.Nome;

            var pessoas = _pessoaHandler.ListarPorSalao(idSalao); // Pega apenas ativos por padrao
            // Mas precisamos dos excluidos tambem para exibir o nome!
            // PessoaHandler deveria ter ListarTodos? Ou usar ListarExcluidos + Listar?
            // Vou usar Listar + ListarExcluidos e unir.
            foreach(var p in pessoas) _pessoasCache[p.IdPessoa] = p.Nome;
            
            var excluidos = _pessoaHandler.ListarExcluidos(idSalao);
            foreach(var p in excluidos) _pessoasCache[p.IdPessoa] = p.Nome + " (Excluído)";

            var funcionarios = _funcionarioHandler.Listar().Where(f => f.IdSalao == idSalao).ToList();
             foreach(var f in funcionarios) _funcionariosCache[f.IdFuncionario] = f.Nome;
        }

        public string GetServicoNome(int id) => _servicosCache.ContainsKey(id) ? _servicosCache[id] : "N/D";
        public string GetPessoaNome(int id) => _pessoasCache.ContainsKey(id) ? _pessoasCache[id] : "N/D";
        public string GetFuncionarioNome(int id) => _funcionariosCache.ContainsKey(id) ? _funcionariosCache[id] : "N/D";
    }
}
