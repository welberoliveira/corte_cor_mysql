using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Transactions;
using CorteCor.Services;

namespace CorteCor.Pages
{
    public class Agendamentos2Model : PageModel
    {
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly AgendamentoHandler _agendamentoHandler;
        private readonly FuncionarioHandler _funcionarioHandler;
        private readonly FuncionarioServicoHandler _fsHandler;
        private readonly MeioPagamentoHandler _meioPagamentoHandler;
        private readonly PagamentoHandler _pagamentoHandler;
        private readonly MercadoPagoService _mpService;
        private readonly IConfiguration _configuration;
        
        // Servicos Fiscais
        private readonly SalaoConfigFiscalHandler _salaoConfigFiscalHandler;
        private readonly NotaFiscalHandler _notaFiscalHandler;
        private readonly NFSeEmissorService _nfseEmissorService;
        private readonly NFCeEmissorService _nfceEmissorService;
        private readonly FiscalBuilderService _fiscalBuilderService;
        private readonly NotaFiscalLogHandler _notaFiscalLogHandler;

        public Agendamentos2Model(
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            AgendamentoHandler agendamentoHandler,
            FuncionarioHandler funcionarioHandler,
            FuncionarioServicoHandler fsHandler,
            MeioPagamentoHandler meioPagamentoHandler,
            PagamentoHandler pagamentoHandler,
            MercadoPagoService mpService,
            IConfiguration configuration,
            SalaoConfigFiscalHandler salaoConfigFiscalHandler,
            NotaFiscalHandler notaFiscalHandler,
            NFSeEmissorService nfseEmissorService,
            NFCeEmissorService nfceEmissorService,
            FiscalBuilderService fiscalBuilderService,
            NotaFiscalLogHandler notaFiscalLogHandler)
        {
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _agendamentoHandler = agendamentoHandler;
            _funcionarioHandler = funcionarioHandler;
            _fsHandler = fsHandler;
            _meioPagamentoHandler = meioPagamentoHandler;
            _pagamentoHandler = pagamentoHandler;
            _mpService = mpService;
            _configuration = configuration;
            _salaoConfigFiscalHandler = salaoConfigFiscalHandler;
            _notaFiscalHandler = notaFiscalHandler;
            _nfseEmissorService = nfseEmissorService;
            _nfceEmissorService = nfceEmissorService;
            _fiscalBuilderService = fiscalBuilderService;
            _notaFiscalLogHandler = notaFiscalLogHandler;
        }

        public List<Servico> Servicos { get; set; } = new();
        public List<Pessoa> Clientes { get; set; } = new();

        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            Servicos = _servicoHandler.ListarPorSalao(idSalao);
            Clientes = _pessoaHandler.ListarPorSalao(idSalao);
        }

        // FullCalendar espera: [{ id, title, start, end, color }]
        public IActionResult OnGetEvents(DateTime start, DateTime end)
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Carrega dados auxiliares para montar o objeto final
            var servicos = _servicoHandler.ListarPorSalao(idSalao);
            var dictServicos = servicos.ToDictionary(s => s.IdServico);

            var clientes = _pessoaHandler.ListarPorSalao(idSalao);
            var dictPessoas = clientes.ToDictionary(p => p.IdPessoa, p => p.Nome);

            // Ajuste de datas para garantir cobertura (start/end do FullCalendar são exatos)
            var dataInicio = start.AddDays(-1);
            var dataFim = end.AddDays(1);

            // Agora usa o filtro de data no banco via ListarPorIntervalo
            var allAgendamentos = _agendamentoHandler.ListarPorIntervalo(idSalao, dataInicio, dataFim);

            var items = allAgendamentos
                .Where(a => a.Status != "Cancelado")
                .Select(a =>
                {
                    var servico = dictServicos.ContainsKey(a.IdServico) ? dictServicos[a.IdServico] : null;
                    var nomeCliente = dictPessoas.ContainsKey(a.IdPessoa) ? dictPessoas[a.IdPessoa] : "Cliente Removido";

                    var duracao = servico?.Duracao ?? TimeSpan.FromMinutes(30);

                    // Map legacy "Confirmado" to "Pago" for display
                    var statusExibicao = a.Status;
                    if (statusExibicao == "Confirmado") statusExibicao = "Pago";

                    var cor = GetCorPorStatus(statusExibicao);

                    var primeiroNome = nomeCliente.Split(' ')[0];
                    var titulo = $"{primeiroNome} - {servico?.Nome ?? "Serviço Removido"}";
                    if (statusExibicao == "Pago") titulo = "? " + titulo;

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
            try
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
                var servico = _servicoHandler.ObterPorId(req.IdServico);
                if (servico == null) return BadRequest(new ErrorResponse { Message = "Serviço não encontrado" });
                if (servico.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Serviço inválido para este salão" });

                var pessoa = _pessoaHandler.ObterPorId(req.IdPessoa);
                if (pessoa == null) return BadRequest(new ErrorResponse { Message = "Cliente não encontrado" });
                if (pessoa.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Cliente inválido para este salão" });

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

                int novoId = _agendamentoHandler.CadastrarAgendamento(novoAgendamento);

                return new JsonResult(new
                {
                    id = novoId,
                    servicoNome = servico.Nome,
                    servicoCor = GetCorPorStatus("Agendado")
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

        public IActionResult OnGetDetails(int id)
        {
            if (id <= 0) return BadRequest(new ErrorResponse { Message = "ID inválido." });

            var a = _agendamentoHandler.ObterPorId(id);
            if (a == null) return NotFound();

            var status = a.Status;
            if (status == "Confirmado") status = "Pago";

            var servico = _servicoHandler.ObterPorId(a.IdServico);

            return new JsonResult(new
            {
                id = a.IdAgendamento,
                idPessoa = a.IdPessoa,
                idServico = a.IdServico,
                servicoNome = servico?.Nome ?? "Serviço não encontrado",
                start = a.DataHora,
                status = status
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
            try
            {
                if (req == null || req.Id <= 0) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                var agendamento = _agendamentoHandler.ObterPorId(req.Id);
                if (agendamento == null) return NotFound("Agendamento não encontrado.");

                if (agendamento.Status == "Pago")
                    return BadRequest(new ErrorResponse { Message = "Agendamentos pagos não podem ser alterados." });

                // Validações
                var servico = _servicoHandler.ObterPorId(req.IdServico);
                if (servico == null || servico.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Serviço inválido" });

                var pessoa = _pessoaHandler.ObterPorId(req.IdPessoa);
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

                _agendamentoHandler.Atualizar(agendamento, idSalao);

                // Geração de Pagamento Manual se status for "Pago" e data/hora atual é posterior à alteração de status
                if (agendamento.Status == "Pago")
                {
                    // Lógica para preencher o registro de Pagamento como Manual.
                    var existingPayment = _pagamentoHandler.ObterPorIdAgendamento(agendamento.IdAgendamento);
                    if (existingPayment == null || existingPayment.Status != "Pago")
                    {
                        var novoPag = new Pagamento
                        {
                            IdPagamento = Guid.NewGuid(),
                            IdAgendamento = agendamento.IdAgendamento,
                            Ativo = true,
                            Status = "Pago",
                            Valor = servico.Preco,
                            Moeda = "BRL",
                            Descricao = $"Pagamento do agendamento {agendamento.IdAgendamento} (Marcado Manualmente)",
                            Data = DateTime.Now,
                            PagoEm = DateTime.Now,
                            CriadoEm = DateTime.UtcNow,
                            Tipo = "Manual"
                        };
                        _pagamentoHandler.CadastrarPagamento(novoPag);
                    }
                }

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
            if (req == null || req.IdAgendamento <= 0) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var a = _agendamentoHandler.ObterPorId(req.IdAgendamento);
            if (a == null) return NotFound("Agendamento não encontrado");

            // Valida se pertence ao salão
            var s = _servicoHandler.ObterPorId(a.IdServico);
            if (s == null || s.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Acesso negado" });

            if (a.Status == "Pago") return BadRequest(new ErrorResponse { Message = "Este agendamento já foi pago" });

            var p = _pessoaHandler.ObterPorId(a.IdPessoa);
            if (p == null) return BadRequest(new ErrorResponse { Message = "Cliente não encontrado" });

            // 1. Identificar se é produção ou sandbox baseado no Config do Banco

            // 2. Buscar configurações de Meio de Pagamento do Salão
            // Using injected handler
            var meios = _meioPagamentoHandler.ListarPorSalao(idSalao, somenteAtivos: true);
            var mpConfig = meios.FirstOrDefault(m =>
                (m.Gateway != null && m.Gateway.Replace(" ", "").Equals("MercadoPago", StringComparison.OrdinalIgnoreCase)));

            if (mpConfig == null)
                return BadRequest(new ErrorResponse { Message = "Meio de pagamento Mercado Pago não configurado para este salão." });

            bool isProduction = mpConfig.MpProduction;

            string accessToken = isProduction ? mpConfig.MpAccessTokenProd : mpConfig.MpAccessTokenSandbox;

            if (string.IsNullOrWhiteSpace(accessToken))
                return BadRequest(new ErrorResponse
                {
                    Message = isProduction
                    ? "Token de Produção do Mercado Pago não configurado."
                    : "Token de Sandbox do Mercado Pago não configurado."
                });

            // Gera novo ID para o Pagamento
            var idPagamento = Guid.NewGuid();

            // Gera Preferência no Mercado Pago usando o ID do Pagamento e o Token do Banco
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var (pref, error) = await _mpService.CreatePreferenceAsync(
                accessToken,
                idPagamento,
                $"Serviço {s.Nome} - Corte & Cor",
                s.Preco,
                p.Email ?? "cliente@cortecor.com",
                baseUrl);

            if (pref == null)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "Erro ao gerar preferência de pagamento no Mercado Pago",
                    Detail = error
                });
            }

            // Inicia transação para garantir consistência
            using (var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            {
                // Atualiza status do agendamento para Pendente (se não estiver pago)
                if (a.Status != "Pago")
                    _agendamentoHandler.AtualizarStatus(a.IdAgendamento, "Pendente", idSalao);

                // Registra o pagamento na tabela com o ID gerado
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

                _pagamentoHandler.CadastrarPagamento(novoPag);

                scope.Complete();
            }

            return new JsonResult(new { checkoutUrl = pref.InitPoint });
        }

        public class EmitirNotaRequest
        {
            public int IdAgendamento { get; set; }
        }

        public async Task<IActionResult> OnPostEmitirNota([FromBody] EmitirNotaRequest req)
        {
            if (req == null || req.IdAgendamento <= 0) return BadRequest(new ErrorResponse { Message = "Requisição inválida." });

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var agendamento = _agendamentoHandler.ObterPorId(req.IdAgendamento);
            if (agendamento == null) return NotFound("Agendamento não encontrado.");

            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            if (servico == null || servico.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Serviço inválido" });

            var cliente = _pessoaHandler.ObterPorId(agendamento.IdPessoa);
            if (cliente == null || cliente.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Cliente não encontrado" });

            try
            {
                // 1. Obter Configuracao Fiscal do Salao
                var configFiscal = await _salaoConfigFiscalHandler.ObterPorSalaoAsync(idSalao);
                if (configFiscal == null || configFiscal.CertificadoPfx == null)
                {
                    await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, null, "Pré-Validação", "Configurações Fiscais ou Certificado A1 não cadastrados para este Salão.");
                    return new JsonResult(new { success = false, message = "Configurações Fiscais ou Certificado A1 não cadastrados para este Salão." });
                }

                // Determinar o Tipo de Nota baseado em alguma regra de negócio.
                // Exemplo prático: Se o CodigoTributacaoMunicipio estiver preenchido, tratamos como NFS-e.
                // Caso contrário (venda de item com NCM prático), seria NFC-e. 
                // Por ora, assumiremos a base: A existência de um código de ISS dita a ida para a Prefeitura.
                bool isServicoNfse = !string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) || servico.Preco > 0;

                if (isServicoNfse)
                {
                    await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, null, "Início Emissão", "Iniciando processo de emissão de NFS-e (Serviço).");
                    var nfseObj = _fiscalBuilderService.MontarNFSe(configFiscal, cliente, servico, agendamento);
                    
                    var retornoEmissao = await _nfseEmissorService.EmitirNFSeAsync(configFiscal, nfseObj, req.IdAgendamento);
                    
                    var notaPersistida = new NotaFiscal
                    {
                        IdNotaFiscal = Guid.NewGuid(),
                        IdSalao = idSalao,
                        IdAgendamento = agendamento.IdAgendamento,
                        TipoNota = "NFS-e",
                        Ambiente = configFiscal.Ambiente,
                        Numero = 1,
                        Serie = 1,
                        ValorTotal = servico.Preco,
                        Status = retornoEmissao.Autorizada ? "Processando" : "Rejeitada",
                        ChaveAcesso = retornoEmissao.ChaveAcesso,
                        NumeroRecibo = retornoEmissao.Protocolo, 
                        ProtocoloAutorizacao = retornoEmissao.Protocolo,
                        JustificativaRejeicao = !retornoEmissao.Autorizada ? retornoEmissao.Motivo : null,
                        XmlEnvio = retornoEmissao.XmlEnvio,
                        XmlRetorno = retornoEmissao.XmlRetorno,
                        DataEmissao = DateTime.Now
                    };

                    await _notaFiscalHandler.InserirAsync(notaPersistida);

                    if (retornoEmissao.Autorizada)
                    {
                        await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, notaPersistida.IdNotaFiscal, "Processando", $"NFS-e recebida na prefeitura! Recibo: {retornoEmissao.Protocolo}", retornoEmissao.XmlRetorno);
                        return new JsonResult(new { success = true, message = $"NFS-e enviada com sucesso! Lote em processamento. Recibo: {retornoEmissao.Protocolo}", nf_id = notaPersistida.IdNotaFiscal });
                    }
                    else
                    {
                        await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, notaPersistida.IdNotaFiscal, "Rejeitada", $"NFS-e Rejeitada pela Prefeitura: {retornoEmissao.Motivo}", retornoEmissao.XmlRetorno);
                        return new JsonResult(new { success = false, message = $"NFS-e Rejeitada: {retornoEmissao.Motivo}" });
                    }
                }
                else
                {
                    // Fluxo NFC-e (Produtos)
                    await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, null, "Início Emissão", "Iniciando processo de emissão de NFC-e (Produto).");
                    
                    // Monta o XML da NFe
                    var nfeObj = _fiscalBuilderService.MontarNFCe(configFiscal, cliente, servico, agendamento);
                    
                    // Despacha para SEFAZ
                    var retornoEmissaoNFCe = await _nfceEmissorService.EmitirNFCeSincronoAsync(configFiscal, nfeObj);
                    
                    var notaPersistidaNfce = new NotaFiscal
                    {
                        IdNotaFiscal = Guid.NewGuid(),
                        IdSalao = idSalao,
                        IdAgendamento = agendamento.IdAgendamento,
                        TipoNota = "NFC-e",
                        Ambiente = configFiscal.Ambiente,
                        Numero = 1, // Idealmente vindo do banco (contador)
                        Serie = 1,
                        ValorTotal = servico.Preco,
                        Status = retornoEmissaoNFCe.Autorizada ? "Autorizada" : "Rejeitada",
                        ChaveAcesso = retornoEmissaoNFCe.ChaveAcesso,
                        NumeroRecibo = retornoEmissaoNFCe.Protocolo, 
                        ProtocoloAutorizacao = retornoEmissaoNFCe.Protocolo,
                        JustificativaRejeicao = !retornoEmissaoNFCe.Autorizada ? retornoEmissaoNFCe.Motivo : null,
                        XmlEnvio = retornoEmissaoNFCe.XmlEnvio,
                        XmlRetorno = retornoEmissaoNFCe.XmlRetorno,
                        DataEmissao = DateTime.Now
                    };

                    await _notaFiscalHandler.InserirAsync(notaPersistidaNfce);

                    if (retornoEmissaoNFCe.Autorizada)
                    {
                        await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, notaPersistidaNfce.IdNotaFiscal, "Autorizada", $"NFC-e Autorizada com sucesso! Prot: {retornoEmissaoNFCe.Protocolo}", retornoEmissaoNFCe.XmlRetorno);
                        return new JsonResult(new { success = true, message = $"NFC-e Autorizada! Status 100.", nf_id = notaPersistidaNfce.IdNotaFiscal });
                    }
                    else
                    {
                        await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, notaPersistidaNfce.IdNotaFiscal, "Rejeitada", $"NFC-e Rejeitada Sefaz: {retornoEmissaoNFCe.Motivo}", retornoEmissaoNFCe.XmlRetorno);
                        return new JsonResult(new { success = false, message = $"NFC-e Rejeitada: {retornoEmissaoNFCe.Motivo}" });
                    }
                }
            }
            catch (Exception ex)
            {
                await _notaFiscalLogHandler.LogarEtapaAsync(idSalao, req.IdAgendamento, null, "Erro Interno", "Falha catastrófica ao emitir nota: " + ex.Message);
                return new JsonResult(new { success = false, message = "Erro ao processar emissão NF: " + ex.Message });
            }
        }

        public IActionResult OnPostDelete(int id)
        {
            var agendamento = _agendamentoHandler.ObterPorId(id);
            if (agendamento == null) return NotFound();

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            // Validar se pertence ao salão
            var s = _servicoHandler.ObterPorId(agendamento.IdServico);
            if (s == null || s.IdSalao != idSalao) return BadRequest(new ErrorResponse { Message = "Acesso negado" });

            if (agendamento.Status == "Pago")
                return BadRequest(new ErrorResponse { Message = "Agendamentos pagos não podem ser excluídos." });

            _agendamentoHandler.Excluir(id);

            return new JsonResult(new { ok = true });
        }

        public IActionResult OnGetAvailableServices(DateTime start)
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var allServicos = _servicoHandler.ListarPorSalao(idSalao);

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
            var servico = _servicoHandler.ObterPorId(idServico);
            if (servico == null || servico.IdSalao != idSalao) return 0;

            var idsFuncionarios = _fsHandler.ListarFuncionariosDoServico(idServico);

            // Calculamos o fim
            DateTime fim = start.Add(servico.Duracao);

            foreach (var idF in idsFuncionarios)
            {
                var f = _funcionarioHandler.ObterPorId(idF);
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
                        if (_agendamentoHandler.VerificarDisponibilidade(f.IdFuncionario, start, fim, idAgendamentoIgnorar))
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

