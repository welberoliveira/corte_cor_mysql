using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    public class Agendamentos2Model : PageModel
    {
        public List<Servico> Servicos { get; set; } = new();
        public List<Pessoa> Clientes { get; set; } = new();

        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var servicoHandler = new ServicoHandler();
            Servicos = servicoHandler.ListarPorSalao(idSalao);

            var pessoaHandler = new PessoaHandler();
            Clientes = pessoaHandler.ListarPorSalao(idSalao);
        }

        // FullCalendar espera: [{ id, title, start, end, color }]
        public IActionResult OnGetEvents(DateTime start, DateTime end)
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Carrega dados auxiliares para montar o objeto final
            var servicoHandler = new ServicoHandler();
            var servicos = servicoHandler.ListarPorSalao(idSalao);
            var dictServicos = servicos.ToDictionary(s => s.IdServico);

            var pessoaHandler = new PessoaHandler();
            var clientes = pessoaHandler.ListarPorSalao(idSalao);
            var dictPessoas = clientes.ToDictionary(p => p.IdPessoa, p => p.Nome);

            // Carrega agendamentos do banco
            var agendamentoHandler = new AgendamentoHandler();
            // Nota: ListarPorSalao filtra por IdSalao via serviço.
            // Para otimizar, seria ideal filtrar por data no banco, mas usaremos o filtro em memória por enquanto
            // pois o método ListarPorSalao retorna todos.
            var allAgendamentos = agendamentoHandler.ListarPorSalao(idSalao);

            var items = allAgendamentos
                .Where(a => a.DataHora >= start && a.DataHora < end && a.Status != "Cancelado")
                .Select(a => {
                   var servico = dictServicos.ContainsKey(a.IdServico) ? dictServicos[a.IdServico] : null;
                   var nomeCliente = dictPessoas.ContainsKey(a.IdPessoa) ? dictPessoas[a.IdPessoa] : "Cliente Removido";
                   
                   var duracao = servico?.Duracao ?? TimeSpan.FromMinutes(30);
                   var cor = servico?.Cor;
                   if (string.IsNullOrEmpty(cor)) cor = "#3788d8";

                   return new
                   {
                       id = a.IdAgendamento.ToString(),
                       title = $"{servico?.Nome ?? "Serviço Removido"} - {nomeCliente}",
                       start = a.DataHora,
                       end = a.DataHora.Add(duracao),
                       color = cor
                   };
                })
                .ToList();

            return new JsonResult(items);
        }

        public class CreateRequest
        {
            public string? Start { get; set; }
            public int IdPessoa { get; set; }
            public int IdServico { get; set; }
        }


        [ValidateAntiForgeryToken]
        public IActionResult OnPostCreate([FromBody] CreateRequest req)
        {
            if (req == null) return BadRequest();

            if (string.IsNullOrWhiteSpace(req.Start)) return BadRequest("Start inválido");
            if (!DateTime.TryParse(req.Start, out var start)) return BadRequest("Start inválido");

            if (req.IdServico <= 0) return BadRequest("Serviço é obrigatório");
            if (req.IdPessoa <= 0) return BadRequest("Cliente é obrigatório");

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Validações
            var servicoHandler = new ServicoHandler();
            var servico = servicoHandler.ObterPorId(req.IdServico);
            if (servico == null) return BadRequest("Serviço não encontrado");
            if (servico.IdSalao != idSalao) return BadRequest("Serviço inválido para este salão");

            var pessoaHandler = new PessoaHandler();
            var pessoa = pessoaHandler.ObterPorId(req.IdPessoa);
            if (pessoa == null) return BadRequest("Cliente não encontrado");
            if (pessoa.IdSalao != idSalao) return BadRequest("Cliente inválido para este salão");


            var agendamentoHandler = new AgendamentoHandler();
            var fsHandler = new FuncionarioServicoHandler();
            var funcionarioHandler = new FuncionarioHandler();

            var idsFuncionarios = fsHandler.ListarFuncionariosDoServico(req.IdServico);
            int idFuncionarioSelecionado = 0;

            foreach (var idF in idsFuncionarios)
            {
                var f = funcionarioHandler.ObterPorId(idF);
                if (f == null || f.IdSalao != idSalao) continue;

                // Verifica se trabalha no dia
                bool trabalha = false;
                TimeSpan? ini = null, fim = null;

                switch (start.DayOfWeek)
                {
                    case DayOfWeek.Monday: trabalha = f.seg; ini = f.seg_ini; fim = f.seg_fim; break;
                    case DayOfWeek.Tuesday: trabalha = f.ter; ini = f.ter_ini; fim = f.ter_fim; break;
                    case DayOfWeek.Wednesday: trabalha = f.qua; ini = f.qua_ini; fim = f.qua_fim; break;
                    case DayOfWeek.Thursday: trabalha = f.qui; ini = f.qui_ini; fim = f.qui_fim; break;
                    case DayOfWeek.Friday: trabalha = f.sex; ini = f.sex_ini; fim = f.sex_fim; break;
                    case DayOfWeek.Saturday: trabalha = f.sab; ini = f.sab_ini; fim = f.sab_fim; break;
                    case DayOfWeek.Sunday: trabalha = f.dom; ini = f.dom_ini; fim = f.dom_fim; break;
                }

                if (trabalha && ini.HasValue && fim.HasValue)
                {
                    var horaAgendamento = start.TimeOfDay;
                    // Se o atendimento cabe na janela (não verificamos sobreposição aqui por enquanto, apenas horário)
                    if (horaAgendamento >= ini.Value && horaAgendamento + servico.Duracao <= fim.Value)
                    {
                        idFuncionarioSelecionado = f.IdFuncionario;
                        break;
                    }
                }
            }

            if (idFuncionarioSelecionado == 0)
                return BadRequest("Não há profissionais disponíveis para este serviço no horário selecionado.");

            var novoAgendamento = new Agendamento
            {
                DataHora = start,
                IdServico = req.IdServico,
                IdPessoa = req.IdPessoa,
                IdFuncionario = idFuncionarioSelecionado,
                Status = "Agendado"
            };

            int novoId = agendamentoHandler.CadastrarAgendamento(novoAgendamento);

            return new JsonResult(new
            {
                id = novoId,
                servicoNome = servico.Nome,
                servicoCor = servico.Cor
            });
        }


        [ValidateAntiForgeryToken]
        public IActionResult OnPostDelete(int id)
        {
            if (id <= 0) return BadRequest();

            // Opcional: Validar se pertence ao salão antes de excluir
            // Mas para MVP/Demo, vamos direto ao delete
            var agendamentoHandler = new AgendamentoHandler();
            agendamentoHandler.Excluir(id);
            
            return new JsonResult(new { ok = true });
        }
    }
}
