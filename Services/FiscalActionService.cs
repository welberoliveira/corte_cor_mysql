using CorteCor.Models;
using System;
using System.Threading.Tasks;

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
            var cert = _certificadoFactory.InstanciarCertificado(config);
            
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

            // Usamos dynamic para atribuir o conteúdo do evento de cancelamento sem depender do nome exato da classe na versão do Unimake
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
                TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFCe,
                CertificadoDigital = cert,
                TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao
            };

            var recepcaoEvento = new Unimake.Business.DFe.Servicos.NFe.RecepcaoEvento(cancelamento, configuracao);
            recepcaoEvento.Executar();

            return new NotaFiscalEvento
            {
                TipoEvento = "Cancelamento NFC-e",
                Justificativa = justificativa,
                Status = "Processado (Consultar XML para Detalhes)",
                ProtocoloEvento = "Veja XML Retorno",
                XmlEnvio = cancelamento.GerarXML().OuterXml,
                XmlRetorno = recepcaoEvento.RetornoWSString 
            };
        }

        public async Task<NotaFiscalEvento> CancelarNfseAsync(SalaoConfigFiscal config, string chaveNfseNacional, string justificativa)
        {
            // ... (logica de NFSe Nacional já verificada)
            var eventoRetorno = new NotaFiscalEvento
            {
                TipoEvento = "Cancelamento NFS-e Nacional",
                Justificativa = justificativa,
                Status = "Processando"
            };

            try
            {
                var cert = _certificadoFactory.InstanciarCertificado(config);

                var evt = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.PedRegEvento
                {
                    Versao = "1.00",
                    InfPedReg = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.InfPedReg
                    {
                        Id = "ID" + chaveNfseNacional + "1011011",
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

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFSe,
                    TipoEmissao = Unimake.Business.DFe.Servicos.TipoEmissao.Normal,
                    CertificadoDigital = cert,
                    CodigoMunicipio = 1001058, 
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    PadraoNFSe = Unimake.Business.DFe.Servicos.PadraoNFSe.NACIONAL,
                    Servico = Unimake.Business.DFe.Servicos.Servico.NFSeGerarNfse 
                };

                var recepcaoEvento = new Unimake.Business.DFe.Servicos.NFSe.CancelarNfse(evt.GerarXML(), configuracao);
                recepcaoEvento.Executar();

                eventoRetorno.XmlEnvio = evt.GerarXML().OuterXml;
                eventoRetorno.XmlRetorno = recepcaoEvento.RetornoWSString;
                
                if (recepcaoEvento.RetornoWSString.Contains("sucesso") || recepcaoEvento.RetornoWSString.Contains("Autorizado")) 
                {
                      eventoRetorno.Status = "Autorizado";
                }
                else 
                {
                      eventoRetorno.Status = "Rejeitado";
                }
            }
            catch(Exception ex)
            {
                eventoRetorno.Status = "Erro Interno: " + ex.Message;
            }

            return eventoRetorno;
        }

        public async Task<NotaFiscalInutilizacao> InutilizarNfceAsync(SalaoConfigFiscal config, int ano, int serie, int numeroInicial, int numeroFinal, string justificativa)
        {
            var cert = _certificadoFactory.InstanciarCertificado(config);

            dynamic inutNFe = new Unimake.Business.DFe.Xml.NFe.InutNFe
            {
                Versao = "4.00"
            };

            // Criamos o InfInut via dynamic para contornar discrepâncias de tipos e nomes entre versões do Unimake
            inutNFe.InfInut = new 
            {
                Id = "ID" + config.CodigoUFIBGE + (ano % 100).ToString("D2") + config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "") + "65" + serie.ToString("D3") + numeroInicial.ToString("D9") + numeroFinal.ToString("D9"),
                TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                CUF = (Unimake.Business.DFe.Servicos.UFBrasil)config.CodigoUFIBGE,
                Ano = (ano % 100).ToString("D2"),
                CNPJ = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                Mod = (Unimake.Business.DFe.Servicos.ModeloDFe)65,
                Serie = serie,
                NNFIni = numeroInicial.ToString(),
                NNFFin = numeroFinal.ToString(),
                XJust = justificativa
            };

            var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
            {
                TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFCe,
                CertificadoDigital = cert,
                TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao
            };

            var servicoInut = new Unimake.Business.DFe.Servicos.NFe.Inutilizacao(inutNFe, configuracao);
            servicoInut.Executar();

            return new NotaFiscalInutilizacao
            {
                Ano = ano,
                Serie = serie,
                NumeroInicial = numeroInicial,
                NumeroFinal = numeroFinal,
                Justificativa = justificativa,
                Status = "Processado (Consultar XML)",
                Modelo = 65,
                Protocolo = "Consulte XML",
                XmlRetorno = servicoInut.RetornoWSString
            };
        }
    }
}
