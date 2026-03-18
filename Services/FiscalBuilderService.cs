using System;
using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using Unimake.Business.DFe.Xml.NFSe;

namespace CorteCor.Services
{
    public class FiscalBuilderService
    {
        public NFe MontarNFCe(SalaoConfigFiscal config, Pessoa cliente, CorteCor.Models.Servico servico, Agendamento agendamento, int? serie = null, int? numero = null)
        {
            var infNfe = new InfNFe
            {
                Versao = "4.00",
                Ide = new Ide
                {
                    CUF = (UFBrasil)config.CodigoUFIBGE,
                    NatOp = "Venda Presencial", // Definimento base de NFC-e
                    Mod = ModeloDFe.NFCe,
                    Serie = serie ?? config.SerieNFCe,
                    NNF = numero ?? config.NumeroNFCe, 
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
                        XMun = config.EnderecoLogradouro != null ? "Cidade Vinculada" : "Nao Informado", // Correcting: Model doesn't have EnderecoCidade
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
                        // Totais aproximados (Lei 12.741) - estimativa baseada em alíquota aproximada de 13.45% (exemplo salão)
                        VBC = 0,
                        VICMS = 0,
                        VDesc = 0,
                        VTotTrib = Math.Round((double)servico.Preco * 0.1345, 2)
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

        public object MontarNFSe(SalaoConfigFiscal config, Pessoa cliente, CorteCor.Models.Servico servico, Agendamento agendamento, int? serie = null, int? numero = null)
        {
            // Montar o Id conforme padrão TSIdDPS do XSD Nacional:
            // DPS (3) + cMunEmi (7) + tpInscFed (1 = 1:CPF, 2:CNPJ) + cpfCnpj (14 padded) + serie (5) + nDPS (15)
            // Total: 3 + 7 + 1 + 14 + 5 + 15 = 45 caracteres (Regex: DPS[0-9]{42})
            string cnpjLimpo = (config.Cnpj ?? "").Replace(".", "").Replace("/", "").Replace("-", "");
            string tpInscFed = cnpjLimpo.Length <= 11 ? "1" : "2";
            string cpfCnpjPadded = cnpjLimpo.PadLeft(14, '0');
            string serieDPS = (serie ?? config.SerieNFSe).ToString().PadLeft(5, '0'); 
            string numeroDPS = (numero ?? config.NumeroNFSe).ToString(); 
            string idDPS = $"DPS{config.CodigoMunicipioIBGE.ToString().PadLeft(7, '0')}{tpInscFed}{cpfCnpjPadded}{serieDPS}{numeroDPS.PadLeft(15, '0')}";

            var dps = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.DPS
            {
                Versao = "1.01",
                InfDPS = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.InfDPS
                {
                    Id = idDPS,
                    TpAmb = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    DhEmi = DateTimeOffset.Now,
                    VerAplic = "CorteCor 1.0",
                    Serie = serieDPS,
                    NDPS = numeroDPS,
                    DCompet = DateTimeOffset.Now,
                    CLocEmi = config.CodigoMunicipioIBGE,
                    TpEmit = Unimake.Business.DFe.Servicos.TipoEmitenteNFSe.Prestador, 
                    Prest = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Prest
                    {
                        CNPJ = config.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", ""),
                        IM = string.IsNullOrWhiteSpace(config.InscricaoMunicipal) ? "101477" : config.InscricaoMunicipal.Replace(".", "").Replace("-", "").Replace("/", "").Replace(" ", ""),
                        RegTrib = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.RegTrib
                        {
                            // Enviar como ME_EPP (valor 3) para alinhar com o cadastro real da Tonni Tecnologia no Sefin Nacional
                            OpSimpNac = Unimake.Business.DFe.Servicos.OptSimplesNacional.ME_EPP,
                            RegApTribSN = (Unimake.Business.DFe.Servicos.RegApTribSN)1,
                            RegEspTrib = (Unimake.Business.DFe.Servicos.RegEspTrib)config.RegimeEspecialTributacao
                        }
                    },
                    Toma = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Toma
                    {
                        XNome = string.IsNullOrWhiteSpace(cliente.Nome) ? "Consumidor Final" : cliente.Nome,
                        End = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.End
                        {
                            EndNac = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.EndNac
                            {
                                CEP = string.IsNullOrWhiteSpace(cliente.Cep) || cliente.Cep.Replace("-", "").Length != 8 
                                        ? "39400001" 
                                        : cliente.Cep.Replace("-", ""),
                                CMun = int.TryParse(cliente.Cidade, out int codMunToma) && codMunToma > 0 ? codMunToma : 3143302
                            },
                            XLgr = string.IsNullOrWhiteSpace(cliente.Logradouro) ? "Rua Doutor Santos" : cliente.Logradouro,
                            Nro = string.IsNullOrWhiteSpace(cliente.Numero) ? "123" : cliente.Numero,
                            XBairro = string.IsNullOrWhiteSpace(cliente.Bairro) ? "Centro" : cliente.Bairro
                        }
                    },
                    Serv = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Serv
                    {
                        LocPrest = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.LocPrest
                        {
                            CLocPrestacao = config.CodigoMunicipioIBGE 
                        },
                        CServ = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.CServ
                        {
                            CTribNac = GetCTribNacValido(servico.CodigoTributacaoMunicipio),
                            CTribMun = null, // Sefin Nacional Prefers omitting municipal code if National is present
                            XDescServ = $"Serviço de {servico.Nome} prestado no dia {agendamento.DataHora.ToString("dd/MM/yyyy HH:mm")}."
                        }
                    },
                    Valores = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Valores
                    {
                        VServPrest = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.VServPrest
                        {
                            VServ = (double)servico.Preco
                        },
                        Trib = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Trib
                        {
                            TribMun = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.TribMun
                            {
                                TribISSQN = (Unimake.Business.DFe.Servicos.TribISSQN)config.IssExigibilidade,
                                TpRetISSQN = (Unimake.Business.DFe.Servicos.TipoRetencaoISSQN)config.IssRetido,
                                PAliqField = (config.RegimeTributario == 3 || config.RegimeTributario == 2) 
                                                ? Math.Min((double)(servico.AliquotaISS ?? 0m), 5.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                                : null
                            },
                            TotTrib = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.TotTrib
                            {
                                VTotTrib = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.VTotTrib
                                {
                                    VTotTribFedField = Math.Round((double)servico.Preco * 0.048, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                    VTotTribEstField = "0.00",
                                    VTotTribMunField = Math.Round((double)servico.Preco * 0.05, 2).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                }
                            }
                        }
                    }
                }
            };
            
            // Set CPF or CNPJ for Tomador
            if (!string.IsNullOrEmpty(cliente.CpfCnpj)) {
                var doc = cliente.CpfCnpj.Replace(".", "").Replace("/", "").Replace("-", "");
                if (doc.Length <= 11) dps.InfDPS.Toma.CPF = doc;
                else dps.InfDPS.Toma.CNPJ = doc;
            }

            if (config.Ambiente == 2) // Homologacao
            {
                dps.InfDPS.Serv.CServ.XDescServ = "[AMBIENTE DE HOMOLOGACAO] " + dps.InfDPS.Serv.CServ.XDescServ;
            }

            return dps;
        }

        /// <summary>
        /// Valida e retorna um código cTribNac no formato exigido pelo XSD Nacional (6 dígitos numéricos).
        /// Se o valor do banco estiver vazio, nulo ou em formato inválido, retorna o código padrão "010100".
        /// </summary>
        private string GetCTribNacValido(string? codigoTributacao)
        {
            if (string.IsNullOrWhiteSpace(codigoTributacao))
                return "060101"; // Fallback para salão de beleza (06.01 - Cabeleireiros, manicuros, pedicuros)

            // Limpar espaços e traços. Manteremos os pontos para saber se é formato da LC 116 (ex: 06.01, 1.05)
            string str = codigoTributacao.Replace("-", "").Replace(" ", "").Trim();

            // Se o usuário digitou exatamente no formato X.YY ou XX.YY (ex: 6.01 ou 06.01)
            var match = System.Text.RegularExpressions.Regex.Match(str, @"^(\d{1,2})\.(\d{2})$");
            if (match.Success)
            {
                // Item (XX) e Subitem (YY) formatados com 2 casas
                string item = match.Groups[1].Value.PadLeft(2, '0');
                string subitem = match.Groups[2].Value.PadLeft(2, '0');
                
                // O padrão Sefaz Nacional acrescenta o desdobramento final, geralmente "01" para serviços não divididos nacionalmente
                return $"{item}{subitem}01"; 
            }

            // Remoção final do ponto se não bateu na REGEX acima
            string limpo = str.Replace(".", "");

            // Se for exatamente 6 dígitos enviados com sucesso, é o cTribNac Nacional pronto
            if (System.Text.RegularExpressions.Regex.IsMatch(limpo, @"^\d{6}$"))
                return limpo;

            // Se tiver 3 ou 4 dígitos (ex: "101" ou "0601" -> 010101, 060101)
            if (limpo.Length >= 3 && limpo.Length <= 4)
            {
                string parsed = limpo.PadLeft(4, '0');
                return $"{parsed}01"; // Retorna XXYY01
            }

            // Fallback final
            return "060101";
        }
    }
}
