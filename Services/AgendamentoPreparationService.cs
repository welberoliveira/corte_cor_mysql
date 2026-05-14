using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services
{
    public class AgendamentoTelaContexto
    {
        public List<Servico> Servicos { get; set; } = new();
        public List<Pessoa> Clientes { get; set; } = new();
    }

    public class AgendamentoEventoCalendario
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Color { get; set; } = "#3788d8";
    }

    public class AgendamentoServicoDisponivel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int DuracaoMin { get; set; }
    }

    public class AgendamentoHorarioValidado
    {
        public DateTime Inicio { get; set; }
        public DateTime Fim { get; set; }
    }

    public class AgendamentoPreparationService
    {
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly AgendamentoHandler _agendamentoHandler;
        private readonly FuncionarioHandler _funcionarioHandler;
        private readonly FuncionarioServicoHandler _funcionarioServicoHandler;

        public AgendamentoPreparationService(
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            AgendamentoHandler agendamentoHandler,
            FuncionarioHandler funcionarioHandler,
            FuncionarioServicoHandler funcionarioServicoHandler)
        {
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _agendamentoHandler = agendamentoHandler;
            _funcionarioHandler = funcionarioHandler;
            _funcionarioServicoHandler = funcionarioServicoHandler;
        }

        public virtual AgendamentoTelaContexto ObterContextoTela(int idSalao)
        {
            return new AgendamentoTelaContexto
            {
                Servicos = _servicoHandler.ListarPorSalao(idSalao),
                Clientes = _pessoaHandler.ListarPorSalao(idSalao)
            };
        }

        public virtual List<AgendamentoEventoCalendario> ListarEventos(int idSalao, DateTime start, DateTime end)
        {
            var servicos = _servicoHandler.ListarPorSalao(idSalao);
            var dictServicos = servicos.ToDictionary(s => s.IdServico);

            var clientes = _pessoaHandler.ListarPorSalao(idSalao);
            var dictPessoas = clientes.ToDictionary(p => p.IdPessoa, p => p.Nome);

            var dataInicio = start.AddDays(-1);
            var dataFim = end.AddDays(1);
            var agendamentos = _agendamentoHandler.ListarPorIntervalo(idSalao, dataInicio, dataFim);

            return agendamentos
                .Where(a => AgendamentoStatus.Normalizar(a.Status) != AgendamentoStatus.Cancelado)
                .Select(a =>
                {
                    var servico = dictServicos.TryGetValue(a.IdServico, out var itemServico) ? itemServico : null;
                    var nomeCliente = dictPessoas.TryGetValue(a.IdPessoa, out var nome) ? nome : "Cliente Removido";
                    var statusExibicao = AgendamentoStatus.Normalizar(a.Status);
                    var primeiroNome = nomeCliente.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nomeCliente;
                    var titulo = $"{primeiroNome} - {servico?.Nome ?? "Servico Removido"}";
                    if (statusExibicao == AgendamentoStatus.Pago)
                    {
                        titulo = "? " + titulo;
                    }

                    return new AgendamentoEventoCalendario
                    {
                        Id = a.IdAgendamento.ToString(),
                        Title = titulo,
                        Start = a.DataHora,
                        End = a.DataHora.Add(servico?.Duracao ?? TimeSpan.FromMinutes(30)),
                        Color = AgendamentoStatus.ObterCor(statusExibicao)
                    };
                })
                .ToList();
        }

        public virtual List<AgendamentoServicoDisponivel> ListarServicosDisponiveis(int idSalao, DateTime start, int? idAgendamentoIgnorar = null)
        {
            return _servicoHandler.ListarPorSalao(idSalao)
                .Where(s => ObterFuncionarioDisponivelId(s.IdServico, start, idSalao, idAgendamentoIgnorar) > 0)
                .Select(s => new AgendamentoServicoDisponivel
                {
                    Id = s.IdServico,
                    Nome = s.Nome,
                    DuracaoMin = (int)s.Duracao.TotalMinutes
                })
                .ToList();
        }

        public virtual AgendamentoHorarioValidado ValidarHorarioServico(int idServico, DateTime inicio, DateTime? fimInformado, int idSalao)
        {
            var servico = _servicoHandler.ObterPorId(idServico);
            if (servico == null || servico.IdSalao != idSalao)
            {
                throw new InvalidOperationException("Servi\u00E7o inv\u00E1lido para este sal\u00E3o.");
            }

            var fimEsperado = inicio.Add(servico.Duracao);
            if (fimInformado.HasValue)
            {
                var fim = fimInformado.Value.Kind == DateTimeKind.Utc
                    ? fimInformado.Value.ToLocalTime()
                    : fimInformado.Value;

                if (fim <= inicio)
                {
                    throw new InvalidOperationException("O hor\u00E1rio final precisa ser maior que o hor\u00E1rio inicial.");
                }

                if (fim != fimEsperado)
                {
                    throw new InvalidOperationException("O hor\u00E1rio final n\u00E3o confere com a dura\u00E7\u00E3o configurada para o servi\u00E7o.");
                }
            }

            return new AgendamentoHorarioValidado
            {
                Inicio = inicio,
                Fim = fimEsperado
            };
        }

        public virtual int ObterFuncionarioDisponivelId(int idServico, DateTime start, int idSalao, int? idAgendamentoIgnorar = null)
        {
            var servico = _servicoHandler.ObterPorId(idServico);
            if (servico == null || servico.IdSalao != idSalao)
            {
                return 0;
            }

            var idsFuncionarios = _funcionarioServicoHandler.ListarFuncionariosDoServico(idServico);
            var fim = start.Add(servico.Duracao);

            foreach (var idFuncionario in idsFuncionarios)
            {
                var funcionario = _funcionarioHandler.ObterPorId(idFuncionario);
                if (funcionario == null || funcionario.IdSalao != idSalao)
                {
                    continue;
                }

                var disponibilidade = ObterDisponibilidadeDoDia(funcionario, start.DayOfWeek);
                if (!disponibilidade.Trabalha || !disponibilidade.Inicio.HasValue || !disponibilidade.Fim.HasValue)
                {
                    continue;
                }

                if (start.TimeOfDay < disponibilidade.Inicio.Value || fim.TimeOfDay > disponibilidade.Fim.Value)
                {
                    continue;
                }

                if (_agendamentoHandler.VerificarDisponibilidade(funcionario.IdFuncionario, start, fim, idAgendamentoIgnorar))
                {
                    return funcionario.IdFuncionario;
                }
            }

            return 0;
        }

        private static (bool Trabalha, TimeSpan? Inicio, TimeSpan? Fim) ObterDisponibilidadeDoDia(Funcionario funcionario, DayOfWeek diaDaSemana)
        {
            return diaDaSemana switch
            {
                DayOfWeek.Monday => (funcionario.seg, funcionario.seg_ini, funcionario.seg_fim),
                DayOfWeek.Tuesday => (funcionario.ter, funcionario.ter_ini, funcionario.ter_fim),
                DayOfWeek.Wednesday => (funcionario.qua, funcionario.qua_ini, funcionario.qua_fim),
                DayOfWeek.Thursday => (funcionario.qui, funcionario.qui_ini, funcionario.qui_fim),
                DayOfWeek.Friday => (funcionario.sex, funcionario.sex_ini, funcionario.sex_fim),
                DayOfWeek.Saturday => (funcionario.sab, funcionario.sab_ini, funcionario.sab_fim),
                DayOfWeek.Sunday => (funcionario.dom, funcionario.dom_ini, funcionario.dom_fim),
                _ => (false, null, null)
            };
        }
    }
}
