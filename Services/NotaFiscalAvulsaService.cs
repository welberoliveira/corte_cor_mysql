using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using CorteCor.Handlers;
using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;

namespace CorteCor.Services
{
    public class NotaFiscalAvulsaRequest
    {
        public byte[]? CertificadoPfxBytes { get; set; }
        public string? CertificadoSenha { get; set; }
        public int Ambiente { get; set; } = 2;
        public string Modelo { get; set; } = "55";
        public string NaturezaOperacao { get; set; } = "Prestacao de servico";
        public int Serie { get; set; } = 1;
        public int Numero { get; set; } = 1;
        public DateTime DataEmissao { get; set; } = DateTime.Now;
        public string? EmitenteCnpj { get; set; }
        public string? EmitenteNome { get; set; }
        public string? EmitenteIE { get; set; }
        public string? EmitenteIM { get; set; }
        public int EmitenteCRT { get; set; } = 1;
        public string? EmitenteLogradouro { get; set; }
        public string? EmitenteNumero { get; set; }
        public string? EmitenteBairro { get; set; }
        public string? EmitenteCep { get; set; }
        public string? EmitenteCidade { get; set; }
        public string? EmitenteUF { get; set; }
        public int EmitenteCodMun { get; set; }
        public string? DestinatarioCpfCnpj { get; set; }
        public string? DestinatarioNome { get; set; }
        public string? DestinatarioIE { get; set; }
        public string? DestinatarioLogradouro { get; set; }
        public string? DestinatarioNumero { get; set; }
        public string? DestinatarioBairro { get; set; }
        public string? DestinatarioCep { get; set; }
        public string? DestinatarioCidade { get; set; }
        public string? DestinatarioUF { get; set; }
        public int DestinatarioCodMun { get; set; }
        public string? DestinatarioEmail { get; set; }
        public List<NotaFiscalAvulsaItemRequest> Itens { get; set; } = new();
    }

    public class NotaFiscalAvulsaItemRequest
    {
        public string CProd { get; set; } = "1";
        public string XProd { get; set; } = string.Empty;
        public string NCM { get; set; } = "00";
        public string CFOP { get; set; } = "5102";
        public string UCom { get; set; } = "UN";
        public decimal QCom { get; set; } = 1;
        public decimal VUnCom { get; set; }
        public decimal VProd { get; set; }
        public string CSOSN { get; set; } = "102";
        public decimal VTotTrib { get; set; }
        public string? CodigoTributacao { get; set; }
        public decimal AliquotaISS { get; set; } = 5;
    }

    public class NotaFiscalOperacaoResult
    {
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";
        public string? ProtocoloAutorizacao { get; set; }
        public string? XmlRetorno { get; set; }
        public string? XmlEnvio { get; set; }
        public string? ChaveAcesso { get; set; }
        public Guid? IdNotaFiscal { get; set; }
        public NotaFiscal? NotaFiscal { get; set; }
        public NotaFiscalRetornoResumo? RetornoResumo { get; set; }
        public List<string> Logs { get; } = new();
    }

    public class NotaFiscalRetornoResumo
    {
        public string StatusFiscal { get; set; } = NotaFiscalStatus.Rejeitada;
        public string? CodigoStatus { get; set; }
        public string? MensagemRetorno { get; set; }
        public string? Protocolo { get; set; }
        public string? ChaveAcesso { get; set; }
        public bool PodeCancelar { get; set; }
        public bool OperacaoConcluida { get; set; }
    }

    public class NotaFiscalHistoricoResult
    {
        public NotaFiscal? NotaFiscal { get; set; }
        public NotaFiscalRetornoResumo? RetornoResumo { get; set; }
        public List<NotaFiscalEvento> Eventos { get; set; } = new();
        public List<NotaFiscalLog> Logs { get; set; } = new();
    }

    public class NotaFiscalAcoesDisponiveis
    {
        public string ChaveFiscal { get; set; } = string.Empty;
        public string ClasseStatus { get; set; } = "bg-warning";
        public bool PodeBaixarXml { get; set; }
        public string TipoXmlPreferencial { get; set; } = "retorno";
        public bool PodeGerarPdf { get; set; }
        public bool PodeCancelar { get; set; }
        public bool PodeEnviarEmail { get; set; }
        public bool PodeCartaCorrecao { get; set; }
    }

    public class NotaFiscalConsultaNfsePayload
    {
        public string Metodo { get; set; } = "chave";
        public string XmlEnvio { get; set; } = string.Empty;
        public string XmlRetorno { get; set; } = string.Empty;
        public string? ProtocoloAutorizacao { get; set; }
        public string? ChaveAcesso { get; set; }
    }

    public class NotaFiscalAvulsaTelaContexto
    {
        public int Ambiente { get; set; } = 2;
        public int Serie { get; set; } = 1;
        public int NumeroSugerido { get; set; } = 1;
        public bool HasSavedCertificate { get; set; }
        public string? EmitenteCnpj { get; set; }
        public string? EmitenteNome { get; set; }
        public string? EmitenteIE { get; set; }
        public string? EmitenteIM { get; set; }
        public int EmitenteCRT { get; set; } = 1;
        public string? EmitenteLogradouro { get; set; }
        public string? EmitenteNumero { get; set; }
        public string? EmitenteBairro { get; set; }
        public string? EmitenteCep { get; set; }
        public string? EmitenteCidade { get; set; }
        public string? EmitenteUF { get; set; }
        public int EmitenteCodMun { get; set; }
        public NotaFiscalAvulsaItemRequest ItemPadrao { get; set; } = new()
        {
            XProd = "Servico avulso",
            QCom = 1,
            VUnCom = 0,
            CodigoTributacao = "010501",
            AliquotaISS = 5
        };
        public PagedResult<NotaFiscal> NotasEmitidasPaginadas { get; set; } = new();
        public PagedResult<NotaFiscalInutilizacao> InutilizacoesPaginadas { get; set; } = new();
        public List<NotaFiscal> NotasEmitidas { get; set; } = new();
        public List<NotaFiscalInutilizacao> Inutilizacoes { get; set; } = new();
    }

    public class NotaFiscalAvulsaService
    {
        private readonly SalaoConfigFiscalHandler _configHandler;
        private readonly NotaFiscalHandler _notaHandler;
        private readonly NotaFiscalLogHandler _logHandler;
        private readonly NotaFiscalEventoHandler _eventoHandler;
        private readonly NotaFiscalInutilizacaoHandler _inutHandler;
        private readonly ICriptografiaService _criptoService;
        private readonly FiscalBuilderService _fiscalBuilder;
        private readonly FiscalActionService _fiscalAction;
        private readonly NFSeEmissorService _nfseEmissor;
        private readonly FiscalPdfGenerator _pdfGenerator;
        private readonly IValidaParametrosMunicipioService _validaMunicipioService;
        private readonly BrevoEmailService _emailService;

        public NotaFiscalAvulsaService(
            SalaoConfigFiscalHandler configHandler,
            NotaFiscalHandler notaHandler,
            NotaFiscalLogHandler logHandler,
            NotaFiscalEventoHandler eventoHandler,
            NotaFiscalInutilizacaoHandler inutHandler,
            ICriptografiaService criptoService,
            FiscalBuilderService fiscalBuilder,
            FiscalActionService fiscalAction,
            NFSeEmissorService nfseEmissor,
            FiscalPdfGenerator pdfGenerator,
            IValidaParametrosMunicipioService validaMunicipioService,
            BrevoEmailService emailService)
        {
            _configHandler = configHandler;
            _notaHandler = notaHandler;
            _logHandler = logHandler;
            _eventoHandler = eventoHandler;
            _inutHandler = inutHandler;
            _criptoService = criptoService;
            _fiscalBuilder = fiscalBuilder;
            _fiscalAction = fiscalAction;
            _nfseEmissor = nfseEmissor;
            _pdfGenerator = pdfGenerator;
            _validaMunicipioService = validaMunicipioService;
            _emailService = emailService;
        }

        public async Task<NotaFiscalOperacaoResult> EmitirAsync(int idSalao, NotaFiscalAvulsaRequest request, string? usuario = null)
        {
            var result = new NotaFiscalOperacaoResult();
            var tipoNota = InferirTipoNota(request.Modelo);
            var notaId = Guid.NewGuid();

            request.Itens = request.Itens
                .Where(i => !string.IsNullOrWhiteSpace(i.XProd) || i.VUnCom > 0)
                .ToList();

            if (request.Itens.Count == 0)
            {
                throw new InvalidOperationException("Informe ao menos um item para emitir a nota fiscal.");
            }

            var config = await ObterConfigAtualizadaAsync(idSalao, request);
            var cert = await ObterCertificadoAsync(idSalao, request, config);
            if (cert == null)
            {
                throw new InvalidOperationException("Certificado digital não selecionado ou senha inválida.");
            }

            await _logHandler.LogarEtapaAsync(idSalao, null, notaId, "INICIO_AVULSA", $"Emitindo {tipoNota} serie {request.Serie} numero {request.Numero}");
            result.Logs.Add($"Processo iniciado para {tipoNota} serie {request.Serie} numero {request.Numero}.");

            if (tipoNota == "NFS-e")
            {
                await EmitirNfseAsync(idSalao, notaId, request, config, result);
            }
            else
            {
                await EmitirDocumentoEstadualAsync(idSalao, notaId, request, config, cert, result);
            }

            result.Mensagem = $"{tipoNota} processada com status {result.NotaFiscal?.Status}.";
            result.MensagemTipo = result.NotaFiscal?.Status == NotaFiscalStatus.Autorizada ? "success" : "warning";
            result.RetornoResumo ??= CriarResumoRetorno(result.XmlRetorno, result.ProtocoloAutorizacao, result.ChaveAcesso, result.NotaFiscal?.Status);
            return result;
        }

        public async Task<NotaFiscalAvulsaTelaContexto> ObterContextoTelaAsync(
            int idSalao,
            string modelo,
            int ambienteInformado,
            int serieInformada,
            int numeroInformado,
            int notasPage = 1,
            int inutilizacoesPage = 1,
            int pageSize = 10)
        {
            var config = await _configHandler.ObterPorSalaoAsync(idSalao);
            var tipoNota = InferirTipoNota(modelo);
            var ambiente = ambienteInformado == 0 ? config?.Ambiente ?? 2 : ambienteInformado;
            var serie = serieInformada > 0
                ? serieInformada
                : string.Equals(tipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase)
                    ? config?.SerieNFSe ?? 1
                    : config?.SerieNFCe ?? 1;

            var numero = numeroInformado > 1
                ? numeroInformado
                : await _notaHandler.ObterProximoNumeroAsync(idSalao, tipoNota, serie, ambiente);

            var notasPaginadas = await _notaHandler.ListarPorSalaoPaginadoAsync(idSalao, notasPage, pageSize);
            var inutilizacoesPaginadas = await _inutHandler.ListarPorSalaoPaginadoAsync(idSalao, inutilizacoesPage, pageSize);

            return new NotaFiscalAvulsaTelaContexto
            {
                Ambiente = ambiente,
                Serie = serie,
                NumeroSugerido = numero,
                HasSavedCertificate = config?.CertificadoPfx != null && config.CertificadoPfx.Length > 0,
                EmitenteCnpj = config?.Cnpj,
                EmitenteNome = config?.RazaoSocial,
                EmitenteIE = config?.InscricaoEstadual,
                EmitenteIM = config?.InscricaoMunicipal,
                EmitenteCRT = config?.RegimeTributario > 0 ? config.RegimeTributario : 1,
                EmitenteLogradouro = config?.EnderecoLogradouro,
                EmitenteNumero = config?.EnderecoNumero,
                EmitenteBairro = config?.EnderecoBairro,
                EmitenteCep = config?.EnderecoCep,
                EmitenteCidade = config?.EnderecoCidade,
                EmitenteUF = config?.EnderecoUF,
                EmitenteCodMun = config?.CodigoMunicipioIBGE ?? 0,
                NotasEmitidasPaginadas = notasPaginadas,
                InutilizacoesPaginadas = inutilizacoesPaginadas,
                NotasEmitidas = notasPaginadas.Items,
                Inutilizacoes = inutilizacoesPaginadas.Items
            };
        }

        public async Task<NotaFiscalOperacaoResult> ConsultarAsync(int idSalao, string modelo, int ambiente, string chaveAcesso)
        {
            ValidarChaveFiscal(chaveAcesso, "consultar");

            var result = new NotaFiscalOperacaoResult { ChaveAcesso = chaveAcesso };
            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada para a chave informada.");
            var config = await _configHandler.ObterPorSalaoAsync(idSalao)
                ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");
            var cert = new CertificadoFiscalFactory(_criptoService).InstanciarCertificado(config);
            var chaveFiscalNormalizada = string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase)
                ? PrimeiroValorPreenchido(ObterChaveFiscal(nota), chaveAcesso)
                : chaveAcesso;
            result.ChaveAcesso = chaveFiscalNormalizada;

            string novoStatus;
            if (nota.TipoNota == "NFS-e" || string.Equals(modelo, "NFSE", StringComparison.OrdinalIgnoreCase))
            {
                if (await SincronizarCancelamentoNfseSeExistirAsync(idSalao, config, nota, chaveFiscalNormalizada))
                {
                    novoStatus = NotaFiscalStatus.Cancelada;
                    result.XmlEnvio = nota.XmlEnvio;
                    result.XmlRetorno = nota.XmlRetorno;
                    result.ProtocoloAutorizacao = nota.ProtocoloAutorizacao;
                    result.ChaveAcesso = PrimeiroValorPreenchido(ObterChaveFiscal(nota), chaveFiscalNormalizada);
                }
                else
                {
                    var consulta = ConsultarNfsePorChave(cert, ambiente, chaveFiscalNormalizada);
                    result.XmlEnvio = consulta.XmlEnvio;
                    result.XmlRetorno = consulta.XmlRetorno;
                    result.Logs.Add("Consulta NFS-e executada pelo identificador da propria NFS-e.");

                    if (EhErroConsultaChaveNfse(result.XmlRetorno))
                    {
                        var idDps = ExtrairIdDps(nota.XmlEnvio) ?? ExtrairIdDps(nota.XmlRetorno);
                        if (!string.IsNullOrWhiteSpace(idDps))
                        {
                            consulta = ConsultarNfsePorDps(cert, ambiente, idDps);
                            result.XmlEnvio = consulta.XmlEnvio;
                            result.XmlRetorno = consulta.XmlRetorno;
                            result.Logs.Add("Consulta NFS-e refeita pelo identificador da DPS apos retorno E2406 na consulta por chave.");
                        }
                    }

                    result.ProtocoloAutorizacao = ExtrairProtocolo(result.XmlRetorno);
                    result.ChaveAcesso = ExtrairChaveNfse(result.XmlRetorno) ?? chaveFiscalNormalizada;
                    novoStatus = ClassificarStatusPorXml(result.XmlRetorno);

                    if (novoStatus != NotaFiscalStatus.Cancelada &&
                        await SincronizarCancelamentoNfseSeExistirAsync(idSalao, config, nota, chaveFiscalNormalizada))
                    {
                        novoStatus = NotaFiscalStatus.Cancelada;
                        result.XmlRetorno = nota.XmlRetorno;
                        result.ProtocoloAutorizacao = nota.ProtocoloAutorizacao;
                        result.ChaveAcesso = PrimeiroValorPreenchido(ObterChaveFiscal(nota), chaveFiscalNormalizada);
                    }
                    else if (EhErroConsultaChaveNfse(result.XmlRetorno))
                    {
                        // Não rebaixa o estado local por uma consulta nacional inconclusiva.
                        novoStatus = nota.Status;
                    }
                }
            }
            else
            {
                var tipoDocumento = string.Equals(nota.TipoNota, "NFC-e", StringComparison.OrdinalIgnoreCase)
                    ? TipoDFe.NFCe
                    : TipoDFe.NFe;

                var configuracao = new Configuracao
                {
                    TipoDFe = tipoDocumento,
                    CertificadoDigital = cert,
                    TipoAmbiente = ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao
                };

                var consulta = new ConsSitNFe
                {
                    Versao = "4.00",
                    TpAmb = ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                    ChNFe = chaveAcesso
                };

                var servico = new Unimake.Business.DFe.Servicos.NFe.ConsultaProtocolo(consulta, configuracao);
                servico.Executar();
                result.XmlRetorno = servico.RetornoWSString;
                result.ProtocoloAutorizacao = ExtrairProtocolo(result.XmlRetorno);
                result.ChaveAcesso = chaveAcesso;
                novoStatus = ClassificarStatusPorXml(result.XmlRetorno);
            }

              nota.Status = novoStatus;
              nota.ProtocoloAutorizacao = result.ProtocoloAutorizacao ?? nota.ProtocoloAutorizacao;
              nota.XmlRetorno = result.XmlRetorno ?? nota.XmlRetorno;
              if (!string.IsNullOrWhiteSpace(result.ChaveAcesso) && nota.TipoNota == "NFS-e")
              {
                  nota.ChaveAcessoNacional = result.ChaveAcesso;
              }

            await _notaHandler.AtualizarAsync(nota);
            await _logHandler.LogarEtapaAsync(idSalao, nota.IdAgendamento, nota.IdNotaFiscal, "CONSULTA_STATUS", $"Consulta atualizou a nota para {novoStatus}.", result.XmlRetorno);

            result.NotaFiscal = nota;
            result.IdNotaFiscal = nota.IdNotaFiscal;
            var detalheConsulta = ExtrairMensagemRetorno(result.XmlRetorno);
            result.Mensagem = string.IsNullOrWhiteSpace(detalheConsulta)
                ? $"Consulta realizada. Status atual: {novoStatus}."
                : $"Consulta realizada. Status atual: {novoStatus}. Detalhe: {detalheConsulta}";
            result.MensagemTipo =
                novoStatus == NotaFiscalStatus.Autorizada || novoStatus == NotaFiscalStatus.Cancelada
                    ? "success"
                    : "warning";
            result.RetornoResumo = CriarResumoRetorno(result.XmlRetorno, result.ProtocoloAutorizacao, result.ChaveAcesso, novoStatus);
            result.Logs.Add(result.Mensagem);
            return result;
        }

        private static Configuracao CriarConfiguracaoConsultaNfse(X509Certificate2 cert, int ambiente)
        {
            return new Configuracao
            {
                TipoDFe = TipoDFe.NFSe,
                CertificadoDigital = cert,
                CodigoMunicipio = 1001058,
                TipoAmbiente = ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                PadraoNFSe = PadraoNFSe.NACIONAL,
                Servico = Unimake.Business.DFe.Servicos.Servico.NFSeConsultarNfse,
                SchemaVersao = "1.01",
                BuscarConfiguracaoPastaBase = true,
                PastaArquivoConfiguracao = AppContext.BaseDirectory
            };
        }

        private static NotaFiscalConsultaNfsePayload ConsultarNfsePorChave(X509Certificate2 cert, int ambiente, string chaveFiscal)
        {
            var consulta = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.NFSe
            {
                Versao = "1.01",
                InfNFSe = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.InfNFSe
                {
                    Id = chaveFiscal
                }
            };

            var xml = consulta.GerarXML();
            var servico = new Unimake.Business.DFe.Servicos.NFSe.ConsultarNfse(xml, CriarConfiguracaoConsultaNfse(cert, ambiente));
            servico.Executar();

            return new NotaFiscalConsultaNfsePayload
            {
                Metodo = "chave",
                XmlEnvio = xml.OuterXml,
                XmlRetorno = servico.RetornoWSString
            };
        }

        private static NotaFiscalConsultaNfsePayload ConsultarNfsePorDps(X509Certificate2 cert, int ambiente, string idDps)
        {
            var consulta = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.DPS
            {
                Versao = "1.01",
                InfDPS = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.InfDPS
                {
                    Id = idDps
                }
            };

            var xml = consulta.GerarXML();
            var servico = new Unimake.Business.DFe.Servicos.NFSe.ConsultarNfse(xml, CriarConfiguracaoConsultaNfse(cert, ambiente));
            servico.Executar();

            return new NotaFiscalConsultaNfsePayload
            {
                Metodo = "dps",
                XmlEnvio = xml.OuterXml,
                XmlRetorno = servico.RetornoWSString
            };
        }

        public async Task<NotaFiscalOperacaoResult> CancelarAsync(int idSalao, string chaveAcesso, string justificativa)
        {
            ValidarChaveFiscal(chaveAcesso, "cancelar");
            ValidarTextoMinimo(justificativa, 15, "Informe uma justificativa com no minimo 15 caracteres.");

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada para cancelamento.");
            ValidarNotaParaCancelamento(nota);
            var config = await _configHandler.ObterPorSalaoAsync(idSalao)
                ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");

            var chaveFiscalNormalizada = string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase)
                ? PrimeiroValorPreenchido(ObterChaveFiscal(nota), chaveAcesso)
                : chaveAcesso;

            NotaFiscalEvento evento = nota.TipoNota == "NFS-e"
                ? await _fiscalAction.CancelarNfseAsync(config, chaveFiscalNormalizada, justificativa)
                : await _fiscalAction.CancelarNfceAsync(config, chaveFiscalNormalizada, justificativa, nota.ProtocoloAutorizacao ?? string.Empty);

            evento.IdNotaFiscal = nota.IdNotaFiscal;
            evento.IdSalao = idSalao;
            evento.DataRegistro = DateTime.Now;
            await _eventoHandler.InserirAsync(evento);

            if (EhStatusEventoAutorizado(evento.Status, evento.XmlRetorno))
            {
                nota.Status = NotaFiscalStatus.Cancelada;
                await _notaHandler.AtualizarAsync(nota);
            }
            else if (nota.TipoNota == "NFS-e" &&
                     EhCancelamentoNfseJaVinculado(evento.Status, evento.XmlRetorno))
            {
                await SincronizarCancelamentoNfseSeExistirAsync(idSalao, config, nota, chaveFiscalNormalizada);
                nota.Status = NotaFiscalStatus.Cancelada;
                await _notaHandler.AtualizarAsync(nota);
                nota.Status = NotaFiscalStatus.Cancelada;
                evento.Status = "Cancelamento Autorizado (evento ja vinculado no provedor)";
            }

            await _logHandler.LogarEtapaAsync(idSalao, nota.IdAgendamento, nota.IdNotaFiscal, "CANCELAMENTO", evento.Status, evento.XmlRetorno);

            return new NotaFiscalOperacaoResult
            {
                IdNotaFiscal = nota.IdNotaFiscal,
                NotaFiscal = nota,
                ChaveAcesso = chaveFiscalNormalizada,
                XmlEnvio = evento.XmlEnvio,
                XmlRetorno = evento.XmlRetorno,
                ProtocoloAutorizacao = evento.ProtocoloEvento,
                Mensagem = MontarMensagemOperacao("Cancelamento", evento.Status, evento.XmlRetorno),
                MensagemTipo = nota.Status == NotaFiscalStatus.Cancelada ? "success" : "warning",
                RetornoResumo = CriarResumoRetorno(evento.XmlRetorno, evento.ProtocoloEvento, chaveFiscalNormalizada, nota.Status)
            };
        }

        public async Task<NotaFiscalOperacaoResult> InutilizarAsync(int idSalao, int ano, int serie, int numInicial, int numFinal, string justificativa, string tipoNota)
        {
            ValidarTextoMinimo(justificativa, 15, "Informe uma justificativa com no minimo 15 caracteres.");

            ValidarFaixaInutilizacao(ano, serie, numInicial, numFinal, tipoNota);

            var config = await _configHandler.ObterPorSalaoAsync(idSalao)
                ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");
            var evento = await _fiscalAction.InutilizarNfceAsync(config, ano, serie, numInicial, numFinal, justificativa, tipoNota);

            var inutilizacao = new NotaFiscalInutilizacao
            {
                IdSalao = idSalao,
                Ano = ano,
                Modelo = tipoNota == "NF-e" ? 55 : 65,
                Serie = serie,
                NumeroInicial = numInicial,
                NumeroFinal = numFinal,
                Justificativa = justificativa,
                Status = evento.Status,
                Protocolo = evento.ProtocoloEvento,
                XmlRetorno = evento.XmlRetorno,
                DataInutilizacao = DateTime.Now
            };

            await _inutHandler.InserirAsync(inutilizacao);
            await _logHandler.LogarEtapaAsync(idSalao, null, null, "INUTILIZACAO", evento.Status, evento.XmlRetorno);

            return new NotaFiscalOperacaoResult
            {
                Mensagem = MontarMensagemOperacao("Inutilizacao", evento.Status, evento.XmlRetorno),
                MensagemTipo = EhStatusEventoAutorizado(evento.Status, evento.XmlRetorno) ? "success" : "warning",
                XmlEnvio = evento.XmlEnvio,
                XmlRetorno = evento.XmlRetorno,
                ProtocoloAutorizacao = evento.ProtocoloEvento,
                RetornoResumo = CriarResumoRetorno(
                    evento.XmlRetorno,
                    evento.ProtocoloEvento,
                    null,
                    EhStatusEventoAutorizado(evento.Status, evento.XmlRetorno) ? NotaFiscalStatus.Autorizada : NotaFiscalStatus.Rejeitada)
            };
        }

        public async Task<NotaFiscalOperacaoResult> CartaCorrecaoAsync(int idSalao, string chaveAcesso, string textoCorrecao)
        {
            ValidarChaveFiscal(chaveAcesso, "enviar carta de correção");
            ValidarTextoMinimo(textoCorrecao, 15, "A correção deve ter no mínimo 15 caracteres.");

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada.");
            ValidarCartaCorrecaoParaTipoNota(nota.TipoNota);
            ValidarNotaParaCartaCorrecao(nota);
            var config = await _configHandler.ObterPorSalaoAsync(idSalao)
                ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");
            var eventos = await _eventoHandler.ListarPorNotaAsync(nota.IdNotaFiscal);
            var sequencia = eventos.Count(e => e.TipoEvento == "CC-e") + 1;

            var evento = await _fiscalAction.EnviarCartaCorrecaoAsync(config, chaveAcesso, textoCorrecao, sequencia);
            evento.IdNotaFiscal = nota.IdNotaFiscal;
            evento.IdSalao = idSalao;
            evento.DataRegistro = DateTime.Now;
            await _eventoHandler.InserirAsync(evento);
            await _logHandler.LogarEtapaAsync(idSalao, nota.IdAgendamento, nota.IdNotaFiscal, "CARTA_CORRECAO", evento.Status, evento.XmlRetorno);

            return new NotaFiscalOperacaoResult
            {
                IdNotaFiscal = nota.IdNotaFiscal,
                NotaFiscal = nota,
                ChaveAcesso = chaveAcesso,
                XmlEnvio = evento.XmlEnvio,
                XmlRetorno = evento.XmlRetorno,
                ProtocoloAutorizacao = evento.ProtocoloEvento,
                Mensagem = MontarMensagemOperacao("Carta de correção", evento.Status, evento.XmlRetorno),
                MensagemTipo = evento.Status.Contains("135") ? "success" : "warning",
                RetornoResumo = CriarResumoRetorno(
                    evento.XmlRetorno,
                    evento.ProtocoloEvento,
                    chaveAcesso,
                    evento.Status.Contains("135", StringComparison.OrdinalIgnoreCase) ? NotaFiscalStatus.Autorizada : NotaFiscalStatus.Rejeitada)
            };
        }

        public async Task<(byte[] Bytes, string FileName)> GerarPdfAsync(int idSalao, string chaveAcesso)
        {
            ValidarChaveFiscal(chaveAcesso, "gerar PDF");

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada.");
            var config = await _configHandler.ObterPorSalaoAsync(idSalao);
            var bytes = await _pdfGenerator.GerarPdfAsync(nota, config);
            var prefixo = string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase) ? "DANFSE" : "DANFE";
            return (bytes, $"{prefixo}_{nota.TipoNota}_{nota.Numero}.pdf");
        }

        public async Task<NotaFiscalOperacaoResult> EnviarEmailAsync(int idSalao, string chaveAcesso, string emailDestino, string? nomeDestino = null)
        {
            ValidarChaveFiscal(chaveAcesso, "enviar e-mail");
            ValidarEmailDestino(emailDestino);

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada.");

            if (!string.Equals(nota.Status, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(nota.Status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Somente notas autorizadas ou canceladas podem ser enviadas por e-mail.");
            }

            var pdf = await GerarPdfAsync(idSalao, chaveAcesso);
            var anexos = new List<EmailAttachment>
            {
                new()
                {
                    Name = pdf.FileName,
                    Content = pdf.Bytes,
                    ContentType = "application/pdf"
                }
            };

            if (!string.IsNullOrWhiteSpace(nota.XmlRetorno))
            {
                anexos.Add(new EmailAttachment
                {
                    Name = $"NF_{nota.TipoNota}_{nota.Numero}_retorno.xml",
                    Content = Encoding.UTF8.GetBytes(nota.XmlRetorno),
                    ContentType = "application/xml"
                });
            }

            if (!string.IsNullOrWhiteSpace(nota.XmlEnvio))
            {
                anexos.Add(new EmailAttachment
                {
                    Name = $"NF_{nota.TipoNota}_{nota.Numero}_envio.xml",
                    Content = Encoding.UTF8.GetBytes(nota.XmlEnvio),
                    ContentType = "application/xml"
                });
            }

            var assunto = $"{nota.TipoNota} {nota.Numero} - CorteCor";
            var corpoHtml = new StringBuilder()
                .Append("<p>Segue em anexo o documento fiscal solicitado.</p>")
                .Append($"<p><strong>Tipo:</strong> {nota.TipoNota}<br/>")
                .Append($"<strong>Numero:</strong> {nota.Numero}<br/>")
                .Append($"<strong>Serie:</strong> {nota.Serie}<br/>")
                .Append($"<strong>Status:</strong> {nota.Status}<br/>")
                .Append($"<strong>Protocolo:</strong> {nota.ProtocoloAutorizacao ?? "-"}<br/>")
                .Append($"<strong>Chave:</strong> {nota.ChaveAcesso ?? nota.ChaveAcessoNacional ?? "-"}</p>")
                .Append("<p>Este e-mail foi enviado automaticamente pelo modulo fiscal.</p>")
                .ToString();

            var envio = await _emailService.EnviarEmailComAnexosAsync(
                emailDestino,
                string.IsNullOrWhiteSpace(nomeDestino) ? "Cliente" : nomeDestino,
                assunto,
                corpoHtml,
                anexos);

            if (!envio.Success)
            {
                await _logHandler.LogarEtapaAsync(idSalao, nota.IdAgendamento, nota.IdNotaFiscal, "ENVIO_EMAIL_ERRO", envio.ErrorMessage ?? "Falha ao enviar e-mail.");
                throw new InvalidOperationException(envio.ErrorMessage ?? "Falha ao enviar o e-mail da nota.");
            }

            await _logHandler.LogarEtapaAsync(idSalao, nota.IdAgendamento, nota.IdNotaFiscal, "ENVIO_EMAIL", $"Nota enviada por e-mail para {emailDestino}.");

            return new NotaFiscalOperacaoResult
            {
                IdNotaFiscal = nota.IdNotaFiscal,
                NotaFiscal = nota,
                ChaveAcesso = ObterChaveFiscal(nota),
                Mensagem = $"Nota enviada por e-mail para {emailDestino}.",
                MensagemTipo = "success",
                RetornoResumo = CriarResumoRetorno(
                    nota.XmlRetorno,
                    nota.ProtocoloAutorizacao,
                    ObterChaveFiscal(nota),
                    nota.Status)
              };
        }

        public async Task<(byte[] Bytes, string FileName)> ObterXmlAsync(int idSalao, string chaveAcesso, string tipo)
        {
            ValidarChaveFiscal(chaveAcesso, "baixar XML");
            ValidarTipoXmlSolicitado(tipo);

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada.");
            var xml = tipo == "retorno" ? nota.XmlRetorno : nota.XmlEnvio;
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new InvalidOperationException("XML não encontrado para a nota informada.");
            }

            return (Encoding.UTF8.GetBytes(xml), $"NotaFiscal_{nota.TipoNota}_{nota.Numero}_{tipo}.xml");
        }

        public async Task<NotaFiscalHistoricoResult> ObterHistoricoAsync(int idSalao, string chaveAcesso)
        {
            ValidarChaveFiscal(chaveAcesso, "consultar o historico");

            var nota = await ObterNotaPorIdentificadorAsync(idSalao, chaveAcesso)
                ?? throw new InvalidOperationException("Nota não encontrada.");

            return new NotaFiscalHistoricoResult
            {
                NotaFiscal = nota,
                RetornoResumo = CriarResumoRetorno(
                    nota.XmlRetorno,
                    nota.ProtocoloAutorizacao,
                    ObterChaveFiscal(nota),
                    nota.Status),
                Eventos = (await _eventoHandler.ListarPorNotaAsync(nota.IdNotaFiscal))
                    .OrderByDescending(e => e.DataRegistro)
                    .ToList(),
                Logs = (await _logHandler.ListarPorNotaFiscalAsync(nota.IdNotaFiscal, idSalao))
                    .OrderByDescending(l => l.DataHora)
                    .ToList()
            };
        }

        public async Task<List<NotaFiscal>> ListarNotasRecentesAsync(int idSalao, int limite = 20)
        {
            var notas = await _notaHandler.ListarPorSalaoAsync(idSalao);
            notas = notas.Take(limite).ToList();

            foreach (var nota in notas)
            {
                await SincronizarChaveNfseSeNecessarioAsync(nota);
                await ReconciliarStatusPersistidoAsync(idSalao, nota);
            }

            return notas;
        }

        private async Task EmitirNfseAsync(int idSalao, Guid notaId, NotaFiscalAvulsaRequest request, SalaoConfigFiscal config, NotaFiscalOperacaoResult result)
        {
            var item = request.Itens.First();
            var tomador = new Pessoa
            {
                Nome = request.DestinatarioNome,
                CpfCnpj = request.DestinatarioCpfCnpj,
                Cep = request.DestinatarioCep,
                Logradouro = request.DestinatarioLogradouro,
                Numero = request.DestinatarioNumero,
                Bairro = request.DestinatarioBairro,
                Cidade = (request.DestinatarioCodMun > 0 ? request.DestinatarioCodMun : 3143302).ToString()
            };
            var servico = new CorteCor.Models.Servico
            {
                IdServico = 0,
                Nome = item.XProd,
                Preco = item.VUnCom,
                CodigoTributacaoMunicipio = item.CodigoTributacao ?? "060101",
                AliquotaISS = item.AliquotaISS
            };
            var agendamento = new Agendamento { DataHora = request.DataEmissao };

            await _validaMunicipioService.ValidateAsync(config, servico);
            var dps = _fiscalBuilder.MontarNFSe(config, tomador, servico, agendamento, request.Serie, request.Numero);
            var retorno = await _nfseEmissor.EmitirNFSeAsync(config, dps, null, notaId);

            var valorTotal = request.Itens.Sum(i => i.QCom * i.VUnCom);
              var nota = new NotaFiscal
              {
                  IdNotaFiscal = notaId,
                  IdSalao = idSalao,
                  TipoNota = "NFS-e",
                Ambiente = request.Ambiente,
                Numero = request.Numero,
                Serie = request.Serie,
                ValorTotal = valorTotal,
                Status = retorno.Autorizada ? NotaFiscalStatus.Autorizada : ClassificarStatusPorXml(retorno.XmlRetorno),
                  ChaveAcessoNacional = PrimeiroValorPreenchido(
                      ChaveNfseValidaOuNula(retorno.ChaveAcesso),
                      ExtrairChaveNfse(retorno.XmlRetorno),
                      ExtrairChaveNfse(retorno.XmlEnvio)),
                  ChaveAcesso = null,
                  NumeroNFSeNacional = PrimeiroValorPreenchido(retorno.NumeroDocumentoFiscal, ExtrairNumeroNfse(retorno.XmlRetorno)),
                  ProtocoloAutorizacao = PrimeiroValorPreenchido(
                      ExtrairPrimeiroValorXml(retorno.XmlRetorno, "nDFSe", "nProt"),
                      retorno.Protocolo),
                JustificativaRejeicao = retorno.Autorizada ? null : CoalesceMensagemFiscal(retorno.Motivo, retorno.XmlRetorno),
                XmlEnvio = retorno.XmlEnvio,
                XmlRetorno = retorno.XmlRetorno,
                DataEmissao = request.DataEmissao,
                DataAtualizacao = DateTime.Now
            };

              await _notaHandler.InserirAsync(nota);
              result.NotaFiscal = nota;
              result.IdNotaFiscal = nota.IdNotaFiscal;
              result.ChaveAcesso = ObterChaveFiscal(nota);
            result.XmlEnvio = nota.XmlEnvio;
            result.XmlRetorno = nota.XmlRetorno;
            result.ProtocoloAutorizacao = nota.ProtocoloAutorizacao;
            result.Mensagem = nota.Status == NotaFiscalStatus.Autorizada
                ? "NFS-e emitida com sucesso."
                : $"NFS-e retornou {nota.Status}. {nota.JustificativaRejeicao}";
            result.MensagemTipo = nota.Status == NotaFiscalStatus.Autorizada ? "success" : "warning";
              result.RetornoResumo = CriarResumoRetorno(nota.XmlRetorno, nota.ProtocoloAutorizacao, ObterChaveFiscal(nota), nota.Status);
            result.Logs.Add($"NFS-e salva no banco com status {nota.Status}.");
        }

        private async Task EmitirDocumentoEstadualAsync(int idSalao, Guid notaId, NotaFiscalAvulsaRequest request, SalaoConfigFiscal config, X509Certificate2 cert, NotaFiscalOperacaoResult result)
        {
            var modeloDFe = request.Modelo == "65" ? ModeloDFe.NFCe : ModeloDFe.NFe;
            var infNfe = new InfNFe
            {
                Versao = "4.00",
                Ide = new Ide
                {
                    CUF = GetUfBrasil(request.EmitenteUF),
                    NatOp = string.IsNullOrWhiteSpace(request.NaturezaOperacao) ? "Operacao avulsa" : request.NaturezaOperacao,
                    Mod = modeloDFe,
                    Serie = request.Serie,
                    NNF = request.Numero,
                    DhEmi = request.DataEmissao,
                    TpNF = TipoOperacao.Saida,
                    IdDest = string.Equals(request.EmitenteUF, request.DestinatarioUF, StringComparison.OrdinalIgnoreCase)
                        ? DestinoOperacao.OperacaoInterna
                        : DestinoOperacao.OperacaoInterestadual,
                    CMunFG = request.EmitenteCodMun,
                    TpImp = modeloDFe == ModeloDFe.NFCe ? FormatoImpressaoDANFE.NFCe : FormatoImpressaoDANFE.NormalRetrato,
                    TpEmis = TipoEmissao.Normal,
                    TpAmb = request.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                    FinNFe = FinalidadeNFe.Normal,
                    IndFinal = SimNao.Sim,
                    IndPres = IndicadorPresenca.OperacaoPresencial,
                    ProcEmi = ProcessoEmissao.AplicativoContribuinte,
                    VerProc = "CorteCor 1.0"
                },
                Emit = new Emit
                {
                    CNPJ = LimparDocumento(request.EmitenteCnpj),
                    XNome = request.EmitenteNome,
                    IE = string.IsNullOrWhiteSpace(request.EmitenteIE) ? null : request.EmitenteIE,
                    CRT = (CRT)request.EmitenteCRT,
                    EnderEmit = new EnderEmit
                    {
                        XLgr = request.EmitenteLogradouro ?? "Endereço não informado",
                        Nro = request.EmitenteNumero ?? "SN",
                        XBairro = request.EmitenteBairro ?? "Bairro não informado",
                        CMun = request.EmitenteCodMun,
                        XMun = request.EmitenteCidade ?? "Cidade não informada",
                        UF = GetUfBrasil(request.EmitenteUF),
                        CEP = LimparCep(request.EmitenteCep),
                        CPais = 1058,
                        XPais = "Brasil"
                    }
                },
                Dest = MontarDestinatario(request),
                Det = new List<Det>()
            };

            double totalBase = 0;
            for (var i = 0; i < request.Itens.Count; i++)
            {
                var item = request.Itens[i];
                var valorProduto = (double)(item.QCom * item.VUnCom);
                totalBase += valorProduto;
                infNfe.Det.Add(new Det
                {
                    NItem = i + 1,
                    Prod = new Prod
                    {
                        CProd = string.IsNullOrWhiteSpace(item.CProd) ? (i + 1).ToString(CultureInfo.InvariantCulture) : item.CProd,
                        XProd = request.Ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : item.XProd,
                        NCM = string.IsNullOrWhiteSpace(item.NCM) || item.NCM == "00" ? "99" : item.NCM,
                        CFOP = string.IsNullOrWhiteSpace(item.CFOP) ? "5102" : item.CFOP,
                        UCom = string.IsNullOrWhiteSpace(item.UCom) ? "UN" : item.UCom,
                        QCom = item.QCom,
                        VUnCom = item.VUnCom,
                        VProd = valorProduto,
                        CEAN = "SEM GTIN",
                        CEANTrib = "SEM GTIN",
                        UTrib = string.IsNullOrWhiteSpace(item.UCom) ? "UN" : item.UCom,
                        QTrib = item.QCom,
                        VUnTrib = item.VUnCom,
                        IndTot = SimNao.Sim
                    },
                    Imposto = new Imposto
                    {
                        ICMS = new ICMS
                        {
                            ICMSSN102 = new ICMSSN102
                            {
                                Orig = OrigemMercadoria.Nacional,
                                CSOSN = string.IsNullOrWhiteSpace(item.CSOSN) ? "102" : item.CSOSN
                            }
                        }
                    }
                });
            }

            infNfe.Total = new Total
            {
                ICMSTot = new ICMSTot
                {
                    VProd = totalBase,
                    VNF = totalBase,
                    VTotTrib = (double)request.Itens.Sum(i => i.VTotTrib),
                    VBC = 0,
                    VICMS = 0,
                    VDesc = 0
                }
            };
            infNfe.Transp = new Transp { ModFrete = ModalidadeFrete.SemOcorrenciaTransporte };
            infNfe.Pag = new Pag
            {
                DetPag = new List<DetPag>
                {
                    new DetPag
                    {
                        IndPag = IndicadorPagamento.PagamentoVista,
                        TPag = Unimake.Business.DFe.Servicos.MeioPagamento.Outros,
                        VPag = totalBase
                    }
                }
            };

            var nfe = new NFe { InfNFe = new List<InfNFe> { infNfe } };
            var enviNfe = new EnviNFe
            {
                Versao = "4.00",
                IdLote = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture),
                IndSinc = SimNao.Sim,
                NFe = new List<NFe> { nfe }
            };

            var configuracao = new Configuracao
            {
                TipoDFe = modeloDFe == ModeloDFe.NFCe ? TipoDFe.NFCe : TipoDFe.NFe,
                CertificadoDigital = cert,
                TipoAmbiente = request.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                BuscarConfiguracaoPastaBase = false
            };

            if (modeloDFe == ModeloDFe.NFCe)
            {
                if (string.IsNullOrWhiteSpace(config.CSC) || string.IsNullOrWhiteSpace(config.IdCSC))
                {
                    throw new InvalidOperationException("Configure CSC e IdCSC antes de emitir NFC-e pela tela avulsa.");
                }

                configuracao.CSC = config.CSC;
                configuracao.CSCIDToken = int.TryParse(config.IdCSC, out var tokenId) ? tokenId : 1;
                configuracao.CodigoUF = (int)infNfe.Ide.CUF;
                configuracao.Definida = true;
                configuracao.VersaoQRCodeNFCe = 2;
            }

            var validarSchema = configuracao.GetType().GetProperty("ValidarSchema");
            if (validarSchema != null)
            {
                validarSchema.SetValue(configuracao, false);
            }

            string xmlRetorno;
            if (modeloDFe == ModeloDFe.NFCe)
            {
                var servico = new Unimake.Business.DFe.Servicos.NFCe.Autorizacao(enviNfe, configuracao);
                servico.Executar();
                xmlRetorno = servico.RetornoWSString;
            }
            else
            {
                var servico = new Unimake.Business.DFe.Servicos.NFe.Autorizacao(enviNfe, configuracao);
                servico.Executar();
                xmlRetorno = servico.RetornoWSString;
            }

            var protocolo = ExtrairProtocolo(xmlRetorno);
            var chave = infNfe.Id?.Replace("NFe", string.Empty, StringComparison.Ordinal) ?? string.Empty;
            var status = ClassificarStatusPorXml(xmlRetorno);
            var mensagemFiscal = CoalesceMensagemFiscal(protocolo, xmlRetorno);
            var nota = new NotaFiscal
            {
                IdNotaFiscal = notaId,
                IdSalao = idSalao,
                TipoNota = request.Modelo == "65" ? "NFC-e" : "NF-e",
                Ambiente = request.Ambiente,
                Numero = request.Numero,
                Serie = request.Serie,
                ValorTotal = (decimal)totalBase,
                Status = status,
                ChaveAcesso = chave,
                ProtocoloAutorizacao = protocolo,
                JustificativaRejeicao = status == NotaFiscalStatus.Rejeitada ? mensagemFiscal : null,
                XmlEnvio = enviNfe.GerarXML().OuterXml,
                XmlRetorno = xmlRetorno,
                DataEmissao = request.DataEmissao,
                DataAtualizacao = DateTime.Now
            };

            await _notaHandler.InserirAsync(nota);
            await _logHandler.LogarEtapaAsync(idSalao, null, notaId, "AUTORIZACAO_ESTADUAL", $"Documento retornou {status}.", xmlRetorno);

            result.NotaFiscal = nota;
            result.IdNotaFiscal = nota.IdNotaFiscal;
            result.ChaveAcesso = chave;
            result.XmlEnvio = nota.XmlEnvio;
            result.XmlRetorno = nota.XmlRetorno;
            result.ProtocoloAutorizacao = nota.ProtocoloAutorizacao;
            result.Mensagem = status == NotaFiscalStatus.Autorizada
                ? $"{nota.TipoNota} emitida com sucesso."
                : $"{nota.TipoNota} retornou {status}. {mensagemFiscal}";
            result.MensagemTipo = status == NotaFiscalStatus.Autorizada ? "success" : "warning";
            result.RetornoResumo = CriarResumoRetorno(nota.XmlRetorno, nota.ProtocoloAutorizacao, nota.ChaveAcesso, nota.Status);
            result.Logs.Add($"{nota.TipoNota} salva no banco com status {nota.Status}.");
        }

        private Dest MontarDestinatario(NotaFiscalAvulsaRequest request)
        {
            var destinatario = new Dest
            {
                XNome = request.Ambiente == 2
                    ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL"
                    : (request.DestinatarioNome ?? "CONSUMIDOR FINAL"),
                IndIEDest = IndicadorIEDestinatario.NaoContribuinte,
                EnderDest = new EnderDest
                {
                    XLgr = request.DestinatarioLogradouro ?? "Endereço não informado",
                    Nro = request.DestinatarioNumero ?? "SN",
                    XBairro = request.DestinatarioBairro ?? "Bairro não informado",
                    CMun = request.DestinatarioCodMun,
                    XMun = request.DestinatarioCidade ?? "Cidade não informada",
                    UF = GetUfBrasil(request.DestinatarioUF),
                    CEP = LimparCep(request.DestinatarioCep)
                }
            };

            var documento = LimparDocumento(request.DestinatarioCpfCnpj);
            if (documento.Length > 11)
            {
                destinatario.CNPJ = documento;
            }
            else if (documento.Length > 0)
            {
                destinatario.CPF = documento;
            }

            return destinatario;
        }

        private async Task<SalaoConfigFiscal> ObterConfigAtualizadaAsync(int idSalao, NotaFiscalAvulsaRequest request)
        {
            var config = await _configHandler.ObterPorSalaoAsync(idSalao) ?? new SalaoConfigFiscal
            {
                IdSalao = idSalao,
                Ambiente = request.Ambiente
            };

            config.Cnpj = !string.IsNullOrWhiteSpace(request.EmitenteCnpj) ? LimparDocumento(request.EmitenteCnpj) : config.Cnpj;
            config.RazaoSocial = !string.IsNullOrWhiteSpace(request.EmitenteNome) ? request.EmitenteNome : config.RazaoSocial;
            config.InscricaoEstadual = !string.IsNullOrWhiteSpace(request.EmitenteIE) ? request.EmitenteIE : config.InscricaoEstadual;
            config.InscricaoMunicipal = !string.IsNullOrWhiteSpace(request.EmitenteIM) ? request.EmitenteIM : config.InscricaoMunicipal;
            config.Ambiente = request.Ambiente;
            config.RegimeTributario = request.EmitenteCRT > 0 ? request.EmitenteCRT : (config.RegimeTributario == 0 ? 1 : config.RegimeTributario);
            config.CodigoMunicipioIBGE = request.EmitenteCodMun > 0 ? request.EmitenteCodMun : config.CodigoMunicipioIBGE;
            config.EnderecoLogradouro = request.EmitenteLogradouro ?? config.EnderecoLogradouro;
            config.EnderecoNumero = request.EmitenteNumero ?? config.EnderecoNumero;
            config.EnderecoBairro = request.EmitenteBairro ?? config.EnderecoBairro;
            config.EnderecoCep = !string.IsNullOrWhiteSpace(request.EmitenteCep) ? LimparCep(request.EmitenteCep) : config.EnderecoCep;
            config.EnderecoCidade = request.EmitenteCidade ?? config.EnderecoCidade;
            config.EnderecoUF = request.EmitenteUF ?? config.EnderecoUF;
            config.SerieNFCe = request.Serie;
            config.NumeroNFCe = request.Numero;
            config.SerieNFSe = request.Serie;
            config.NumeroNFSe = request.Numero;
            config.DataAtualizacao = DateTime.Now;

            if (config.CodigoUFIBGE == 0)
            {
                config.CodigoUFIBGE = ObterCodigoUf(request.EmitenteUF ?? config.EnderecoUF);
            }

            if (!string.IsNullOrWhiteSpace(config.Cnpj) && !string.IsNullOrWhiteSpace(config.RazaoSocial))
            {
                await _configHandler.SalvarAsync(config);
            }

            return config;
        }

        private async Task<X509Certificate2?> ObterCertificadoAsync(int idSalao, NotaFiscalAvulsaRequest request, SalaoConfigFiscal config)
        {
            byte[]? pfxBytes = request.CertificadoPfxBytes;
            string? senha = request.CertificadoSenha;

            if (pfxBytes != null && pfxBytes.Length > 0)
            {
                config.CertificadoPfx = pfxBytes;
                if (!string.IsNullOrWhiteSpace(senha))
                {
                    config.CertificadoSenha = _criptoService.Criptografar(senha);
                }

                config.DataAtualizacao = DateTime.Now;
                await _configHandler.SalvarAsync(config);
            }
            else if (config.CertificadoPfx != null)
            {
                pfxBytes = config.CertificadoPfx;
                if (string.IsNullOrWhiteSpace(senha) && config.CertificadoSenha != null)
                {
                    senha = _criptoService.Descriptografar(config.CertificadoSenha);
                }
            }

            if (pfxBytes == null || string.IsNullOrWhiteSpace(senha))
            {
                return null;
            }

            return new X509Certificate2(pfxBytes, senha, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);
        }

        public static string InferirTipoNota(string modelo) => modelo switch
        {
            "65" => "NFC-e",
            "NFSE" => "NFS-e",
            _ => "NF-e"
        };

        public static bool EhRetornoAutorizado(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }

            return xml.Contains("cStat>100<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("cStat>101<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("cStat>135<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("cStat>136<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("Autorizado", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("sucesso", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("chNFSe", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EhRetornoCancelado(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }

            return xml.Contains("cStat>101<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("cStat>135<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("cStat>136<", StringComparison.OrdinalIgnoreCase) ||
                   xml.Contains("Cancelad", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EhStatusEventoAutorizado(string? status, string? xmlRetorno)
        {
            if (!string.IsNullOrWhiteSpace(status) &&
                (status.Contains("135", StringComparison.OrdinalIgnoreCase) ||
                 status.Contains("101", StringComparison.OrdinalIgnoreCase) ||
                 status.Contains("sucesso", StringComparison.OrdinalIgnoreCase) ||
                 status.Contains("autorizad", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return EhRetornoCancelado(xmlRetorno) || EhRetornoAutorizado(xmlRetorno);
        }

        public static bool EhCancelamentoNfseJaVinculado(string? status, string? xmlRetorno)
        {
            var conteudo = PrimeiroValorPreenchido(status, ExtrairMensagemRetorno(xmlRetorno), xmlRetorno) ?? string.Empty;
            return conteudo.Contains("E0840", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("já está vinculado", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("já está vinculado", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EhErroConsultaChaveNfse(string? xmlRetorno)
        {
            var conteudo = PrimeiroValorPreenchido(ExtrairMensagemRetorno(xmlRetorno), xmlRetorno) ?? string.Empty;
            return conteudo.Contains("E2406", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("chave de acesso consultada deve conter 50 números", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("chave de acesso consultada deve conter 50 numeros", StringComparison.OrdinalIgnoreCase);
        }

        public static string? ExtrairIdDps(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                foreach (var tag in new[] { "infDPS", "InfDPS" })
                {
                    var nodes = doc.GetElementsByTagName(tag);
                    if (nodes.Count == 0)
                    {
                        continue;
                    }

                    foreach (XmlNode node in nodes)
                    {
                        var id = node.Attributes?["Id"]?.Value;
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            return id;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        public static string ExtrairProtocolo(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return "Vazio";
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var nProt = doc.GetElementsByTagName("nProt");
                if (nProt.Count > 0)
                {
                    return nProt[0]!.InnerText;
                }

                var cStat = doc.GetElementsByTagName("cStat");
                var xMotivo = doc.GetElementsByTagName("xMotivo");
                if (cStat.Count > 0 && xMotivo.Count > 0)
                {
                    return $"[{cStat[0]!.InnerText}] {xMotivo[0]!.InnerText}";
                }

                if (xMotivo.Count > 0)
                {
                    return xMotivo[0]!.InnerText;
                }

                var mensagem = ExtrairPrimeiroValor(doc, "Mensagem", "mensagem", "MensagemRetorno", "Descricao", "descricao");
                if (!string.IsNullOrWhiteSpace(mensagem))
                {
                    return mensagem;
                }

                return "Verifique o XML de retorno";
            }
            catch
            {
                return "Erro ao ler resposta do provedor fiscal";
            }
        }

        public static string? ExtrairChaveNfse(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var chaveTag = ChaveNfseValidaOuNula(ExtrairPrimeiroValor(doc, "chNFSe", "ChNFSe", "chNfse", "ChNfse"));
                if (!string.IsNullOrWhiteSpace(chaveTag))
                {
                    return chaveTag;
                }

                var infNfse = doc.GetElementsByTagName("infNFSe");
                if (infNfse.Count > 0)
                {
                    var id = infNfse[0]?.Attributes?["Id"]?.Value;
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        var chaveId = id.StartsWith("NFS", StringComparison.OrdinalIgnoreCase)
                            ? id.Substring(3)
                            : id;
                        return ChaveNfseValidaOuNula(chaveId);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string? ExtrairNumeroNfse(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return ExtrairPrimeiroValor(doc, "nNFSe", "NNFSe", "nNfse", "numero", "Numero");
            }
            catch
            {
                return null;
            }
        }

        public static string ClassificarStatusPorXml(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return NotaFiscalStatus.Rejeitada;
            }

            if (EhRetornoCancelado(xml))
            {
                return NotaFiscalStatus.Cancelada;
            }

            if (EhRetornoAutorizado(xml))
            {
                return NotaFiscalStatus.Autorizada;
            }

            return NotaFiscalStatus.Rejeitada;
        }

        public static string? ExtrairCodigoStatus(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return ExtrairPrimeiroValor(doc, "cStat", "Codigo", "codigo");
            }
            catch
            {
                return null;
            }
        }

        public static string ExtrairMensagemRetorno(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return string.Empty;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var codigo = ExtrairPrimeiroValor(doc, "cStat", "Codigo", "codigo");
                var motivo = ExtrairPrimeiroValor(doc, "xMotivo", "Mensagem", "mensagem", "MensagemRetorno", "Descricao", "descricao", "xMsg");

                if (!string.IsNullOrWhiteSpace(codigo) && !string.IsNullOrWhiteSpace(motivo))
                {
                    return $"[{codigo}] {motivo}";
                }

                return motivo ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string CoalesceMensagemFiscal(string? mensagem, string? xml)
        {
            if (!string.IsNullOrWhiteSpace(mensagem) &&
                !string.Equals(mensagem, "Vazio", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mensagem, "Verifique o XML de retorno", StringComparison.OrdinalIgnoreCase))
            {
                return mensagem;
            }

            var retorno = ExtrairMensagemRetorno(xml);
            return string.IsNullOrWhiteSpace(retorno) ? "Sem detalhe adicional retornado pelo provedor fiscal." : retorno;
        }

        public static string MontarMensagemOperacao(string operacao, string? status, string? xmlRetorno)
        {
            var detalhe = CoalesceMensagemFiscal(status, xmlRetorno);
            return $"{operacao} processada: {detalhe}";
        }

        public static void ValidarFaixaInutilizacao(int ano, int serie, int numInicial, int numFinal, string? tipoNota)
        {
            var anoAtual = DateTime.Now.Year;
            if (ano < 2000 || ano > anoAtual + 1)
            {
                throw new InvalidOperationException("Informe um ano válido para inutilização.");
            }

            if (serie <= 0)
            {
                throw new InvalidOperationException("A série deve ser maior que zero.");
            }

            if (numInicial <= 0 || numFinal <= 0)
            {
                throw new InvalidOperationException("Os números da faixa devem ser maiores que zero.");
            }

            if (numFinal < numInicial)
            {
                throw new InvalidOperationException("O número final não pode ser menor que o número inicial.");
            }

            if (!string.Equals(tipoNota, "NF-e", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tipoNota, "NFC-e", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A inutilização avulsa suporta apenas NF-e e NFC-e.");
            }
        }

        public static void ValidarCartaCorrecaoParaTipoNota(string? tipoNota)
        {
            if (string.Equals(tipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Carta de correção não está disponível para NFS-e na tela avulsa.");
            }
        }

        public static NotaFiscalAcoesDisponiveis ObterAcoesDisponiveis(NotaFiscal nota)
        {
            var autorizada = string.Equals(nota.Status, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase);
            var cancelada = string.Equals(nota.Status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase);

            return new NotaFiscalAcoesDisponiveis
            {
                ChaveFiscal = ObterChaveFiscal(nota),
                ClasseStatus = ObterClasseStatus(nota.Status),
                PodeBaixarXml = !string.IsNullOrWhiteSpace(nota.XmlEnvio) || !string.IsNullOrWhiteSpace(nota.XmlRetorno),
                TipoXmlPreferencial = !string.IsNullOrWhiteSpace(nota.XmlRetorno) ? "retorno" : "envio",
                PodeGerarPdf = autorizada || cancelada,
                PodeCancelar = autorizada,
                PodeEnviarEmail = autorizada || cancelada,
                PodeCartaCorrecao = autorizada && !string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase)
            };
        }

        public static string ObterChaveFiscal(NotaFiscal nota)
        {
            if (string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
            {
                return PrimeiroValorPreenchido(
                    ChaveNfseValidaOuNula(nota.ChaveAcessoNacional),
                    ChaveNfseValidaOuNula(nota.ChaveAcesso),
                    ExtrairChaveNfse(nota.XmlRetorno),
                    ExtrairChaveNfse(nota.XmlEnvio));
            }

            return PrimeiroValorPreenchido(nota.ChaveAcesso, nota.ChaveAcessoNacional, nota.ProtocoloAutorizacao);
        }

        public static string ObterClasseStatus(string? status)
        {
            if (string.Equals(status, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase))
            {
                return "bg-success";
            }

            if (string.Equals(status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
            {
                return "bg-danger";
            }

            return "bg-warning";
        }

        private static string PrimeiroValorPreenchido(params string?[] valores)
        {
            foreach (var valor in valores)
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    return valor;
                }
            }

            return string.Empty;
        }

        private async Task<NotaFiscal?> ObterNotaPorIdentificadorAsync(int idSalao, string identificadorFiscal)
        {
            var nota = await _notaHandler.ObterPorChaveAsync(identificadorFiscal, idSalao);
            if (nota != null)
            {
                await SincronizarChaveNfseSeNecessarioAsync(nota);
                await ReconciliarStatusPersistidoAsync(idSalao, nota);
                return nota;
            }

            var notas = await _notaHandler.ListarPorSalaoAsync(idSalao);
            nota = notas.FirstOrDefault(n =>
                string.Equals(ObterChaveFiscal(n), identificadorFiscal, StringComparison.OrdinalIgnoreCase));

            if (nota != null)
            {
                await SincronizarChaveNfseSeNecessarioAsync(nota);
                await ReconciliarStatusPersistidoAsync(idSalao, nota);
            }

            return nota;
        }

        private async Task ReconciliarStatusPersistidoAsync(int idSalao, NotaFiscal nota)
        {
            if (!string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var statusOriginal = nota.Status;
            var xmlRetornoOriginal = nota.XmlRetorno;
            var justificativaOriginal = nota.JustificativaRejeicao;
            var protocoloOriginal = nota.ProtocoloAutorizacao;

            var eventosLocais = await _eventoHandler.ListarPorNotaAsync(nota.IdNotaFiscal);
            var eventoLocalConcluido = eventosLocais
                .OrderByDescending(e => e.DataRegistro)
                .FirstOrDefault(EhEventoCancelamentoNfseConcluido);

            if (eventoLocalConcluido != null)
            {
                nota.Status = NotaFiscalStatus.Cancelada;
                nota.JustificativaRejeicao = null;
                if (!string.IsNullOrWhiteSpace(eventoLocalConcluido.XmlRetorno))
                {
                    nota.XmlRetorno = eventoLocalConcluido.XmlRetorno;
                }
            }
            else
            {
                var logs = await _logHandler.ListarPorNotaFiscalAsync(nota.IdNotaFiscal, idSalao);
                if (EhRetornoAutorizado(nota.XmlRetorno) || logs.Any(LogIndicaAutorizacaoNfse))
                {
                    nota.Status = NotaFiscalStatus.Autorizada;
                    nota.JustificativaRejeicao = null;
                }
            }

            nota.ProtocoloAutorizacao = SanitizarProtocoloFiscal(nota.ProtocoloAutorizacao);

            if (!string.Equals(statusOriginal, nota.Status, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(xmlRetornoOriginal, nota.XmlRetorno, StringComparison.Ordinal) ||
                !string.Equals(justificativaOriginal, nota.JustificativaRejeicao, StringComparison.Ordinal) ||
                !string.Equals(protocoloOriginal, nota.ProtocoloAutorizacao, StringComparison.Ordinal))
            {
                nota.DataAtualizacao = DateTime.Now;
                await _notaHandler.AtualizarAsync(nota);
                await _logHandler.LogarEtapaAsync(
                    idSalao,
                    nota.IdAgendamento,
                    nota.IdNotaFiscal,
                    "RECONCILIACAO_STATUS_NFSE",
                    $"Status reconciliado para {nota.Status}.",
                    nota.XmlRetorno);
            }
        }

        private static bool LogIndicaAutorizacaoNfse(NotaFiscalLog? log)
        {
            if (log == null)
            {
                return false;
            }

            var etapa = log.TipoEvento ?? string.Empty;
            var mensagem = log.Mensagem ?? string.Empty;
            var conteudo = $"{etapa} {mensagem}";

            return conteudo.Contains("Retorno PGN Sucesso", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("Emissão Efetuada", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("Emissao Efetuada", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("status Autorizada", StringComparison.OrdinalIgnoreCase) ||
                   conteudo.Contains("emitida com sucesso", StringComparison.OrdinalIgnoreCase);
        }

        public static string? SanitizarProtocoloFiscal(string? protocolo)
        {
            if (string.IsNullOrWhiteSpace(protocolo))
            {
                return null;
            }

            var valor = protocolo.Trim();
            if (string.Equals(valor, "Vazio", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("E2406", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("chave de acesso consultada", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("E0840", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("Sistema Nacional NFS-e", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("evento de cancelamento", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("impedindo sua recepcao", StringComparison.OrdinalIgnoreCase) ||
                valor.Contains("impedindo sua recepção", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return valor;
        }

        private async Task<bool> SincronizarCancelamentoNfseSeExistirAsync(int idSalao, SalaoConfigFiscal config, NotaFiscal nota, string chaveAcesso)
        {
            if (!string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var eventosLocais = await _eventoHandler.ListarPorNotaAsync(nota.IdNotaFiscal);
            var eventoLocalConcluido = eventosLocais
                .OrderByDescending(e => e.DataRegistro)
                .FirstOrDefault(EhEventoCancelamentoNfseConcluido);

            if (eventoLocalConcluido != null)
            {
                nota.Status = NotaFiscalStatus.Cancelada;
                nota.XmlRetorno = !string.IsNullOrWhiteSpace(eventoLocalConcluido.XmlRetorno) ? eventoLocalConcluido.XmlRetorno : nota.XmlRetorno;
                await _notaHandler.AtualizarAsync(nota);
                await _logHandler.LogarEtapaAsync(
                    idSalao,
                    nota.IdAgendamento,
                    nota.IdNotaFiscal,
                    "SINCRONIZACAO_CANCELAMENTO_NFSE",
                    eventoLocalConcluido.Status ?? "Evento local de cancelamento encontrado.",
                    eventoLocalConcluido.XmlRetorno);

                return true;
            }

            var consultaEvento = await _fiscalAction.ConsultarEventoCancelamentoNfseAsync(config, chaveAcesso);
            if (!consultaEvento.EncontrouEvento)
            {
                return false;
            }

            nota.Status = NotaFiscalStatus.Cancelada;
            nota.XmlRetorno = !string.IsNullOrWhiteSpace(consultaEvento.XmlRetorno) ? consultaEvento.XmlRetorno : nota.XmlRetorno;
            await _notaHandler.AtualizarAsync(nota);
            await _logHandler.LogarEtapaAsync(
                idSalao,
                nota.IdAgendamento,
                nota.IdNotaFiscal,
                "SINCRONIZACAO_CANCELAMENTO_NFSE",
                consultaEvento.Mensagem ?? "Evento de cancelamento encontrado no provedor nacional.",
                consultaEvento.XmlRetorno);

            return true;
        }

        private static bool EhEventoCancelamentoNfseConcluido(NotaFiscalEvento? evento)
        {
            if (evento == null)
            {
                return false;
            }

            if (!string.Equals(evento.TipoEvento, "Cancelamento NFS-e Nacional", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return EhStatusEventoAutorizado(evento.Status, evento.XmlRetorno) ||
                   EhCancelamentoNfseJaVinculado(evento.Status, evento.XmlRetorno);
        }

        private async Task SincronizarChaveNfseSeNecessarioAsync(NotaFiscal nota)
        {
            if (!string.Equals(nota.TipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var chaveCorreta = PrimeiroValorPreenchido(
                ExtrairChaveNfse(nota.XmlRetorno),
                ExtrairChaveNfse(nota.XmlEnvio),
                ChaveNfseValidaOuNula(nota.ChaveAcessoNacional),
                ChaveNfseValidaOuNula(nota.ChaveAcesso));

            if (string.IsNullOrWhiteSpace(chaveCorreta))
            {
                return;
            }

            var alterou = false;
            if (!string.Equals(nota.ChaveAcessoNacional, chaveCorreta, StringComparison.Ordinal))
            {
                nota.ChaveAcessoNacional = chaveCorreta;
                alterou = true;
            }

            if (alterou)
            {
                await _notaHandler.AtualizarAsync(nota);
            }
        }

        private static string? ChaveNfseValidaOuNula(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var digits = new string(valor.Where(char.IsDigit).ToArray());
            return digits.Length == 50 ? digits : null;
        }

        private static string? ExtrairPrimeiroValorXml(string? xml, params string[] tags)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return ExtrairPrimeiroValor(doc, tags);
            }
            catch
            {
                return null;
            }
        }

        public static void ValidarNotaParaCancelamento(NotaFiscal nota)
        {
            if (string.Equals(nota.Status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A nota já está cancelada.");
            }

            if (string.Equals(nota.Status, NotaFiscalStatus.Inutilizada, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Notas inutilizadas não podem ser canceladas.");
            }

            if (!string.Equals(nota.Status, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Somente notas autorizadas podem ser canceladas na tela avulsa.");
            }
        }

        public static void ValidarNotaParaCartaCorrecao(NotaFiscal nota)
        {
            if (!string.Equals(nota.Status, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A carta de correção exige uma nota autorizada.");
            }
        }

        public static void ValidarChaveFiscal(string? chaveAcesso, string operacao)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso))
            {
                throw new InvalidOperationException($"Informe a chave fiscal para {operacao}.");
            }
        }

        public static void ValidarEmailDestino(string? emailDestino)
        {
            if (string.IsNullOrWhiteSpace(emailDestino))
            {
                throw new InvalidOperationException("Informe o e-mail de destino para enviar a nota.");
            }

            try
            {
                _ = new MailAddress(emailDestino.Trim());
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Informe um e-mail de destino válido para enviar a nota.");
            }
        }

        public static void ValidarTipoXmlSolicitado(string? tipo)
        {
            if (!string.Equals(tipo, "envio", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tipo, "retorno", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tipo de XML inválido. Use envio ou retorno.");
            }
        }

        public static void ValidarTextoMinimo(string? texto, int tamanhoMinimo, string mensagemErro)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < tamanhoMinimo)
            {
                throw new InvalidOperationException(mensagemErro);
            }
        }

        public static NotaFiscalRetornoResumo CriarResumoRetorno(string? xml, string? protocolo = null, string? chaveAcesso = null, string? statusAtual = null)
        {
            var statusFiscal = !string.IsNullOrWhiteSpace(statusAtual)
                ? statusAtual
                : ClassificarStatusPorXml(xml);

            var mensagemRetorno = ExtrairMensagemRetorno(xml);
            var protocoloFinal = SanitizarProtocoloFiscal(!string.IsNullOrWhiteSpace(protocolo) ? protocolo : ExtrairProtocolo(xml));
            var identificadorFiscal = PrimeiroValorPreenchido(chaveAcesso, ExtrairChaveNfse(xml), ExtrairNumeroNfse(xml), protocoloFinal);
            var consultaInconclusiva = EhErroConsultaChaveNfse(xml);

            if (consultaInconclusiva &&
                (string.Equals(statusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(statusFiscal, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase)))
            {
                mensagemRetorno = string.Equals(statusFiscal, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase)
                    ? "Status reconciliado a partir do evento de cancelamento já registrado."
                    : "Status reconciliado a partir das evidências locais da emissão autorizada.";
            }

            return new NotaFiscalRetornoResumo
            {
                StatusFiscal = statusFiscal,
                CodigoStatus = consultaInconclusiva &&
                              (string.Equals(statusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(statusFiscal, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
                    ? "LOCAL_SYNC"
                    : ExtrairCodigoStatus(xml),
                MensagemRetorno = string.IsNullOrWhiteSpace(mensagemRetorno) ? null : mensagemRetorno,
                Protocolo = protocoloFinal,
                ChaveAcesso = string.IsNullOrWhiteSpace(identificadorFiscal) ? null : identificadorFiscal,
                PodeCancelar = string.Equals(statusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase),
                OperacaoConcluida =
                    string.Equals(statusFiscal, NotaFiscalStatus.Autorizada, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(statusFiscal, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(statusFiscal, NotaFiscalStatus.Inutilizada, StringComparison.OrdinalIgnoreCase)
            };
        }

        private static string? ExtrairPrimeiroValor(XmlDocument doc, params string[] nomesTags)
        {
            foreach (var nome in nomesTags)
            {
                var nodes = doc.GetElementsByTagName(nome);
                if (nodes.Count > 0 && !string.IsNullOrWhiteSpace(nodes[0]?.InnerText))
                {
                    return nodes[0]!.InnerText.Trim();
                }

                var xpath = $"//*[translate(local-name(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') = '{nome.ToLowerInvariant()}']";
                var node = doc.SelectSingleNode(xpath);
                if (!string.IsNullOrWhiteSpace(node?.InnerText))
                {
                    return node!.InnerText.Trim();
                }
            }

            return null;
        }

        private static string LimparDocumento(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Replace(".", string.Empty, StringComparison.Ordinal)
                    .Replace("/", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal)
                    .Trim();

        private static string LimparCep(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? "00000000"
                : valor.Replace("-", string.Empty, StringComparison.Ordinal).Trim();

        private static UFBrasil GetUfBrasil(string? uf)
        {
            if (!string.IsNullOrWhiteSpace(uf) && Enum.TryParse(uf, true, out UFBrasil resultado))
            {
                return resultado;
            }

            return UFBrasil.MG;
        }

        private static int ObterCodigoUf(string? uf)
        {
            return (int)GetUfBrasil(uf);
        }
    }
}
