using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CorteCor.Models;
using System.Transactions;

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
            
            // Ajuste de datas para garantir cobertura (start/end do FullCalendar são exatos)
            var dataInicio = start.AddDays(-1);
            var dataFim = end.AddDays(1);
            
            // Agora usa o filtro de data no banco via ListarPorIntervalo
            var allAgendamentos = agendamentoHandler.ListarPorIntervalo(idSalao, dataInicio, dataFim);

            var items = allAgendamentos
                .Where(a => a.Status != "Cancelado")
                .Select(a => {
                   var servico = dictServicos.ContainsKey(a.IdServico) ? dictServicos[a.IdServico] : null;
                   var nomeCliente = dictPessoas.ContainsKey(a.IdPessoa) ? dictPessoas[a.IdPessoa] : "Cliente Removido";
                   
                   var duracao = servico?.Duracao ?? TimeSpan.FromMinutes(30);
                   var cor = GetCorPorStatus(a.Status);

                    var primeiroNome = nomeCliente.Split(' ')[0];
                    var titulo = $"{primeiroNome} - {servico?.Nome ?? "Serviço Removido"}";
                    if (a.Status == "Pago") titulo = "✅ " + titulo;

                    return new
                    {
                        id = a.IdAgendamento.ToString(),
                        title = titulo,
                        start = a.DataHora,
                        end = a.DataHora.Add(duracao),
                        color = cor
                    };
                })
                .ToList();

            return new JsonResult(items);
        }

        private string GetCorPorStatus(string status)
        {
            return status switch
            {
                "Agendado" => "#3788d8", // Azul Claro
                "Pendente" => "#ffc107", // Amarelo Claro (Bootstrap warning)
                "Pago" => "#28a745",     // Verde Claro (Bootstrap success)
                "Cancelado" => "#dc3545", // Vermelho (Bootstrap danger)
                _ => "#3788d8"           // Default Azul
            };
        }

        public class CreateRequest
        {
            public string? Start { get; set; }
            public string? End { get; set; }
            public int IdPessoa { get; set; }
            public int IdServico { get; set; }
        }


        public IActionResult OnPostCreate([FromBody] CreateRequest req)
        {
            if (req == null) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

            if (string.IsNullOrWhiteSpace(req.Start)) return BadRequest(new ErrorResponse { Message = "Início é obrigatório." });
            
            if (!DateTime.TryParse(req.Start, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
                return BadRequest(new ErrorResponse { Message = "Formato de data de início inválido." });

            if (start.Kind == DateTimeKind.Utc)
            {
                start = start.ToLocalTime();
            }

            if (req.IdServico <= 0) return BadRequest(new ErrorResponse { Message = "Serviço é obrigatório." });
            if (req.IdPessoa <= 0) return BadRequest(new ErrorResponse { Message = "Cliente é obrigatório." });

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Validações
            var servicoHandler = new ServicoHandler();
            var servico = servicoHandler.ObterPorId(req.IdServico);
            if (servico == null) return BadRequest(new ErrorResponse { Message = "Serviço não encontrado" });
            if (servico.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Serviço inválido para este salão" });

            var pessoaHandler = new PessoaHandler();
            var pessoa = pessoaHandler.ObterPorId(req.IdPessoa);
            if (pessoa == null) return BadRequest(new ErrorResponse { Message = "Cliente não encontrado" });
            if (pessoa.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Cliente inválido para este salão" });


            var agendamentoHandler = new AgendamentoHandler();
            var fsHandler = new FuncionarioServicoHandler();
            var funcionarioHandler = new FuncionarioHandler();

            int idFuncionarioSelecionado = GetAvailableFuncionarioId(req.IdServico, start, idSalao);

            if (idFuncionarioSelecionado == 0)
                return BadRequest(new ErrorResponse { Message = "Não há profissionais disponíveis para este serviço no horário selecionado." });

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
                servicoCor = GetCorPorStatus("Agendado")
            });
        }

        public IActionResult OnGetDetails(int id)
        {
            if (id <= 0) return BadRequest(new ErrorResponse { Message = "ID inválido." });

            var agendamentoHandler = new AgendamentoHandler();
            var a = agendamentoHandler.ObterPorId(id);
            if (a == null) return NotFound();

            return new JsonResult(new
            {
                id = a.IdAgendamento,
                idPessoa = a.IdPessoa,
                idServico = a.IdServico,
                start = a.DataHora,
                status = a.Status
            });
        }

        public class UpdateRequest
        {
            public int Id { get; set; }
            public int IdPessoa { get; set; }
            public int IdServico { get; set; }
            public string? Status { get; set; }
            public string? Start { get; set; } // NEW: support rescheduling
        }

        public IActionResult OnPostUpdate([FromBody] UpdateRequest req)
        {
            if (req == null || req.Id <= 0) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var agendamentoHandler = new AgendamentoHandler();
            var agendamento = agendamentoHandler.ObterPorId(req.Id);
            if (agendamento == null) return NotFound("Agendamento não encontrado.");

            if (agendamento.Status == "Pago")
                return BadRequest(new ErrorResponse { Message = "Agendamentos pagos não podem ser alterados." });

            // Validações
            var servicoHandler = new ServicoHandler();
            var servico = servicoHandler.ObterPorId(req.IdServico);
            if (servico == null || servico.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Serviço inválido" });

            var pessoaHandler = new PessoaHandler();
            var pessoa = pessoaHandler.ObterPorId(req.IdPessoa);
            if (pessoa == null || pessoa.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Cliente inválido" });

            // Determina a data/hora (mantém a original se não enviada)
            DateTime dataHora = agendamento.DataHora;
            if (!string.IsNullOrWhiteSpace(req.Start))
            {
                if (DateTime.TryParse(req.Start, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                {
                    dataHora = parsed;
                    if (dataHora.Kind == DateTimeKind.Utc) dataHora = dataHora.ToLocalTime();
                }
                else
                {
                    return BadRequest(new ErrorResponse { Message = "Formato de data inválido." });
                }
            }

            // Re-calcula funcionário se necessário (passando o próprio ID para ignorar colisão consigo mesmo)
            int idFuncionarioSelecionado = GetAvailableFuncionarioId(req.IdServico, dataHora, idSalao, agendamento.IdAgendamento);

            if (idFuncionarioSelecionado == 0)
                return BadRequest(new ErrorResponse { Message = "Não há profissionais disponíveis para este serviço no horário selecionado (ou há conflito de agenda)." });

            agendamento.IdPessoa = req.IdPessoa;
            agendamento.IdServico = req.IdServico;
            agendamento.IdFuncionario = idFuncionarioSelecionado;
            agendamento.DataHora = dataHora; // Atualiza data
            if (!string.IsNullOrEmpty(req.Status))
                agendamento.Status = req.Status;

            agendamentoHandler.Atualizar(agendamento);

            var primeiroNome = pessoa.Nome.Split(' ')[0];
            return new JsonResult(new
            {
                id = agendamento.IdAgendamento,
                title = $"{primeiroNome} - {servico.Nome}",
                color = GetCorPorStatus(agendamento.Status),
                start = agendamento.DataHora, // Return new start
                end = agendamento.DataHora.Add(servico.Duracao)
            });
        }


        public class PagarRequest
        {
            public int IdAgendamento { get; set; }
        }

        public async Task<IActionResult> OnPostPagar([FromBody] PagarRequest req)
        {
            if (req == null || req.IdAgendamento <= 0) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var agendamentoHandler = new AgendamentoHandler();
            var a = agendamentoHandler.ObterPorId(req.IdAgendamento);
            if (a == null) return NotFound("Agendamento não encontrado");

            // Valida se pertence ao salão
            var servicoHandler = new ServicoHandler();
            var s = servicoHandler.ObterPorId(a.IdServico);
            if (s == null || s.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Acesso negado" });

            if (a.Status == "Pago") return BadRequest(new ErrorResponse { Message = "Este agendamento já foi pago" });

            var pessoaHandler = new PessoaHandler();
            var p = pessoaHandler.ObterPorId(a.IdPessoa);
            if (p == null) return BadRequest(new ErrorResponse { Message = "Cliente não encontrado" });

            // Gera novo ID para o Pagamento
            var idPagamento = Guid.NewGuid();

            // Gera Preferência no Mercado Pago usando o ID do Pagamento
            var mpService = new MercadoPagoService(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var (pref, error) = await mpService.CreatePreferenceAsync(
                idPagamento, 
                $"Serviço {s.Nome} - Corte & Cor", 
                s.Preco, 
                p.Email ?? "cliente@cortecor.com", 
                baseUrl);

            if (pref == null) 
            {
                return StatusCode(500, new ErrorResponse { 
                    Message = "Erro ao gerar preferência de pagamento no Mercado Pago",
                    Detail = error
                });
            }

            // Inicia transação para garantir consistência
            using (var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            {
                // Atualiza status do agendamento para Pendente (se não estiver pago)
                if (a.Status != "Pago")
                    agendamentoHandler.AtualizarStatus(a.IdAgendamento, "Pendente");

                // Registra o pagamento na tabela com o ID gerado
                var pagHandler = new PagamentoHandler();
                var novoPag = new Pagamento
                {
                    IdPagamento = idPagamento,
                    IdAgendamento = a.IdAgendamento,
                    Ativo = true,
                    Status = "Pendente",
                    Valor = s.Preco,
                    Moeda = "BRL",
                    Descricao = $"Pagamento do agendamento {a.IdAgendamento}",
                    MercadoPagoPreferenceId = pref.Id,
                    CheckoutUrl = pref.InitPoint, // Salva o link de pagamento
                    CriadoEm = DateTime.UtcNow,
                    Tipo = "MercadoPago"
                };

                pagHandler.CadastrarPagamento(novoPag);

                scope.Complete();
            }

            return new JsonResult(new { checkoutUrl = pref.InitPoint });
        }


        public IActionResult OnPostDelete(int id)
        {
            var agendamentoHandler = new AgendamentoHandler();
            var agendamento = agendamentoHandler.ObterPorId(id);
            if (agendamento == null) return NotFound();

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Validar se pertence ao salão
            var servicoHandler = new ServicoHandler();
            var s = servicoHandler.ObterPorId(agendamento.IdServico);
            if (s == null || s.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Acesso negado" });

            if (agendamento.Status == "Pago")
                return BadRequest(new ErrorResponse { Message = "Agendamentos pagos não podem ser excluídos." });

            agendamentoHandler.Excluir(id);
            
            return new JsonResult(new { ok = true });
        }

        public IActionResult OnGetAvailableServices(DateTime start)
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var servicoHandler = new ServicoHandler();
            var allServicos = servicoHandler.ListarPorSalao(idSalao);

            var available = allServicos
                .Where(s => GetAvailableFuncionarioId(s.IdServico, start, idSalao) > 0)
                .Select(s => new
                {
                    id = s.IdServico,
                    nome = s.Nome,
                    duracaoMin = (int)s.Duracao.TotalMinutes
                })
                .ToList();

            return new JsonResult(available);
        }

        private int GetAvailableFuncionarioId(int idServico, DateTime start, int idSalao, int? idAgendamentoIgnorar = null)
        {
            var servicoHandler = new ServicoHandler();
            var servico = servicoHandler.ObterPorId(idServico);
            if (servico == null || servico.IdSalao != idSalao) return 0;

            var fsHandler = new FuncionarioServicoHandler();
            var funcionarioHandler = new FuncionarioHandler();
            var agendamentoHandler = new AgendamentoHandler();

            var idsFuncionarios = fsHandler.ListarFuncionariosDoServico(idServico);
            
            // Calculamos o fim
            DateTime fim = start.Add(servico.Duracao);

            foreach (var idF in idsFuncionarios)
            {
                var f = funcionarioHandler.ObterPorId(idF);
                if (f == null || f.IdSalao != idSalao) continue;

                bool trabalha = false;
                TimeSpan? ini = null, f_fim = null;

                switch (start.DayOfWeek)
                {
                    case DayOfWeek.Monday: trabalha = f.seg; ini = f.seg_ini; f_fim = f.seg_fim; break;
                    case DayOfWeek.Tuesday: trabalha = f.ter; ini = f.ter_ini; f_fim = f.ter_fim; break;
                    case DayOfWeek.Wednesday: trabalha = f.qua; ini = f.qua_ini; f_fim = f.qua_fim; break;
                    case DayOfWeek.Thursday: trabalha = f.qui; ini = f.qui_ini; f_fim = f.qui_fim; break;
                    case DayOfWeek.Friday: trabalha = f.sex; ini = f.sex_ini; f_fim = f.sex_fim; break;
                    case DayOfWeek.Saturday: trabalha = f.sab; ini = f.sab_ini; f_fim = f.sab_fim; break;
                    case DayOfWeek.Sunday: trabalha = f.dom; ini = f.dom_ini; f_fim = f.dom_fim; break;
                }

                if (trabalha && ini.HasValue && f_fim.HasValue)
                {
                    var horaInicio = start.TimeOfDay;
                    var horaFim = fim.TimeOfDay;

                    // Verifica jornada de trabalho (horário)
                    // Note: horaFim > f_fim.Value pode ser problema se passar da meia noite, mas assumindo mesmo dia:
                    if (horaInicio >= ini.Value && horaFim <= f_fim.Value)
                    {
                        // Verifica colisão com outros agendamentos
                        if (agendamentoHandler.VerificarDisponibilidade(f.IdFuncionario, start, fim, idAgendamentoIgnorar))
                        {
                            return f.IdFuncionario;
                        }
                    }
                }
            }

            return 0;
        }
    }
}

