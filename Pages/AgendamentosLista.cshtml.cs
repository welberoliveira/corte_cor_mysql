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

        public PagedResult<Agendamento> Agendamentos { get; set; } = new PagedResult<Agendamento>();
        public string Mensagem { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdServico { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdFuncionario { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool MostrarExcluidos { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public List<Servico> ServicosOptions { get; set; }
        public List<Pessoa> PessoasOptions { get; set; }
        public List<Funcionario> FuncionariosOptions { get; set; }
        public List<string> StatusOptions { get; set; } = new List<string> { "Agendado", "Pago", "Pendente", "Cancelado" };

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
                    CarregarDadosApoio(idSalao);
                    Agendamentos = _handler.ListarFiltrado(idSalao, DataInicio, DataFim, Status, IdServico, IdPessoa, IdFuncionario, MostrarExcluidos, p > 0 ? p : 1, 10);
                    
                    // Display Fix: Map "Confirmado" to "Pago"
                    foreach (var ag in Agendamentos.Items)
                    {
                        if (ag.Status == "Confirmado") ag.Status = "Pago";
                    }
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
            ServicosOptions = _servicoHandler.Listar().Where(s => s.IdSalao == idSalao).OrderBy(s => s.Nome).ToList();
            foreach(var s in ServicosOptions) _servicosCache[s.IdServico] = s.Nome;

            var pessoas = _pessoaHandler.ListarPorSalao(idSalao); 
            foreach(var p in pessoas) _pessoasCache[p.IdPessoa] = p.Nome;
            
            var excluidos = _pessoaHandler.ListarExcluidos(idSalao);
            foreach(var p in excluidos) _pessoasCache[p.IdPessoa] = p.Nome + " (Excluído)";

            // Combine for dropdown
            PessoasOptions = pessoas.Concat(excluidos).OrderBy(p => p.Nome).ToList();

            FuncionariosOptions = _funcionarioHandler.ListarPorSalao(idSalao).OrderBy(f => f.Nome).ToList();
            foreach(var f in FuncionariosOptions) _funcionariosCache[f.IdFuncionario] = f.Nome;
        }

        public string GetServicoNome(int id) => _servicosCache.ContainsKey(id) ? _servicosCache[id] : "N/D";
        public string GetPessoaNome(int id) => _pessoasCache.ContainsKey(id) ? _pessoasCache[id] : "N/D";
        public string GetFuncionarioNome(int id) 
        {
            if (_funcionariosCache.ContainsKey(id)) return _funcionariosCache[id];
            
            // Fallback: Tenta buscar individualmente (pode ser funcionário deletado ou de outro contexto)
            try {
                var f = _funcionarioHandler.ObterPorId(id);
                if (f != null) {
                    _funcionariosCache[id] = f.Nome;
                    return f.Nome;
                }
            } catch {}
            
            return "N/D";
        }
    }
}
