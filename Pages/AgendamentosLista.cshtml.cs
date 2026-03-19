using CorteCor;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class AgendamentosListaModel : PageModel
    {
        private readonly AgendamentoHandler _handler;
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly FuncionarioHandler _funcionarioHandler;
        private readonly NotaFiscalHandler _notaFiscalHandler;

        public AgendamentosListaModel(
            AgendamentoHandler handler,
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            FuncionarioHandler funcionarioHandler,
            NotaFiscalHandler notaFiscalHandler)
        {
            _handler = handler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _funcionarioHandler = funcionarioHandler;
            _notaFiscalHandler = notaFiscalHandler;
        }

        public PagedResult<Agendamento> Agendamentos { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = string.Empty;

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

        public List<Servico> ServicosOptions { get; set; } = new();
        public List<Pessoa> PessoasOptions { get; set; } = new();
        public List<Funcionario> FuncionariosOptions { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new()
        {
            AgendamentoStatus.Agendado,
            AgendamentoStatus.Pago,
            AgendamentoStatus.Pendente,
            AgendamentoStatus.Cancelado
        };

        private readonly Dictionary<int, string> _servicosCache = new();
        private readonly Dictionary<int, string> _pessoasCache = new();
        private readonly Dictionary<int, string> _funcionariosCache = new();
        private readonly Dictionary<int, NotaFiscal> _fiscalCache = new();

        public async Task OnGetAsync()
        {
            try
            {
                var idSalaoClaim = User.FindFirst("IdSalao");
                if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out var idSalao))
                {
                    CarregarDadosApoio(idSalao);
                    Agendamentos = _handler.ListarFiltrado(idSalao, DataInicio, DataFim, Status, IdServico, IdPessoa, IdFuncionario, MostrarExcluidos, p > 0 ? p : 1, 10);

                    foreach (var agendamento in Agendamentos.Items)
                    {
                        agendamento.Status = AgendamentoStatus.Normalizar(agendamento.Status);
                    }

                    await CarregarSituacaoFiscalAsync(idSalao);
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
            ServicosOptions = _servicoHandler.ListarPorSalao(idSalao).OrderBy(s => s.Nome).ToList();
            foreach (var servico in ServicosOptions)
            {
                _servicosCache[servico.IdServico] = servico.Nome;
            }

            var pessoas = _pessoaHandler.ListarPorSalao(idSalao);
            foreach (var pessoa in pessoas)
            {
                _pessoasCache[pessoa.IdPessoa] = pessoa.Nome;
            }

            var excluidos = _pessoaHandler.ListarExcluidos(idSalao);
            foreach (var pessoa in excluidos)
            {
                _pessoasCache[pessoa.IdPessoa] = pessoa.Nome + " (Excluido)";
            }

            PessoasOptions = pessoas.Concat(excluidos).OrderBy(pessoa => pessoa.Nome).ToList();

            FuncionariosOptions = _funcionarioHandler.ListarPorSalao(idSalao).OrderBy(f => f.Nome).ToList();
            foreach (var funcionario in FuncionariosOptions)
            {
                _funcionariosCache[funcionario.IdFuncionario] = funcionario.Nome;
            }
        }

        private async Task CarregarSituacaoFiscalAsync(int idSalao)
        {
            var notas = await _notaFiscalHandler.ListarPorSalaoAsync(idSalao);
            var notasPorAgendamento = notas
                .Where(n => n.IdAgendamento.HasValue)
                .GroupBy(n => n.IdAgendamento!.Value);

            foreach (var grupo in notasPorAgendamento)
            {
                var nota = grupo
                    .OrderByDescending(NotaAtiva)
                    .ThenByDescending(n => n.DataEmissao)
                    .ThenByDescending(n => n.DataAtualizacao)
                    .FirstOrDefault();

                if (nota != null)
                {
                    _fiscalCache[grupo.Key] = nota;
                }
            }
        }

        public string GetServicoNome(int id) => _servicosCache.TryGetValue(id, out var nome) ? nome : "N/D";

        public string GetPessoaNome(int id) => _pessoasCache.TryGetValue(id, out var nome) ? nome : "N/D";

        public string GetFuncionarioNome(int id)
        {
            if (_funcionariosCache.TryGetValue(id, out var nome))
            {
                return nome;
            }

            try
            {
                var funcionario = _funcionarioHandler.ObterPorId(id);
                if (funcionario != null)
                {
                    _funcionariosCache[id] = funcionario.Nome;
                    return funcionario.Nome;
                }
            }
            catch
            {
            }

            return "N/D";
        }

        public string GetStatusBadgeClass(string? status) => AgendamentoStatus.ObterClasseBadgeBootstrap(status);

        public string GetFiscalStatus(int idAgendamento) =>
            _fiscalCache.TryGetValue(idAgendamento, out var nota) ? nota.Status : "Sem nota";

        public string GetFiscalBadgeClass(int idAgendamento)
        {
            if (!_fiscalCache.TryGetValue(idAgendamento, out var nota))
            {
                return "bg-secondary";
            }

            return NotaFiscalAvulsaService.ObterClasseStatus(nota.Status);
        }

        public string GetFiscalDescricao(int idAgendamento)
        {
            if (!_fiscalCache.TryGetValue(idAgendamento, out var nota))
            {
                return "Sem nota emitida";
            }

            return $"{nota.TipoNota} {nota.Numero}/{nota.Serie}";
        }

        public string GetNotaFiscalUrl(int idAgendamento)
        {
            if (!_fiscalCache.ContainsKey(idAgendamento))
            {
                return string.Empty;
            }

            return Url.Page("/NotaFiscalLista", new
            {
                idAgendamento
            }) ?? string.Empty;
        }

        private static bool NotaAtiva(NotaFiscal nota)
        {
            return !string.Equals(nota.Status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(nota.Status, NotaFiscalStatus.Rejeitada, StringComparison.OrdinalIgnoreCase);
        }
    }
}
