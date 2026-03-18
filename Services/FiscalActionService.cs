using CorteCor.Models;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace CorteCor.Services
{
    public class FiscalActionService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;

        public FiscalActionService(CertificadoFiscalFactory certificadoFactory)
        {
            _certificadoFactory = certificadoFactory;
        }

        public async Task<NotaFiscalEvento> CancelarNfceAsync(SalaoConfigFiscal config, string chaveAcesso, string justificativa, string protocoloAutorizacao)
        {
            if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                throw new ArgumentException("A justificativa de cancelamento deve ter ao menos 15 caracteres.");

            try
            {
                GarantirConfigNfsePadrao();
                var cert = _certificadoFactory.InstanciarCertificado(config);
                string modelo = chaveAcesso.Substring(20, 2);
                var tipoDfe = modelo == "65" ? Unimake.Business.DFe.Servicos.TipoDFe.NFCe : Unimake.Business.DFe.Servicos.TipoDFe.NFe;
                
                dynamic cancelamento = new Unimake.Business.DFe.Xml.NFe.EnvEvento
                {
                    Versao = "1.00",
                    IdLote = "1",
                    Evento = new System.Collections.Generic.List<Unimake.Business.DFe.Xml.NFe.Evento>()
                };

                dynamic evento = new Unimake.Business.DFe.Xml.NFe.Evento
                {
                    Versao = "1.00",
                    InfEvento = new Unimake.Business.DFe.Xml.NFe.InfEvento
                    {
                        Id = "ID110111" + chaveAcesso + "01",
                        COrgao = (Unimake.Business.DFe.Servicos.UFBrasil)config.CodigoUFIBGE,
                        TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                        CNPJ = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                        ChNFe = chaveAcesso,
                        DhEvento = DateTime.Now,
                        TpEvento = (Unimake.Business.DFe.Servicos.TipoEventoNFe)110111,
                        NSeqEvento = 1
                    }
                };

                evento.InfEvento.DetEvento = new 
                {
                    Versao = "1.00",
                    DescEvento = "Cancelamento",
                    NProt = protocoloAutorizacao,
                    XJust = justificativa
                };

                cancelamento.Evento.Add(evento);

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = tipoDfe,
                    CertificadoDigital = cert,
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao
                };

                var recepcaoEvento = new Unimake.Business.DFe.Servicos.NFe.RecepcaoEvento(cancelamento, configuracao);
                recepcaoEvento.Executar();

                var status = "Processado";
                try { status = recepcaoEvento.Result.RetEvento[0].InfEvento.CStat + " - " + recepcaoEvento.Result.RetEvento[0].InfEvento.XMotivo; } catch { }

                return new NotaFiscalEvento
                {
                    TipoEvento = $"Cancelamento {tipoDfe}",
                    Justificativa = justificativa,
                    Status = status,
                    ProtocoloEvento = "Veja XML Retorno",
                    XmlEnvio = cancelamento.GerarXML().OuterXml,
                    XmlRetorno = recepcaoEvento.RetornoWSString 
                };
            }
            catch (Exception ex)
            {
                return new NotaFiscalEvento { Status = "Erro: " + ex.Message, Justificativa = justificativa, TipoEvento = "Cancelamento (Falha)" };
            }
        }

        public async Task<NotaFiscalEvento> InutilizarNfceAsync(SalaoConfigFiscal config, int ano, int serie, int numInicial, int numFinal, string justificativa, string tipoNota)
        {
            if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                throw new ArgumentException("A justificativa de inutilização deve ter ao menos 15 caracteres.");

            try
            {
                var cert = _certificadoFactory.InstanciarCertificado(config);
                var tipoDfe = tipoNota == "NF-e" ? Unimake.Business.DFe.Servicos.TipoDFe.NFe : Unimake.Business.DFe.Servicos.TipoDFe.NFCe;

                dynamic inut = new Unimake.Business.DFe.Xml.NFe.InutNFe
                {
                    Versao = "3.10"
                };

                inut.InfInut = new 
                {
                    Id = "ID" + config.CodigoUFIBGE + ano.ToString().Substring(2) + config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "") + (tipoNota == "NF-e" ? "55" : "65") + serie.ToString().PadLeft(3, '0') + numInicial.ToString().PadLeft(9, '0') + numFinal.ToString().PadLeft(9, '0'),
                    TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    XServ = "INUTILIZAR",
                    CUF = (Unimake.Business.DFe.Servicos.UFBrasil)config.CodigoUFIBGE,
                    Ano = ano.ToString().Substring(2),
                    CNPJ = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                    Mod = tipoNota == "NF-e" ? 55 : 65,
                    Serie = serie.ToString(),
                    NNFIni = numInicial.ToString(),
                    NNFFin = numFinal.ToString(),
                    XJust = justificativa
                };

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = tipoDfe,
                    CertificadoDigital = cert,
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao
                };

                var servico = new Unimake.Business.DFe.Servicos.NFe.Inutilizacao(inut, configuracao);
                servico.Executar();

                var resp = servico.Result;

                return new NotaFiscalEvento
                {
                    TipoEvento = $"Inutilização {tipoNota}",
                    Justificativa = justificativa,
                    Status = resp.InfInut.CStat + " - " + resp.InfInut.XMotivo,
                    ProtocoloEvento = resp.InfInut.NProt,
                    XmlEnvio = inut.GerarXML().OuterXml,
                    XmlRetorno = servico.RetornoWSString
                };
            }
            catch (Exception ex)
            {
                return new NotaFiscalEvento { Status = "Erro: " + ex.Message, Justificativa = justificativa, TipoEvento = $"Inutilização {tipoNota} (Falha)" };
            }
        }

        public async Task<NotaFiscalEvento> EnviarCartaCorrecaoAsync(SalaoConfigFiscal config, string chaveAcesso, string textoCorrecao, int sequencia)
        {
            if (string.IsNullOrWhiteSpace(textoCorrecao) || textoCorrecao.Length < 15)
                throw new ArgumentException("O texto da carta de correção deve ter ao menos 15 caracteres.");

            try
            {
                var cert = _certificadoFactory.InstanciarCertificado(config);
                var modelo = chaveAcesso.Substring(20, 2);
                var tipoDfe = modelo == "55" ? Unimake.Business.DFe.Servicos.TipoDFe.NFe : Unimake.Business.DFe.Servicos.TipoDFe.NFCe;

                dynamic evento = new Unimake.Business.DFe.Xml.NFe.EnvEvento
                {
                    Versao = "1.00",
                    IdLote = "1",
                    Evento = new System.Collections.Generic.List<Unimake.Business.DFe.Xml.NFe.Evento>()
                };

                dynamic evt = new Unimake.Business.DFe.Xml.NFe.Evento
                {
                    Versao = "1.00"
                };

                evt.InfEvento = new 
                {
                    Id = "ID" + "110110" + chaveAcesso + sequencia.ToString().PadLeft(2, '0'),
                    COrgao = (Unimake.Business.DFe.Servicos.UFBrasil)config.CodigoUFIBGE,
                    TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    CNPJ = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                    ChNFe = chaveAcesso,
                    DhEvento = DateTime.Now,
                    TpEvento = 110110,
                    NSeqEvento = sequencia,
                    VerEvento = "1.00"
                };

                evt.InfEvento.DetEvento = new 
                {
                    Versao = "1.00",
                    DescEvento = "Carta de Correcao",
                    XCorrecao = textoCorrecao,
                    XCondUso = "A Carta de Correcao e disciplinada pelo paragrafo 1o-A do art. 7o do Convenio S/N, de 15 de dezembro de 1970 e deve ser aplicada a erros ocorridos na emissao de documentos fiscais, desde que o erro nao esteja relacionado com: I - as variaveis que determinam o valor do imposto tais como: base de calculo, aliquota, diferenca de preco, quantidade, valor da operacao ou da prestacao; II - a correcao de dados cadastrais que implique mudanca do remetente ou do destinatario; III - a data de emissao ou de saida."
                };

                evento.Evento.Add(evt);

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = tipoDfe,
                    CertificadoDigital = cert,
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao
                };

                var servico = new Unimake.Business.DFe.Servicos.NFe.RecepcaoEvento(evento, configuracao);
                servico.Executar();

                var resp = servico.Result.RetEvento[0].InfEvento;

                return new NotaFiscalEvento
                {
                    TipoEvento = "CC-e",
                    Justificativa = textoCorrecao,
                    Status = resp.CStat + " - " + resp.XMotivo,
                    ProtocoloEvento = resp.NProt,
                    XmlEnvio = evento.GerarXML().OuterXml,
                    XmlRetorno = servico.RetornoWSString
                };
            }
            catch (Exception ex)
            {
                return new NotaFiscalEvento { Status = "Erro: " + ex.Message, Justificativa = textoCorrecao, TipoEvento = "CC-e (Falha)" };
            }
        }


        public async Task<NotaFiscalEvento> CancelarNfseAsync(SalaoConfigFiscal config, string chaveNfseNacional, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                throw new ArgumentException("A justificativa de cancelamento deve ter ao menos 15 caracteres.");

            var eventoRetorno = new NotaFiscalEvento
            {
                TipoEvento = "Cancelamento NFS-e Nacional",
                Justificativa = justificativa,
                Status = "Iniciando"
            };

            try
            {
                GarantirArquivosConfigNfse();
                var cert = _certificadoFactory.InstanciarCertificado(config);

                var evt = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.PedRegEvento
                {
                    Versao = "1.01",
                    InfPedReg = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.InfPedReg
                    {
                        Id = "PRE" + chaveNfseNacional + "101101",
                        TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                        CNPJAutor = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                        ChNFSe = chaveNfseNacional,
                        DhEventoField = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK"),
                        E101101 = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Eventos.E101101
                        {
                            CMotivo = (Unimake.Business.DFe.Servicos.CodigoJustificativaCancelamento)2,
                            XMotivo = justificativa
                        }
                    }
                };

                DefinirPropriedadeSeExistir(evt.InfPedReg, "VerAplic", "CorteCor");
                DefinirPropriedadeSeExistir(evt.InfPedReg.E101101, "XDesc", "Cancelamento de NFS-e");

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFSe,
                    TipoEmissao = Unimake.Business.DFe.Servicos.TipoEmissao.Normal,
                    CertificadoDigital = cert,
                    CodigoMunicipio = 1001058,
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    PadraoNFSe = Unimake.Business.DFe.Servicos.PadraoNFSe.NACIONAL,
                    Servico = Unimake.Business.DFe.Servicos.Servico.NFSeRecepcionarEventosDiversos,
                    SchemaVersao = "1.01",
                    BuscarConfiguracaoPastaBase = true,
                    PastaArquivoConfiguracao = AppContext.BaseDirectory
                };

                var xmlEvento = evt.GerarXML();
                var recepcaoEvento = new Unimake.Business.DFe.Servicos.NFSe.RecepcionarEvento(xmlEvento, configuracao);
                recepcaoEvento.Executar();

                eventoRetorno.XmlEnvio = xmlEvento.OuterXml;
                eventoRetorno.XmlRetorno = recepcaoEvento.RetornoWSString;
                eventoRetorno.ProtocoloEvento = ExtrairProtocoloEventoNfse(eventoRetorno.XmlRetorno);
                
                if (NotaFiscalAvulsaService.EhStatusEventoAutorizado(null, eventoRetorno.XmlRetorno))
                {
                    eventoRetorno.Status = "Cancelamento Autorizado";
                }
                else 
                {
                    eventoRetorno.Status = ExtrairDetalheCancelamentoNfse(eventoRetorno.XmlRetorno);
                }
            }
            catch(Exception ex)
            {
                eventoRetorno.Status = "Erro Interno: " + ex.Message;
            }

            return eventoRetorno;
        }

        public async Task<(bool EncontrouEvento, string? XmlRetorno, string? Mensagem)> ConsultarEventoCancelamentoNfseAsync(
            SalaoConfigFiscal config,
            string chaveNfseNacional)
        {
            GarantirArquivosConfigNfse();
            var cert = _certificadoFactory.InstanciarCertificado(config);

            var consulta = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.ConsPedRegEvento
            {
                Versao = "1.01",
                InfConsPedRegEvento = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.InfConsPedRegEvento
                {
                    ChNFSe = chaveNfseNacional,
                    TipoEvento = ((int)Unimake.Business.DFe.Servicos.CodigoEventoNFSe.CancelamentoDeNFSe).ToString(),
                    NumSeqEvento = "1"
                }
            };

            var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
            {
                TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFSe,
                TipoEmissao = Unimake.Business.DFe.Servicos.TipoEmissao.Normal,
                CertificadoDigital = cert,
                CodigoMunicipio = 1001058,
                TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                PadraoNFSe = Unimake.Business.DFe.Servicos.PadraoNFSe.NACIONAL,
                Servico = Unimake.Business.DFe.Servicos.Servico.NFSeConsultarEventosDiversos,
                SchemaVersao = "1.01",
                BuscarConfiguracaoPastaBase = true,
                PastaArquivoConfiguracao = AppContext.BaseDirectory
            };

            var servico = new Unimake.Business.DFe.Servicos.NFSe.ConsultarEvento(consulta.GerarXML(), configuracao);
            servico.Executar();

            var xmlRetorno = servico.RetornoWSString;
            var encontrouEvento = PossuiEventoCancelamentoNfse(xmlRetorno, servico.Result);
            var mensagem = NotaFiscalAvulsaService.ExtrairMensagemRetorno(xmlRetorno);

            return (encontrouEvento, xmlRetorno, mensagem);
        }

        private static void GarantirConfigNfsePadrao()
        {
            GarantirArquivoConfigNfse(
                "Config.NFSe.Config.xml",
                "Unimake.Business.DFe.Servicos.Config.NFSe.Config.xml");
        }

        private static void GarantirArquivosConfigNfse()
        {
            GarantirConfigNfsePadrao();
            GarantirArquivoConfigNfse(
                "NACIONAL.xml",
                "Unimake.Business.DFe.Servicos.Config.NFSe.NACIONAL.xml");
        }

        private static void GarantirArquivoConfigNfse(string nomeArquivo, string nomeRecurso)
        {
            var destino = Path.Combine(AppContext.BaseDirectory, nomeArquivo);
            if (File.Exists(destino))
            {
                return;
            }

            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "Unimake.Business.DFe", StringComparison.Ordinal))
                ?? Assembly.Load("Unimake.Business.DFe");

            using var stream = assembly.GetManifestResourceStream(nomeRecurso)
                ?? throw new InvalidOperationException($"Recurso incorporado nao encontrado: {nomeRecurso}");

            using var file = File.Create(destino);
            stream.CopyTo(file);
        }

        private static string? ExtrairProtocoloEventoNfse(string? retorno)
        {
            if (string.IsNullOrWhiteSpace(retorno))
            {
                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(retorno);

                foreach (var tag in new[] { "nProt", "nProtEvento", "nProtEv", "protocolo" })
                {
                    var nodes = doc.GetElementsByTagName(tag);
                    if (nodes.Count == 0)
                    {
                        continue;
                    }

                    var valor = nodes[0]?.InnerText?.Trim();
                    if (!string.IsNullOrWhiteSpace(valor) && valor.Length <= 255)
                    {
                        return valor;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string ExtrairDetalheCancelamentoNfse(string? retorno)
        {
            if (string.IsNullOrWhiteSpace(retorno))
            {
                return "Rejeitado pelo provedor fiscal sem detalhe adicional.";
            }

            var detalheXml = NotaFiscalAvulsaService.ExtrairMensagemRetorno(retorno);
            if (!string.IsNullOrWhiteSpace(detalheXml))
            {
                return detalheXml;
            }

            var detalheJson = ExtrairMensagemJson(retorno);
            if (!string.IsNullOrWhiteSpace(detalheJson))
            {
                return detalheJson;
            }

            var detalheTexto = CompactarRetornoTexto(retorno);
            return string.IsNullOrWhiteSpace(detalheTexto)
                ? "Rejeitado pelo provedor fiscal sem detalhe adicional."
                : detalheTexto;
        }

        private static string? ExtrairMensagemJson(string conteudo)
        {
            try
            {
                using var document = JsonDocument.Parse(conteudo);
                return ExtrairMensagemJson(document.RootElement);
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtrairMensagemJson(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                string? codigo = null;
                string? mensagem = null;

                foreach (var propriedade in element.EnumerateObject())
                {
                    var nome = propriedade.Name.ToLowerInvariant();
                    if (codigo == null && (nome == "cstat" || nome == "codigo" || nome == "code"))
                    {
                        codigo = propriedade.Value.ToString();
                    }

                    if (mensagem == null &&
                        (nome == "xmotivo" || nome == "mensagem" || nome == "message" || nome == "descricao" || nome == "detail" || nome == "title" || nome == "error"))
                    {
                        mensagem = propriedade.Value.ToString();
                    }

                    var detalheInterno = ExtrairMensagemJson(propriedade.Value);
                    if (!string.IsNullOrWhiteSpace(detalheInterno))
                    {
                        return detalheInterno;
                    }
                }

                if (!string.IsNullOrWhiteSpace(codigo) && !string.IsNullOrWhiteSpace(mensagem))
                {
                    return $"[{codigo}] {mensagem}";
                }

                return mensagem;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var detalhe = ExtrairMensagemJson(item);
                    if (!string.IsNullOrWhiteSpace(detalhe))
                    {
                        return detalhe;
                    }
                }

                return null;
            }

            return null;
        }

        private static string CompactarRetornoTexto(string retorno)
        {
            var texto = retorno.Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Replace("\t", " ", StringComparison.Ordinal)
                .Trim();

            while (texto.Contains("  ", StringComparison.Ordinal))
            {
                texto = texto.Replace("  ", " ", StringComparison.Ordinal);
            }

            return texto.Length <= 240 ? texto : texto[..240] + "...";
        }

        private static bool PossuiEventoCancelamentoNfse(
            string? xmlRetorno,
            Unimake.Business.DFe.Xml.NFSe.NACIONAL.RetConsPedRegEvento? result)
        {
            if (result?.Eventos?.ArquivoXml?.Evento?.InfEvento?.PedRegEvento?.InfPedReg?.E101101 != null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(xmlRetorno))
            {
                return false;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlRetorno);
                var xmlNormalizado = doc.OuterXml;

                return xmlNormalizado.Contains("101101", StringComparison.OrdinalIgnoreCase) &&
                       xmlNormalizado.Contains("pedRegEvento", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void DefinirPropriedadeSeExistir(object? destino, string nomePropriedade, object valor)
        {
            if (destino is null)
            {
                return;
            }

            var propriedade = destino.GetType().GetProperty(nomePropriedade, BindingFlags.Public | BindingFlags.Instance);
            if (propriedade is null || !propriedade.CanWrite)
            {
                return;
            }

            var tipoDestino = Nullable.GetUnderlyingType(propriedade.PropertyType) ?? propriedade.PropertyType;
            var valorConvertido = valor;

            if (tipoDestino.IsEnum && valor is not null)
            {
                valorConvertido = Enum.ToObject(tipoDestino, valor);
            }
            else if (valor is not null && !tipoDestino.IsInstanceOfType(valor))
            {
                valorConvertido = Convert.ChangeType(valor, tipoDestino);
            }

            propriedade.SetValue(destino, valorConvertido);
        }

    }
}
