using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Handlers;
using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using System.Xml;
using System.Text;
using CorteCor.Services;

namespace CorteCor.Pages.Fiscal
{
    [Authorize]
    public class NotaFiscalAvulsaModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly NotaFiscalLogHandler _logHandler;
        private readonly SalaoConfigFiscalHandler _configHandler;
        private readonly CertificadoFiscalFactory _certFactory;
        private readonly ICriptografiaService _criptoService;
        private readonly FiscalBuilderService _fiscalBuilder;
        private readonly NFSeEmissorService _nfseEmissor;

        public NotaFiscalAvulsaModel(
            IDatabaseHandler dbHandler, 
            NotaFiscalLogHandler logHandler, 
            SalaoConfigFiscalHandler configHandler,
            CertificadoFiscalFactory certFactory,
            ICriptografiaService criptoService,
            FiscalBuilderService fiscalBuilder,
            NFSeEmissorService nfseEmissor)
        {
            _dbHandler = dbHandler;
            _logHandler = logHandler;
            _configHandler = configHandler;
            _certFactory = certFactory;
            _criptoService = criptoService;
            _fiscalBuilder = fiscalBuilder;
            _nfseEmissor = nfseEmissor;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            // Certificado & Ambiente
            [Display(Name = "Certificado Digital (.pfx)")]
            public IFormFile? CertificadoFile { get; set; }
            [Display(Name = "Senha do Certificado")]
            [DataType(DataType.Password)]
            public string? CertificadoSenha { get; set; }
            [Display(Name = "Ambiente")]
            public int Ambiente { get; set; } = 2; // 1: Produção, 2: Homologação

            // Identificação da Nota
            [Required]
            [Display(Name = "Modelo")]
            public string Modelo { get; set; } = "55"; // 55: NFe, 65: NFCe, NFSe: Custom
            [Required]
            [Display(Name = "Natureza da Operação")]
            public string NaturezaOperacao { get; set; } = "Venda de Mercadoria/Serviço";
            public int Serie { get; set; } = 1;
            public int Numero { get; set; } = 1;
            public DateTime DataEmissao { get; set; } = DateTime.Now;

            // Emitente
            [Required]
            [Display(Name = "CNPJ do Emitente")]
            public string? EmitenteCnpj { get; set; }
            [Required]
            [Display(Name = "Razão Social")]
            public string? EmitenteNome { get; set; }
            [Display(Name = "Inscrição Estadual")]
            public string? EmitenteIE { get; set; }
            [Display(Name = "Inscrição Municipal")]
            public string? EmitenteIM { get; set; }
            public int EmitenteCRT { get; set; } = 1; // 1: Simples Nacional

            // Endereço Emitente
            public string? EmitenteLogradouro { get; set; }
            public string? EmitenteNumero { get; set; }
            public string? EmitenteBairro { get; set; }
            public string? EmitenteCep { get; set; }
            public string? EmitenteCidade { get; set; }
            public string? EmitenteUF { get; set; }
            public int EmitenteCodMun { get; set; }

            // Destinatário
            [Required]
            [Display(Name = "CPF/CNPJ do Destinatário")]
            public string? DestinatarioCpfCnpj { get; set; }
            [Required]
            [Display(Name = "Nome / Razão Social")]
            public string? DestinatarioNome { get; set; }
            [Display(Name = "Inscrição Estadual")]
            public string? DestinatarioIE { get; set; }
            
            // Endereço Destinatário
            public string? DestinatarioLogradouro { get; set; }
            public string? DestinatarioNumero { get; set; }
            public string? DestinatarioBairro { get; set; }
            public string? DestinatarioCep { get; set; }
            public string? DestinatarioCidade { get; set; }
            public string? DestinatarioUF { get; set; }
            public int DestinatarioCodMun { get; set; }

            // Itens
            public List<NotaFiscalAvulsaItem> Itens { get; set; } = new();
        }

        public class NotaFiscalAvulsaItem
        {
            public string CProd { get; set; } = "";
            public string XProd { get; set; } = "";
            public string NCM { get; set; } = "00";
            public string CFOP { get; set; } = "5102";
            public string uCom { get; set; } = "UN";
            public decimal qCom { get; set; } = 1;
            public decimal vUnCom { get; set; } = 0;
            public decimal vProd { get; set; } = 0;
            
            // Impostos Simplificados
            public string CSOSN { get; set; } = "102";
            public decimal vTotTrib { get; set; } = 0;

            // NFSe specific
            public string? CodigoTributacao { get; set; }
            public decimal AliquotaISS { get; set; } = 5;
        }

        public string Mensagem { get; set; }
        public string MensagemTipo { get; set; } = "info";
        public string ProtocoloAutorizacao { get; set; }
        public string XmlRetorno { get; set; }
        public string XmlEnvio { get; set; }
        public string ChaveAcesso { get; set; }
        public bool HasSavedCertificate { get; set; }
        public List<string> Logs { get; set; } = new();

        public List<SelectModel> ModelosList { get; set; } = new()
        {
            new SelectModel { Value = "55", Text = "55 - NF-e (Mercadorias)" },
            new SelectModel { Value = "65", Text = "65 - NFC-e (Cupom Fiscal)" },
            new SelectModel { Value = "NFSE", Text = "NFS-e (Serviços)" }
        };

        public List<string> NaturezasList { get; set; } = new()
        {
            "Venda de mercadoria",
            "Prestação de serviço",
            "Devolução de compra para comercialização",
            "Remessa para conserto",
            "Remessa para demonstração",
            "Brinde",
            "Doação",
            "Outras"
        };

        public List<SelectModel> MunicipiosList { get; set; } = new()
        {
            new SelectModel { Value = "5300108", Text = "Brasília - DF" },
            new SelectModel { Value = "3550308", Text = "São Paulo - SP" },
            new SelectModel { Value = "3304557", Text = "Rio de Janeiro - RJ" },
            new SelectModel { Value = "3106200", Text = "Belo Horizonte - MG" },
            new SelectModel { Value = "2927408", Text = "Salvador - BA" },
            new SelectModel { Value = "2304400", Text = "Fortaleza - CE" },
            new SelectModel { Value = "4106902", Text = "Curitiba - PR" },
            new SelectModel { Value = "1302603", Text = "Manaus - AM" },
            new SelectModel { Value = "2611606", Text = "Recife - PE" },
            new SelectModel { Value = "4314902", Text = "Porto Alegre - RS" },
            new SelectModel { Value = "3143302", Text = "Montes Claros - MG" }
        };

        public class SelectModel
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }

        public async Task OnGetAsync()
        {
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            var config = await _configHandler.ObterPorSalaoAsync(idSalao);

            // Dados do Emitente (Tonni Tecnologia - Padrão solicitado)
            Input.EmitenteNome = "Tonni Tecnologia";
            Input.EmitenteCnpj = "49.358.717/0001-07";
            Input.EmitenteIM = "101477";
            Input.EmitenteCRT = 1; // Simples Nacional
            Input.EmitenteLogradouro = "Rua M";
            Input.EmitenteNumero = "601";
            Input.EmitenteBairro = "São Geraldo II";
            Input.EmitenteCidade = "Montes Claros";
            Input.EmitenteUF = "MG";
            Input.EmitenteCodMun = 3143302; // Montes Claros

            // Dados do Destinatário (Jeane Ferreira da Silva - Padrão solicitado)
            Input.DestinatarioNome = "Jeane Ferreira da Silva";
            Input.DestinatarioCpfCnpj = "05528366640";
            Input.DestinatarioLogradouro = "Rua Doutor Santos";
            Input.DestinatarioNumero = "123";
            Input.DestinatarioBairro = "Centro";
            Input.DestinatarioCep = "39400-001";
            Input.DestinatarioCidade = "Montes Claros";
            Input.DestinatarioUF = "MG";
            Input.DestinatarioCodMun = 3143302;

            if (config != null)
            {
                Input.Ambiente = config.Ambiente;
                Input.Serie = config.SerieNFCe;
                Input.Numero = config.NumeroNFCe;
                Input.EmitenteIE = config.InscricaoEstadual;
                Input.EmitenteIM = config.InscricaoMunicipal;
                HasSavedCertificate = config.CertificadoPfx != null && config.CertificadoPfx.Length > 0;
            }

            if (string.IsNullOrWhiteSpace(Input.EmitenteIE))
            {
                Input.EmitenteIE = null; // Let the model stay null/empty to test Sefaz actual behavior
            }

            // Adiciona um item vazio inicial
            if (Input.Itens.Count == 0)
            {
                Input.Itens.Add(new NotaFiscalAvulsaItem { 
                    CProd = "1",
                    XProd = "Prestação de Serviço de Tecnologia",
                    qCom = 1,
                    vUnCom = 100,
                    CodigoTributacao = "060101",
                    AliquotaISS = 5
                });
            }
        }

        public async Task<IActionResult> OnPostConsultarAsync()
        {
            if (string.IsNullOrWhiteSpace(ChaveAcesso))
            {
                Mensagem = "Informe a Chave de Acesso para consultar.";
                MensagemTipo = "warning";
                return Page();
            }

            try
            {
                int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                X509Certificate2 cert = await ObterCertificadoAsync(idSalao);
                
                if (cert == null)
                {
                    Mensagem = "Certificado digital não foi carregado. Favor fazer o upload do arquivo .pfx e informar a senha.";
                    MensagemTipo = "danger";
                    return Page();
                }

                // 2. Configurar Serviço de Consulta
                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = Input.Modelo == "65" ? Unimake.Business.DFe.Servicos.TipoDFe.NFCe : Unimake.Business.DFe.Servicos.TipoDFe.NFe,
                    CertificadoDigital = cert,
                    TipoAmbiente = Input.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao
                };

                var consultaDto = new ConsSitNFe
                {
                    Versao = "4.00",
                    TpAmb = Input.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                    ChNFe = ChaveAcesso
                };

                var servico = new Unimake.Business.DFe.Servicos.NFe.ConsultaProtocolo(consultaDto, configuracao);
                Logs.Add($"Consultando chave {ChaveAcesso}...");
                servico.Executar();

                XmlRetorno = servico.RetornoWSString;
                ProtocoloAutorizacao = ExtrairProtocolo(XmlRetorno);
                Mensagem = "Consulta realizada com sucesso.";
                MensagemTipo = "success";
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao consultar: {ex.Message}";
                MensagemTipo = "danger";
                Logs.Add($"ERRO: {ex.Message}");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            Guid notaLogId = Guid.NewGuid();
            
            await _logHandler.LogarEtapaAsync(idSalao, null, notaLogId, "INICIO_AVULSA_TRANSMISSAO", "Iniciando transmissão de nota fiscal avulsa", null);
            Logs.Add("Processo de transmissão iniciado...");

            try
            {
                X509Certificate2 cert = await ObterCertificadoAsync(idSalao);
                if (cert == null)
                {
                    Mensagem = "Certificado digital não selecionado ou senha inválida.";
                    MensagemTipo = "danger";
                    return Page();
                }

                // 2. Transmissão conforme o Modelo
                if (Input.Modelo == "55" || Input.Modelo == "65")
                {
                    await TransmitirNFeAsync(idSalao, notaLogId, cert);
                }
                else if (Input.Modelo == "NFSE")
                {
                    await TransmitirNFSeAsync(idSalao, notaLogId, cert);
                }

                TempData["SuccessMessage"] = "Processamento SEFAZ concluído!";
            }
            catch (Exception ex)
            {
                var fullError = ex.Message;
                if (ex.InnerException != null)
                {
                    fullError += " -> " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                        fullError += " -> " + ex.InnerException.InnerException.Message;
                }

                Logs.Add($"ERRO: {fullError}");
                await _logHandler.LogarEtapaAsync(idSalao, null, notaLogId, "ERRO_AVULSA", ex.ToString(), ex.StackTrace);
                Mensagem = $"Erro na transmissão: {fullError}";
                MensagemTipo = "danger";
            }

            return Page();
        }

        private async Task TransmitirNFeAsync(int idSalao, Guid notaLogId, X509Certificate2 cert)
        {
            var modeloDFe = Input.Modelo == "65" ? ModeloDFe.NFCe : ModeloDFe.NFe;
            
            var infNfe = new InfNFe
            {
                Versao = "4.00",
                Ide = new Ide
                {
                    CUF = GetUfBrasil(Input.EmitenteUF),
                    NatOp = Input.NaturezaOperacao,
                    Mod = modeloDFe,
                    Serie = Input.Serie,
                    NNF = Input.Numero,
                    DhEmi = DateTime.Now,
                    TpNF = TipoOperacao.Saida,
                    IdDest = Input.EmitenteUF == Input.DestinatarioUF ? DestinoOperacao.OperacaoInterna : DestinoOperacao.OperacaoInterestadual,
                    CMunFG = Input.EmitenteCodMun,
                    TpImp = modeloDFe == ModeloDFe.NFCe ? FormatoImpressaoDANFE.NFCe : FormatoImpressaoDANFE.NormalRetrato,
                    TpEmis = TipoEmissao.Normal,
                    TpAmb = Input.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                    FinNFe = FinalidadeNFe.Normal,
                    IndFinal = SimNao.Sim,
                    IndPres = IndicadorPresenca.OperacaoPresencial,
                    ProcEmi = ProcessoEmissao.AplicativoContribuinte,
                    VerProc = "CorteCor 1.0"
                },
                Emit = new Emit
                {
                    CNPJ = Input.EmitenteCnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                    XNome = Input.EmitenteNome,
                    IE = string.IsNullOrWhiteSpace(Input.EmitenteIE) ? null : Input.EmitenteIE,
                    CRT = (CRT)Input.EmitenteCRT,
                    EnderEmit = new EnderEmit
                    {
                        XLgr = Input.EmitenteLogradouro ?? "Rua Nao Informada",
                        Nro = Input.EmitenteNumero ?? "SN",
                        XBairro = Input.EmitenteBairro ?? "Bairro Nao Informado",
                        CMun = Input.EmitenteCodMun,
                        XMun = Input.EmitenteCidade ?? "Cidade Nao Informada",
                        UF = GetUfBrasil(Input.EmitenteUF),
                        CEP = (Input.EmitenteCep?.Replace("-", "")) ?? "00000000",
                        CPais = 1058,
                        XPais = "Brasil"
                    }
                }
            };

            var docDest = (Input.DestinatarioCpfCnpj?.Replace(".", "").Replace("/", "").Replace("-", "")) ?? "";
            
            infNfe.Dest = new Dest
            {
                XNome = (Input.Ambiente == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Input.DestinatarioNome) ?? "CONSUMIDOR",
                IndIEDest = IndicadorIEDestinatario.NaoContribuinte,
                EnderDest = new EnderDest
                {
                    XLgr = Input.DestinatarioLogradouro ?? "Logradouro nao informado",
                    Nro = Input.DestinatarioNumero ?? "SN",
                    XBairro = Input.DestinatarioBairro ?? "Bairro nao informado",
                    CMun = Input.DestinatarioCodMun,
                    XMun = Input.DestinatarioCidade ?? "Cidade nao informada",
                    UF = GetUfBrasil(Input.DestinatarioUF),
                    CEP = (Input.DestinatarioCep?.Replace("-", "")) ?? "00000000"
                }
            };

            if (docDest.Length > 11) infNfe.Dest.CNPJ = docDest;
            else if (docDest.Length > 0) infNfe.Dest.CPF = docDest;
            // Se docDest for vazio, não preenchemos CPF/CNPJ (venda para consumidor final sem doc)

            double totalBase = 0;
            infNfe.Det = new List<Det>();
            for (int i = 0; i < Input.Itens.Count; i++)
            {
                var item = Input.Itens[i];
                var vProd = (double)(item.qCom * item.vUnCom);
                totalBase += vProd;

                infNfe.Det.Add(new Det
                {
                    NItem = i + 1,
                    Prod = new Prod
                    {
                        CProd = string.IsNullOrWhiteSpace(item.CProd) ? "1" : item.CProd,
                        XProd = Input.Ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : item.XProd,
                        NCM = string.IsNullOrWhiteSpace(item.NCM) || item.NCM == "00" ? "99" : item.NCM, // 99 é genérico para serviços se NCM não existir
                        CFOP = string.IsNullOrWhiteSpace(item.CFOP) ? "5102" : item.CFOP,
                        UCom = item.uCom ?? "UN",
                        QCom = item.qCom,
                        VUnCom = item.vUnCom,
                        VProd = vProd,
                        CEAN = "SEM GTIN",
                        CEANTrib = "SEM GTIN",
                        UTrib = item.uCom ?? "UN",
                        QTrib = item.qCom,
                        VUnTrib = item.vUnCom,
                        IndTot = SimNao.Sim
                    },
                    Imposto = new Imposto
                    {
                        ICMS = new ICMS
                        {
                            ICMSSN102 = new ICMSSN102
                            {
                                Orig = OrigemMercadoria.Nacional,
                                CSOSN = item.CSOSN
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
                    VTotTrib = (double)Input.Itens.Sum(i => i.vTotTrib),
                    VBC = 0,
                    VICMS = 0,
                    VDesc = 0
                }
            };

            infNfe.Transp = new Transp
            {
                ModFrete = ModalidadeFrete.SemOcorrenciaTransporte
            };

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
                IdLote = "1",
                IndSinc = SimNao.Sim,
                NFe = new List<NFe> { nfe }
            };

            var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
            {
                TipoDFe = modeloDFe == ModeloDFe.NFCe ? Unimake.Business.DFe.Servicos.TipoDFe.NFCe : Unimake.Business.DFe.Servicos.TipoDFe.NFe,
                CertificadoDigital = cert,
                TipoAmbiente = Input.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                BuscarConfiguracaoPastaBase = false
            };

            if (modeloDFe == ModeloDFe.NFCe)
            {
                configuracao.CSC = "123456789012345678901234567890123456";
                configuracao.CSCIDToken = 1;
                configuracao.CodigoUF = (int)infNfe.Ide.CUF;
                configuracao.Definida = true;
                configuracao.VersaoQRCodeNFCe = 2;
            }

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var schemaPath = Path.Combine(baseDir, "Schemas");
                if (!Directory.Exists(schemaPath)) Directory.CreateDirectory(schemaPath);
                
                var targetFile = Path.Combine(schemaPath, "NFCe.enviNFe_v4.00.xsd");
                if (!System.IO.File.Exists(targetFile))
                {
                    var am = typeof(Unimake.Business.DFe.Servicos.NFe.Autorizacao).Assembly;
                    using (var stream = am.GetManifestResourceStream("Unimake.Business.DFe.Xml.Schemas.NFe.enviNFe_v4.00.xsd"))
                    {
                        if (stream != null)
                        {
                            using (var fileStream = System.IO.File.Create(targetFile))
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }
            catch { }

            // Forçar desligamento da validação de schema para evitar erro de resource na DLL
            var propValidar = configuracao.GetType().GetProperty("ValidarSchema");
            if (propValidar != null) propValidar.SetValue(configuracao, false);

            if (modeloDFe == ModeloDFe.NFCe)
            {
                var servico = new Unimake.Business.DFe.Servicos.NFCe.Autorizacao(enviNfe, configuracao);
                Logs.Add("Transmitindo NFC-e para SEFAZ...");
                servico.Executar();
                XmlRetorno = servico.RetornoWSString;
            }
            else
            {
                var servico = new Unimake.Business.DFe.Servicos.NFe.Autorizacao(enviNfe, configuracao);
                Logs.Add("Transmitindo NF-e para SEFAZ...");
                servico.Executar();
                XmlRetorno = servico.RetornoWSString;
            }

            ProtocoloAutorizacao = ExtrairProtocolo(XmlRetorno);


            XmlEnvio = enviNfe.GerarXML().OuterXml;
            ChaveAcesso = infNfe.Id?.Replace("NFe", "") ?? "";

            _logHandler.LogarEtapaAsync(idSalao, null, null, "RESPOSTA_SEFAZ", $"Protocolo: {ProtocoloAutorizacao}", XmlRetorno);
        }

        private async Task TransmitirNFSeAsync(int idSalao, Guid notaLogId, X509Certificate2 cert)
        {
            Logs.Add("Iniciando Transmissão NFSe Nacional...");
            
            // 1. Obter/Montar Configuração Fiscal
            var config = await _configHandler.ObterPorSalaoAsync(idSalao);
            if (config == null)
            {
                config = new SalaoConfigFiscal
                {
                    Cnpj = Input.EmitenteCnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                    RazaoSocial = Input.EmitenteNome,
                    Ambiente = Input.Ambiente,
                    CodigoMunicipioIBGE = Input.EmitenteCodMun,
                    SerieNFSe = Input.Serie,
                    NumeroNFSe = Input.Numero,
                    RegimeTributario = Input.EmitenteCRT,
                    InscricaoMunicipal = Input.EmitenteIM,
                    EnderecoCep = Input.EmitenteCep,
                    EnderecoLogradouro = Input.EmitenteLogradouro,
                    EnderecoNumero = Input.EmitenteNumero,
                    EnderecoBairro = Input.EmitenteBairro,
                    Telefone = "00000000000",
                    Email = "contato@corteecor.com.br",
                    IssExigibilidade = 1, // Exigível
                    IssRetido = 2, // Não Retido
                    RegimeEspecialTributacao = 0 // Nenhum
                };
            }

            // 2. Garantir Código IBGE e Emitente Válidos (para alinhar a view/cert com DB)
            config.Cnpj = Input.EmitenteCnpj;
            config.RazaoSocial = Input.EmitenteNome;
            config.InscricaoMunicipal = Input.EmitenteIM;

            if (config.CodigoMunicipioIBGE < 1000000)
            {
                config.CodigoMunicipioIBGE = Input.EmitenteCodMun > 0 ? Input.EmitenteCodMun : 3143302; // Default Montes Claros
            }

            // 3. Mapear Tomador
            var tomador = new CorteCor.Models.Pessoa
            {
                Nome = Input.DestinatarioNome,
                CpfCnpj = Input.DestinatarioCpfCnpj,
                Cep = Input.DestinatarioCep,
                Logradouro = Input.DestinatarioLogradouro,
                Numero = Input.DestinatarioNumero,
                Bairro = Input.DestinatarioBairro,
                Cidade = (Input.DestinatarioCodMun > 0 ? Input.DestinatarioCodMun : 3143302).ToString()
            };

            // 4. Mapear Serviço (Pega o primeiro item da lista da página avulsa)
            var itemAvulso = Input.Itens.FirstOrDefault() ?? new NotaFiscalAvulsaItem { XProd = "Serviço Avulso", vUnCom = 0 };
            var servico = new CorteCor.Models.Servico
            {
                IdServico = 11111,
                Nome = itemAvulso.XProd,
                Preco = itemAvulso.vUnCom,
                CodigoTributacaoMunicipio = itemAvulso.CodigoTributacao ?? "060101",
                AliquotaISS = itemAvulso.AliquotaISS
            };

            // 5. Mapear Agendamento (Simulado para data de emissão)
            var agendamento = new Agendamento
            {
                DataHora = Input.DataEmissao
            };

            // 6. Montar DPS (Padrão Nacional)
            var dps = _fiscalBuilder.MontarNFSe(config, tomador, servico, agendamento);

            System.IO.File.WriteAllText(@"C:\tmp\dps_envio.xml", ((Unimake.Business.DFe.Xml.NFSe.NACIONAL.DPS)dps).GerarXML().OuterXml);

            // 7. Transmitir
            var result = await _nfseEmissor.EmitirNFSeAsync(config, dps, null);

            // 7. Processar Retorno
            ProtocoloAutorizacao = result.Protocolo ?? "Sem Protocolo";
            XmlRetorno = result.XmlRetorno;
            XmlEnvio = result.XmlEnvio;
            
            if (result.Autorizada)
            {
                Logs.Add($"NFSe Autorizada com Sucesso! Protocolo: {ProtocoloAutorizacao}");
            }
            else
            {
                Logs.Add($"Falha na Emissão NFSe: {result.Motivo}");
                Logs.Add($"XML Retorno: {result.XmlRetorno}");
                throw new Exception($"{result.Motivo}\n{result.XmlRetorno}");
            }
        }

        private async Task<X509Certificate2> ObterCertificadoAsync(int idSalao)
        {
            var config = await _configHandler.ObterPorSalaoAsync(idSalao);
            byte[] pfxBytes = null;
            string senha = Input.CertificadoSenha;

            if (Input.CertificadoFile != null)
            {
                using (var ms = new MemoryStream())
                {
                    await Input.CertificadoFile.CopyToAsync(ms);
                    pfxBytes = ms.ToArray();
                }

                if (config == null)
                {
                    config = new SalaoConfigFiscal { IdSalao = idSalao };
                    config.Cnpj = Input.EmitenteCnpj;
                    config.RazaoSocial = Input.EmitenteNome;
                }

                config.CertificadoPfx = pfxBytes;
                if (!string.IsNullOrEmpty(senha))
                {
                    config.CertificadoSenha = _criptoService.Criptografar(senha);
                }
                config.DataAtualizacao = DateTime.Now;
                
                await _configHandler.SalvarAsync(config);
                HasSavedCertificate = true;
            }
            else if (config != null && config.CertificadoPfx != null)
            {
                pfxBytes = config.CertificadoPfx;
                if (string.IsNullOrEmpty(senha) && config.CertificadoSenha != null)
                {
                    senha = _criptoService.Descriptografar(config.CertificadoSenha);
                }
            }

            if (pfxBytes != null && !string.IsNullOrEmpty(senha))
            {
                return new X509Certificate2(pfxBytes, senha, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);
            }

            return null;
        }

        private string ExtrairProtocolo(string xml)
        {
            try
            {
                if (string.IsNullOrEmpty(xml)) return "Vazio";
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                
                // Tenta pegar nProt (Autorizado)
                var nProt = doc.GetElementsByTagName("nProt");
                if (nProt.Count > 0) return nProt[0].InnerText;
                
                // Tenta pegar cStat e xMotivo (Rejeição ou Outros)
                var cStat = doc.GetElementsByTagName("cStat");
                var xMotivo = doc.GetElementsByTagName("xMotivo");
                
                if (cStat.Count > 0 && xMotivo.Count > 0)
                {
                    return $"[{cStat[0].InnerText}] {xMotivo[0].InnerText}";
                }

                if (xMotivo.Count > 0) return xMotivo[0].InnerText;

                return "Verifique o XML de Retorno";
            }
            catch { return "Erro ao ler resposta SEFAZ"; }
        }

        private UFBrasil GetUfBrasil(string uf)
        {
            if (string.IsNullOrEmpty(uf)) return UFBrasil.MG;
            if (Enum.TryParse<UFBrasil>(uf, out var res)) return res;
            return UFBrasil.MG;
        }
    }
}
