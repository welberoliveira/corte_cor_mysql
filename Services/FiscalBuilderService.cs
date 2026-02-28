using System;
using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using Unimake.Business.DFe.Xml.NFSe;

namespace CorteCor.Services
{
    public class FiscalBuilderService
    {
        public NFe MontarNFCe(SalaoConfigFiscal config, Pessoa cliente, CorteCor.Models.Servico servico, Agendamento agendamento)
        {
            var infNfe = new InfNFe
            {
                Versao = "4.00",
                Ide = new Ide
                {
                    CUF = (UFBrasil)config.CodigoUFIBGE,
                    NatOp = "Venda Presencial", // Definimento base de NFC-e
                    Mod = ModeloDFe.NFCe,
                    Serie = 1,
                    NNF = 1, // Este numero deveria vir de controle de numeração do BD (Sequence)
                    DhEmi = DateTime.Now,
                    TpNF = TipoOperacao.Saida,
                    IdDest = DestinoOperacao.OperacaoInterna,
                    CMunFG = config.CodigoMunicipioIBGE,
                    TpImp = FormatoImpressaoDANFE.NFCe,
                    TpEmis = TipoEmissao.Normal,
                    TpAmb = config.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                    FinNFe = FinalidadeNFe.Normal,
                    IndFinal = SimNao.Sim,
                    IndPres = IndicadorPresenca.OperacaoPresencial,
                    ProcEmi = ProcessoEmissao.AplicativoContribuinte,
                    VerProc = "CorteCor 1.0"
                },
                Emit = new Emit
                {
                    CNPJ = config.Cnpj,
                    XNome = config.RazaoSocial,
                    IE = config.InscricaoEstadual,
                    CRT = (CRT)config.RegimeTributario,
                    EnderEmit = new EnderEmit
                    {
                        CMun = config.CodigoMunicipioIBGE,
                        XMun = "Municipio Padrao", // Preencher baseado no IBGE ou BD
                        UF = (UFBrasil)config.CodigoUFIBGE
                    }
                },
                Dest = new Dest
                {
                    // Regra de Homologação NFC-e obriga usar "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO"
                    XNome = config.Ambiente == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : (cliente.Nome ?? "Consumidor Final"),
                    // Pode ir CPF se preenchido e se for produção
                    CPF = config.Ambiente == 1 ? (cliente.CpfCnpj?.Replace(".", "").Replace("-", "")) : "" 
                },
                Total = new Total
                {
                    ICMSTot = new ICMSTot
                    {
                        VNF = (double)servico.Preco,
                        VProd = (double)servico.Preco,
                        // Totais zerados pois salao geralmente é simplex
                        VBC = 0,
                        VICMS = 0,
                        VDesc = 0
                    }
                },
                Det = new System.Collections.Generic.List<Det>()
            };

            // Adicionar Item (Servico ou Produto vendido)
            infNfe.Det.Add(new Det
            {
                NItem = 1,
                Prod = new Prod
                {
                    CProd = servico.IdServico.ToString(),
                    CEAN = "SEM GTIN",
                    XProd = config.Ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : servico.Nome,
                    NCM = servico.Cnae ?? "00", // Geralmente tem um NCM padrao para servicos (00 ou 99) quando emitido em NFC-e mista
                    CFOP = "5102", // Venda interna simples
                    UCom = "UN",
                    QCom = 1,
                    VUnCom = servico.Preco,
                    VProd = (double)servico.Preco,
                    CEANTrib = "SEM GTIN",
                    UTrib = "UN",
                    QTrib = 1,
                    VUnTrib = servico.Preco,
                    IndTot = SimNao.Sim
                },
                Imposto = new Imposto
                {
                    ICMS = new ICMS
                    {
                        ICMSSN102 = new ICMSSN102
                        {
                            Orig = OrigemMercadoria.Nacional,
                            CSOSN = "102"
                        }
                    }
                }
            });
            var nfe = new NFe();
            nfe.InfNFe = new System.Collections.Generic.List<InfNFe> { infNfe };

            return nfe;
        }

        public object MontarNFSe(SalaoConfigFiscal config, Pessoa cliente, CorteCor.Models.Servico servico, Agendamento agendamento)
        {
            var lote = new CorteCor.Models.Ginfes.EnviarLoteRpsEnvio
            {
                LoteRps = new CorteCor.Models.Ginfes.LoteRps
                {
                    Id = "LOTE" + DateTime.Now.Ticks.ToString(),
                    Cnpj = config.Cnpj,
                    InscricaoMunicipal = config.InscricaoMunicipal,
                    QuantidadeRps = 1,
                    NumeroLote = 1
                }
            };

            var rps = new CorteCor.Models.Ginfes.Rps
            {
                InfRps = new CorteCor.Models.Ginfes.InfRps
                {
                    Id = "RPS" + DateTime.Now.Ticks.ToString(),
                    IdentificacaoRps = new CorteCor.Models.Ginfes.IdentificacaoRps
                    {
                        Numero = 1,
                        Serie = "1",
                        Tipo = "1" // RPS
                    },
                    DataEmissao = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    NaturezaOperacao = 1, // Tributação no município
                    OptanteSimplesNacional = 1, // Sim
                    IncentivadorCultural = 2, // Não
                    Status = 1, // Normal
                    Servico = new CorteCor.Models.Ginfes.ServicoRps
                    {
                        Valores = new CorteCor.Models.Ginfes.ValoresRps
                        {
                            ValorServicos = servico.Preco,
                            IssRetido = 2, // Não
                            BaseCalculo = servico.Preco,
                            Aliquota = 0,
                            ValorIss = null
                        },
                        ItemListaServico = servico.CodigoTributacaoMunicipio ?? "06.01",
                        CodigoTributacaoMunicipio = servico.CodigoTributacaoMunicipio,
                        Discriminacao = $"Serviço de {servico.Nome} prestado no dia {agendamento.DataHora.ToString("dd/MM/yyyy HH:mm")}.",
                        CodigoMunicipio = config.CodigoMunicipioIBGE.ToString()
                    },
                    Prestador = new CorteCor.Models.Ginfes.PrestadorRps
                    {
                        Cnpj = config.Cnpj,
                        InscricaoMunicipal = config.InscricaoMunicipal
                    },
                    Tomador = new CorteCor.Models.Ginfes.TomadorRps
                    {
                        IdentificacaoTomador = new CorteCor.Models.Ginfes.IdentificacaoTomador
                        {
                            CpfCnpj = new CorteCor.Models.Ginfes.CpfCnpj
                            {
                                Cpf = (cliente.CpfCnpj != null && cliente.CpfCnpj.Length <= 11) ? cliente.CpfCnpj : null,
                                Cnpj = (cliente.CpfCnpj != null && cliente.CpfCnpj.Length > 11) ? cliente.CpfCnpj : null
                            }
                        },
                        RazaoSocial = cliente.Nome ?? "Consumidor Final"
                    }
                }
            };
            
            if (config.Ambiente == 2) // Homologacao
            {
                rps.InfRps.Servico.Discriminacao = "[AMBIENTE DE HOMOLOGACAO] " + rps.InfRps.Servico.Discriminacao;
                rps.InfRps.Tomador.IdentificacaoTomador.CpfCnpj.Cpf = "99999999999";
                rps.InfRps.Tomador.IdentificacaoTomador.CpfCnpj.Cnpj = null;
            }

            lote.LoteRps.ListaRps.Rps.Add(rps);

            return lote;
        }
    }
}
