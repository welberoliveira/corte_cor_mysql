using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Transactions;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class AgendamentosModel : PageModel
    {
        private readonly AgendamentoHandler _agendamentoHandler;
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly MeioPagamentoHandler _meioPagamentoHandler;
        private readonly PagamentoHandler _pagamentoHandler;
        private readonly MercadoPagoService _mpService;
        private readonly AgendamentoPreparationService _agendamentoPreparationService;
        private readonly AgendamentoFiscalPreparationService _agendamentoFiscalPreparationService;

        public AgendamentosModel(
            AgendamentoHandler agendamentoHandler,
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            MeioPagamentoHandler meioPagamentoHandler,
            PagamentoHandler pagamentoHandler,
            MercadoPagoService mpService,
            AgendamentoPreparationService agendamentoPreparationService,
            AgendamentoFiscalPreparationService agendamentoFiscalPreparationService)
        {
            _agendamentoHandler = agendamentoHandler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _meioPagamentoHandler = meioPagamentoHandler;
            _pagamentoHandler = pagamentoHandler;
            _mpService = mpService;
            _agendamentoPreparationService = agendamentoPreparationService;
            _agendamentoFiscalPreparationService = agendamentoFiscalPreparationService;
        }

        public List<Servico> Servicos { get; set; } = new();
        public List<Pessoa> Clientes { get; set; } = new();

        public void OnGet()
        {
            var contexto = _agendamentoPreparationService.ObterContextoTela(ObterIdSalao());
            Servicos = contexto.Servicos;
            Clientes = contexto.Clientes;
        }

        public IActionResult OnGetEvents(DateTime start, DateTime end)
        {
            if (start.Kind == DateTimeKind.Utc)
            {
                start = start.ToLocalTime();
            }

            if (end.Kind == DateTimeKind.Utc)
            {
                end = end.ToLocalTime();
            }

            return new JsonResult(_agendamentoPreparationService.ListarEventos(ObterIdSalao(), start, end));
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
            try
            {
                if (req == null)
                {
                    return BadRequest(new ErrorResponse { Message = "RequisiÃ§Ã£o invÃ¡lida." });
                }

                if (string.IsNullOrWhiteSpace(req.Start))
                {
                    return BadRequest(new ErrorResponse { Message = "InÃ­cio Ã© obrigatÃ³rio." });
                }

                if (!DateTime.TryParse(req.Start, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
                {
                    return BadRequest(new ErrorResponse { Message = "Formato de data de inÃ­cio invÃ¡lido." });
                }

                if (start.Kind == DateTimeKind.Utc)
                {
                    start = start.ToLocalTime();
                }

                if (req.IdServico <= 0)
                {
                    return BadRequest(new ErrorResponse { Message = "ServiÃ§o Ã© obrigatÃ³rio." });
                }

                if (req.IdPessoa <= 0)
                {
                    return BadRequest(new ErrorResponse { Message = "Cliente Ã© obrigatÃ³rio." });
                }

                var idSalao = ObterIdSalao();
                var servico = _servicoHandler.ObterPorId(req.IdServico);
                if (servico == null || servico.IdSalao != idSalao)
                {
                    return BadRequest(new ErrorResponse { Message = "Serviço inválido para esta empresa." });
                }

                var pessoa = _pessoaHandler.ObterPorId(req.IdPessoa);
                if (pessoa == null || pessoa.IdSalao != idSalao)
                {
                    return BadRequest(new ErrorResponse { Message = "Cliente inválido para esta empresa." });
                }

                DateTime? end = null;
                if (!string.IsNullOrWhiteSpace(req.End))
                {
                    if (!DateTime.TryParse(req.End, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedEnd))
                    {
                        return BadRequest(new ErrorResponse { Message = "Formato de data final invÃ¡lido." });
                    }

                    end = parsedEnd.Kind == DateTimeKind.Utc ? parsedEnd.ToLocalTime() : parsedEnd;
                }

                var horario = _agendamentoPreparationService.ValidarHorarioServico(req.IdServico, start, end, idSalao);
                var idFuncionarioSelecionado = _agendamentoPreparationService.ObterFuncionarioDisponivelId(req.IdServico, horario.Inicio, idSalao);
                if (idFuncionarioSelecionado == 0)
                {
                    return BadRequest(new ErrorResponse { Message = "NÃ£o hÃ¡ profissionais disponÃ­veis para este serviÃ§o no horÃ¡rio selecionado." });
                }

                var novoId = _agendamentoHandler.CadastrarAgendamento(new Agendamento
                {
                    DataHora = horario.Inicio,
                    IdServico = req.IdServico,
                    IdPessoa = req.IdPessoa,
                    IdFuncionario = idFuncionarioSelecionado,
                    Status = AgendamentoStatus.Agendado
                });

                return new JsonResult(new
                {
                    id = novoId,
                    servicoNome = servico.Nome,
                    servicoCor = AgendamentoStatus.ObterCor(AgendamentoStatus.Agendado)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Erro ao salvar agendamento: " + ex.Message,
                    Detail = ex.StackTrace
                });
            }
        }

        public async Task<IActionResult> OnGetDetails(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ErrorResponse { Message = "ID invÃ¡lido." });
            }

            var agendamento = _agendamentoHandler.ObterPorId(id);
            if (agendamento == null)
            {
                return NotFound();
            }

            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            var statusNormalizado = AgendamentoStatus.Normalizar(agendamento.Status);
            var situacaoFiscal = await _agendamentoFiscalPreparationService.ObterSituacaoFiscalAsync(
                ObterIdSalao(),
                agendamento.IdAgendamento,
                statusNormalizado);

            return new JsonResult(new
            {
                id = agendamento.IdAgendamento,
                idPessoa = agendamento.IdPessoa,
                idServico = agendamento.IdServico,
                servicoNome = servico?.Nome ?? "ServiÃ§o nÃ£o encontrado",
                start = agendamento.DataHora,
                status = statusNormalizado,
                canEdit = AgendamentoStatus.PodeAlterar(statusNormalizado),
                canDelete = AgendamentoStatus.PodeExcluir(statusNormalizado),
                canPay = AgendamentoStatus.PodePagar(statusNormalizado),
                fiscal = MontarFiscalState(situacaoFiscal)
            });
        }

        public class UpdateRequest
        {
            public int Id { get; set; }
            public int IdPessoa { get; set; }
            public int IdServico { get; set; }
            public string? Status { get; set; }
            public string? Start { get; set; }
        }

        public class AgendamentoFiscalUiState
        {
            public bool PossuiNota { get; set; }
            public bool PossuiNotaAtiva { get; set; }
            public bool PodeEmitir { get; set; }
            public bool PodeAbrirNota { get; set; }
            public string StatusFiscal { get; set; } = "Sem nota";
            public string ClasseStatusFiscal { get; set; } = "bg-secondary";
            public string Mensagem { get; set; } = string.Empty;
            public Guid? IdNotaFiscal { get; set; }
            public int? NumeroNota { get; set; }
            public int? SerieNota { get; set; }
            public string? TipoNota { get; set; }
            public string? UrlLista { get; set; }
        }

        public IActionResult OnPostUpdate([FromBody] UpdateRequest req)
        {
            try
            {
                if (req == null || req.Id <= 0)
                {
                    return BadRequest(new ErrorResponse { Message = "RequisiÃ§Ã£o invÃ¡lida." });
                }

                var idSalao = ObterIdSalao();
                var agendamento = _agendamentoHandler.ObterPorId(req.Id);
                if (agendamento == null)
                {
                    return NotFound("Agendamento nÃ£o encontrado.");
                }

                if (!AgendamentoStatus.PodeAlterar(agendamento.Status))
                {
                    return BadRequest(new ErrorResponse { Message = "Agendamentos pagos ou cancelados nÃ£o podem ser alterados." });
                }

                var servico = _servicoHandler.ObterPorId(req.IdServico);
                if (servico == null || servico.IdSalao != idSalao)
                {
                    return BadRequest(new ErrorResponse { Message = "ServiÃ§o invÃ¡lido." });
                }

                var pessoa = _pessoaHandler.ObterPorId(req.IdPessoa);
                if (pessoa == null || pessoa.IdSalao != idSalao)
                {
                    return BadRequest(new ErrorResponse { Message = "Cliente invÃ¡lido." });
                }

                var dataHora = agendamento.DataHora;
                if (!string.IsNullOrWhiteSpace(req.Start))
                {
                    if (!DateTime.TryParse(req.Start, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        return BadRequest(new ErrorResponse { Message = "Formato de data invÃ¡lido." });
                    }

                    dataHora = parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime() : parsed;
                }

                var horario = _agendamentoPreparationService.ValidarHorarioServico(req.IdServico, dataHora, null, idSalao);
                var idFuncionarioSelecionado = _agendamentoPreparationService.ObterFuncionarioDisponivelId(req.IdServico, horario.Inicio, idSalao, agendamento.IdAgendamento);
                if (idFuncionarioSelecionado == 0)
                {
                    return BadRequest(new ErrorResponse { Message = "NÃ£o hÃ¡ profissionais disponÃ­veis para este serviÃ§o no horÃ¡rio selecionado (ou hÃ¡ conflito de agenda)." });
                }

                agendamento.IdPessoa = req.IdPessoa;
                agendamento.IdServico = req.IdServico;
                agendamento.IdFuncionario = idFuncionarioSelecionado;
                agendamento.DataHora = horario.Inicio;
                agendamento.Status = string.IsNullOrWhiteSpace(req.Status)
                    ? AgendamentoStatus.Normalizar(agendamento.Status)
                    : AgendamentoStatus.Normalizar(req.Status);

                _agendamentoHandler.Atualizar(agendamento, idSalao);

                if (AgendamentoStatus.Normalizar(agendamento.Status) == AgendamentoStatus.Pago)
                {
                    var existingPayment = _pagamentoHandler.ObterPorIdAgendamento(agendamento.IdAgendamento);
                    if (existingPayment == null || existingPayment.Status != AgendamentoStatus.Pago)
                    {
                        _pagamentoHandler.CadastrarPagamento(new Pagamento
                        {
                            IdPagamento = Guid.NewGuid(),
                            IdAgendamento = agendamento.IdAgendamento,
                            Ativo = true,
                            Status = AgendamentoStatus.Pago,
                            Valor = servico.Preco,
                            Moeda = "BRL",
                            Descricao = $"Pagamento do agendamento {agendamento.IdAgendamento} (Marcado Manualmente)",
                            Data = DateTime.Now,
                            PagoEm = DateTime.Now,
                            CriadoEm = DateTime.UtcNow,
                            Tipo = "Manual"
                        });
                    }
                }

                var primeiroNome = pessoa.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? pessoa.Nome;
                return new JsonResult(new
                {
                    id = agendamento.IdAgendamento,
                    title = $"{primeiroNome} - {servico.Nome}",
                    color = AgendamentoStatus.ObterCor(agendamento.Status),
                    start = agendamento.DataHora,
                    end = agendamento.DataHora.Add(servico.Duracao)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Erro ao atualizar agendamento: " + ex.Message,
                    Detail = ex.StackTrace
                });
            }
        }

        public class PagarRequest
        {
            public int IdAgendamento { get; set; }
        }

        public async Task<IActionResult> OnPostPagar([FromBody] PagarRequest req)
        {
            if (req == null || req.IdAgendamento <= 0)
            {
                return BadRequest(new ErrorResponse { Message = "RequisiÃ§Ã£o invÃ¡lida." });
            }

            var idSalao = ObterIdSalao();
            var agendamento = _agendamentoHandler.ObterPorId(req.IdAgendamento);
            if (agendamento == null)
            {
                return NotFound("Agendamento nÃ£o encontrado");
            }

            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            if (servico == null || servico.IdSalao != idSalao)
            {
                return BadRequest(new ErrorResponse { Message = "Acesso negado" });
            }

            if (!AgendamentoStatus.PodePagar(agendamento.Status))
            {
                return BadRequest(new ErrorResponse { Message = "Este agendamento jÃ¡ foi pago ou nÃ£o permite pagamento." });
            }

            var pessoa = _pessoaHandler.ObterPorId(agendamento.IdPessoa);
            if (pessoa == null)
            {
                return BadRequest(new ErrorResponse { Message = "Cliente nÃ£o encontrado" });
            }

            var meios = _meioPagamentoHandler.ListarPorSalao(idSalao, somenteAtivos: true);
            var mpConfig = meios.FirstOrDefault(m =>
                !string.IsNullOrWhiteSpace(m.Gateway) &&
                m.Gateway.Replace(" ", string.Empty).Equals("MercadoPago", StringComparison.OrdinalIgnoreCase));

            if (mpConfig == null)
            {
                return BadRequest(new ErrorResponse { Message = "Meio de pagamento Mercado Pago não configurado para esta empresa." });
            }

            var isProduction = mpConfig.MpProduction;
            var accessToken = isProduction ? mpConfig.MpAccessTokenProd : mpConfig.MpAccessTokenSandbox;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = isProduction
                        ? "Token de ProduÃ§Ã£o do Mercado Pago nÃ£o configurado."
                        : "Token de Sandbox do Mercado Pago nÃ£o configurado."
                });
            }

            var idPagamento = Guid.NewGuid();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var (pref, error) = await _mpService.CreatePreferenceAsync(
                accessToken,
                idPagamento,
                $"ServiÃ§o {servico.Nome} - Corte & Cor",
                servico.Preco,
                pessoa.Email ?? "cliente@cortecor.com",
                baseUrl);

            if (pref == null)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Erro ao gerar preferÃªncia de pagamento no Mercado Pago",
                    Detail = error
                });
            }

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (AgendamentoStatus.Normalizar(agendamento.Status) != AgendamentoStatus.Pago)
            {
                _agendamentoHandler.AtualizarStatus(agendamento.IdAgendamento, AgendamentoStatus.Pendente, idSalao);
            }

            _pagamentoHandler.CadastrarPagamento(new Pagamento
            {
                IdPagamento = idPagamento,
                IdAgendamento = agendamento.IdAgendamento,
                Ativo = true,
                Status = AgendamentoStatus.Pendente,
                Valor = servico.Preco,
                Moeda = "BRL",
                Descricao = $"Pagamento do agendamento {agendamento.IdAgendamento}",
                MercadoPagoPreferenceId = pref.Id,
                CheckoutUrl = pref.InitPoint,
                CriadoEm = DateTime.UtcNow,
                Tipo = "MercadoPago"
            });

            scope.Complete();

            return new JsonResult(new { checkoutUrl = pref.InitPoint });
        }

        public class EmitirNotaRequest
        {
            public int IdAgendamento { get; set; }
        }

        public async Task<IActionResult> OnPostEmitirNota([FromBody] EmitirNotaRequest req)
        {
            if (req == null || req.IdAgendamento <= 0)
            {
                return BadRequest(new ErrorResponse { Message = "RequisiÃ§Ã£o invÃ¡lida." });
            }

            try
            {
                var resultado = await _agendamentoFiscalPreparationService.EmitirNotaServicoAsync(
                    ObterIdSalao(),
                    req.IdAgendamento,
                    ObterUsuarioOperador(),
                    "Manual");

                return new JsonResult(new
                {
                    success = resultado.NotaFiscal?.Status == NotaFiscalStatus.Autorizada,
                    message = resultado.Mensagem,
                    nf_id = resultado.NotaFiscal?.IdNotaFiscal,
                    fiscal = MontarFiscalState(await _agendamentoFiscalPreparationService.ObterSituacaoFiscalAsync(ObterIdSalao(), req.IdAgendamento)),
                    redirectUrl = ObterUrlListaPorAgendamento(req.IdAgendamento)
                });
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message,
                    fiscal = MontarFiscalState(await _agendamentoFiscalPreparationService.ObterSituacaoFiscalAsync(ObterIdSalao(), req.IdAgendamento)),
                    redirectUrl = ObterUrlListaPorAgendamento(req.IdAgendamento)
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Erro ao processar emissÃ£o NF: " + ex.Message });
            }
        }

        public IActionResult OnPostDelete(int id)
        {
            var agendamento = _agendamentoHandler.ObterPorId(id);
            if (agendamento == null)
            {
                return NotFound();
            }

            var idSalao = ObterIdSalao();
            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            if (servico == null || servico.IdSalao != idSalao)
            {
                return BadRequest(new ErrorResponse { Message = "Acesso negado" });
            }

            if (!AgendamentoStatus.PodeExcluir(agendamento.Status))
            {
                return BadRequest(new ErrorResponse { Message = "Agendamentos pagos ou cancelados nÃ£o podem ser excluÃ­dos." });
            }

            _agendamentoHandler.Excluir(id);
            return new JsonResult(new { ok = true });
        }

        public IActionResult OnGetAvailableServices(DateTime start)
        {
            if (start.Kind == DateTimeKind.Utc)
            {
                start = start.ToLocalTime();
            }

            return new JsonResult(_agendamentoPreparationService.ListarServicosDisponiveis(ObterIdSalao(), start));
        }

        private int ObterIdSalao() =>
            int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

        private string? ObterUsuarioOperador() =>
            User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value;

        private AgendamentoFiscalUiState MontarFiscalState(AgendamentoSituacaoFiscalResult situacao)
        {
            var urlLista = ObterUrlNotaFiscal(situacao);

            return new AgendamentoFiscalUiState
            {
                PossuiNota = situacao.PossuiNota,
                PossuiNotaAtiva = situacao.PossuiNotaAtiva,
                PodeEmitir = situacao.PodeEmitir,
                PodeAbrirNota = situacao.PodeAbrirNota,
                StatusFiscal = situacao.StatusFiscal,
                ClasseStatusFiscal = situacao.ClasseStatusFiscal,
                Mensagem = situacao.Mensagem,
                IdNotaFiscal = situacao.IdNotaFiscal,
                NumeroNota = situacao.NumeroNota,
                SerieNota = situacao.SerieNota,
                TipoNota = situacao.TipoNota,
                UrlLista = urlLista
            };
        }

        private string ObterUrlNotaFiscal(AgendamentoSituacaoFiscalResult situacao)
        {
            var rota = $"/NotaFiscalLista?idAgendamento={situacao.IdAgendamento}";
            if (situacao.IdNotaFiscal.HasValue)
            {
                rota += $"&idNotaFiscal={situacao.IdNotaFiscal}";
            }

            if (Url != null)
            {
                return Url.Page("/NotaFiscalLista", new
                {
                    idAgendamento = situacao.IdAgendamento,
                    idNotaFiscal = situacao.IdNotaFiscal
                }) ?? rota;
            }

            var pathBase = HttpContext?.Request?.PathBase.Value ?? string.Empty;
            return $"{pathBase}{rota}";
        }

        private string ObterUrlListaPorAgendamento(int idAgendamento)
        {
            if (Url != null)
            {
                return Url.Page("/NotaFiscalLista", new { idAgendamento }) ?? $"/NotaFiscalLista?idAgendamento={idAgendamento}";
            }

            var pathBase = HttpContext?.Request?.PathBase.Value ?? string.Empty;
            return $"{pathBase}/NotaFiscalLista?idAgendamento={idAgendamento}";
        }
    }
}


