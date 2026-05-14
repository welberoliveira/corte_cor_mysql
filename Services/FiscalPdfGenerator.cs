using System.Globalization;
using System.Xml;
using CorteCor.Models;
using QRCoder;

namespace CorteCor.Services;

public class FiscalPdfGenerator
{
    private const float Margin = 24f;
    private const float ContentWidth = PdfCanvas.A4Width - (Margin * 2f);

    public Task<byte[]> GerarPdfAsync(NotaFiscal nota, SalaoConfigFiscal? config = null)
    {
        if (nota == null)
        {
            throw new InvalidOperationException("Nota fiscal não informada.");
        }

        if (string.IsNullOrWhiteSpace(nota.XmlRetorno) && string.IsNullOrWhiteSpace(nota.XmlEnvio))
        {
            throw new InvalidOperationException("Não há conteúdo fiscal suficiente para gerar o PDF.");
        }

        var reader = XmlFieldReader.From(nota.XmlRetorno, nota.XmlEnvio);
        var documento = MontarDocumento(nota, config, reader);
        return Task.FromResult(Renderizar(documento));
    }

    private static byte[] Renderizar(DanfseDocumento documento)
    {
        var pdf = new PdfCanvas();
        pdf.SetLineWidth(0.8f);
        pdf.SetStrokeColor(110, 118, 130);
        pdf.SetFillColor(24, 24, 24);

        var y = PdfCanvas.A4Height - 30f;
        y = DesenharCabecalho(pdf, documento, y) - 10f;
        y = DesenharPartes(pdf, documento, y) - 10f;
        y = DesenharServico(pdf, documento, y) - 10f;
        y = DesenharTributacaoMunicipal(pdf, documento, y) - 10f;
        y = DesenharTributacaoFederal(pdf, documento, y) - 10f;
        y = DesenharTotais(pdf, documento, y) - 10f;
        DesenharInformacoesComplementares(pdf, documento, y);

        return pdf.Build();
    }

    private static string Currency(decimal value)
    {
        return value.ToString("C", new CultureInfo("pt-BR"));
    }

    private static void DrawSectionBand(PdfCanvas pdf, float x, float topY, float width, string title)
    {
        const float bandHeight = 16f;
        pdf.SetFillColor(233, 242, 238);
        pdf.FillRectangle(x, topY - bandHeight, width, bandHeight);
        pdf.SetStrokeColor(110, 118, 130);
        pdf.DrawRectangle(x, topY - bandHeight, width, bandHeight);
        pdf.SetFillColor(39, 46, 61);
        pdf.DrawText(x + 6, topY - 11, title, 8.2f, true);
    }

    private static void DrawField(PdfCanvas pdf, float x, float y, float width, string label, string value, int maxLines)
    {
        pdf.DrawText(x, y, label, 6.6f, true);
        pdf.DrawParagraph(x, y - 9f, width, value, 8.1f, 9.4f, false, Math.Max(1, maxLines));
    }

    private static float DesenharCabecalho(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 160f;
        const float authWidth = 156f;
        const float authInnerWidth = authWidth - 10f;
        const float qrSize = 34f;
        const float authPadding = 8f;
        var bottomY = topY - height;
        var authX = Margin + ContentWidth - authWidth;
        const float columnGap = 12f;
        var contentLeft = Margin + 8f;
        var leftWidth = authX - contentLeft - 10f;
        var fieldWidth = (leftWidth - (columnGap * 2f)) / 3f;
        var col1 = contentLeft;
        var col2 = col1 + fieldWidth + columnGap;
        var col3 = col2 + fieldWidth + columnGap;

        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "DANFSe v1.0");

        pdf.SetFillColor(15, 125, 87);
        pdf.DrawText(Margin + 10f, topY - 30f, "NFS-e", 16.8f, true);
        pdf.SetFillColor(39, 46, 61);
        pdf.DrawText(Margin + 84f, topY - 28f, "Documento Auxiliar da NFS-e", 11.6f, true);
        pdf.DrawText(Margin + 84f, topY - 40f, documento.SubtituloMunicipio, 8.1f, false);

        if (!string.IsNullOrWhiteSpace(documento.AvisoValidade))
        {
            pdf.SetFillColor(180, 35, 35);
            pdf.DrawText(Margin + (ContentWidth / 2f), topY - 54f, documento.AvisoValidade, 9.2f, true, PdfTextAlign.Center);
            pdf.SetFillColor(39, 46, 61);
        }

        var infoTop = topY - 74f;
        DrawField(pdf, contentLeft, infoTop, leftWidth, "Chave de acesso da NFS-e", documento.ChaveAcesso, 1);

        var rowA = topY - 101f;
        var rowB = topY - 127f;

        DrawField(pdf, col1, rowA, fieldWidth, "Número da NFS-e", documento.NumeroNfse, 1);
        DrawField(pdf, col2, rowA, fieldWidth, "Competência", documento.Competencia, 1);
        DrawField(pdf, col3, rowA, fieldWidth, "Data e hora de emissão da NFS-e", documento.DataHoraEmissaoNfse, 1);
        DrawField(pdf, col1, rowB, fieldWidth, "Número da DPS", documento.NumeroDps, 1);
        DrawField(pdf, col2, rowB, fieldWidth, "Série da DPS", documento.SerieDps, 1);
        DrawField(pdf, col3, rowB, fieldWidth, "Data e hora de emissão da DPS", documento.DataHoraEmissaoDps, 1);

        pdf.DrawRectangle(authX, bottomY + 10f, authInnerWidth, height - 68f);
        pdf.SetFillColor(39, 46, 61);
        pdf.DrawText(authX + (authInnerWidth / 2f), topY - 58f, "Autenticidade", 8f, true, PdfTextAlign.Center);
        DrawField(pdf, authX + authPadding, topY - 70f, authInnerWidth - (authPadding * 2f), "Codigo de verificacao", documento.CodigoVerificacao, 1);
        pdf.DrawParagraph(authX + authPadding, topY - 88f, 90f, documento.TextoAutenticidade, 6.4f, 7.2f, false, 3);
        pdf.DrawUrlParagraph(authX + authPadding, topY - 112f, 90f, documento.UrlConsulta, 6.2f, 6.8f, true, 6);
        pdf.DrawQrCode(authX + authInnerWidth - qrSize - 8f, topY - 88f, qrSize, CreateQrCodeModules(documento.UrlConsulta));
        if (false)
        {
        DrawField(pdf, authX + 8, topY - 60, authWidth - 26, "Código de verificação", documento.CodigoVerificacao, 1);
        pdf.DrawParagraph(authX + 8, topY - 81, authWidth - 26, documento.TextoAutenticidade, 7.2f, 8.5f, false, 3);
        pdf.DrawParagraph(authX + 8, topY - 110, authWidth - 26, documento.UrlConsulta, 7f, 8.2f, true, 3);

        }

        return bottomY;
    }

    private static float DesenharPartes(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 112f;
        const float gap = 10f;
        var blockWidth = (ContentWidth - gap) / 2f;
        var leftX = Margin;
        var rightX = Margin + blockWidth + gap;
        var bottomY = topY - height;

        DrawParte(pdf, leftX, topY, blockWidth, "EMITENTE DA NFS-e / PRESTADOR DO SERVIÇO", documento.Prestador);
        DrawParte(pdf, rightX, topY, blockWidth, "TOMADOR DO SERVIÇO", documento.Tomador);

        return bottomY;
    }

    private static void DrawParte(PdfCanvas pdf, float x, float topY, float width, string titulo, DanfseParte parte)
    {
        const float height = 112f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, x, topY, width, titulo);
        pdf.DrawRectangle(x, bottomY, width, height);

        var left = x + 8f;
        var top = topY - 24f;
        var col2 = x + (width / 2f);
        var valueWidth = (width / 2f) - 16f;

        DrawField(pdf, left, top, valueWidth, "Nome / Nome empresarial", parte.Nome, 2);
        DrawField(pdf, col2, top, valueWidth, "CNPJ / CPF / NIF", parte.Documento, 1);
        DrawField(pdf, left, top - 24f, valueWidth, "Endereço", parte.Endereco, 2);
        DrawField(pdf, col2, top - 24f, valueWidth, "Inscrição municipal / estadual", parte.Inscricoes, 2);
        DrawField(pdf, left, top - 49f, valueWidth, "Município / UF", parte.MunicipioUf, 1);
        DrawField(pdf, col2, top - 49f, valueWidth, "E-mail / telefone", parte.Contato, 2);
        DrawField(pdf, left, top - 73f, width - 16f, "Situação no Simples / regime", parte.Regime, 2);
    }

    private static float DesenharServico(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 96f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "SERVIÇO PRESTADO");
        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);

        var rowY = topY - 24f;
        DrawField(pdf, Margin + 8, rowY, 118f, "Código da tributação nacional", documento.Servico.CodigoTributacaoNacional, 1);
        DrawField(pdf, Margin + 136, rowY, 118f, "Código da tributação municipal", documento.Servico.CodigoTributacaoMunicipal, 1);
        DrawField(pdf, Margin + 264, rowY, 118f, "Local da prestação", documento.Servico.LocalPrestacao, 1);
        DrawField(pdf, Margin + 392, rowY, 127f, "País da prestação", documento.Servico.PaisPrestacao, 1);
        DrawField(pdf, Margin + 8, rowY - 25f, ContentWidth - 16f, "Descrição do serviço", documento.Servico.Descricao, 3);

        return bottomY;
    }

    private static float DesenharTributacaoMunicipal(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 112f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "TRIBUTAÇÃO MUNICIPAL");
        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);

        var col1 = Margin + 8f;
        var col2 = Margin + 188f;
        var col3 = Margin + 368f;
        var w = 160f;
        var row1 = topY - 24f;
        var row2 = topY - 49f;
        var row3 = topY - 74f;
        var row4 = topY - 96f;

        DrawField(pdf, col1, row1, w, "Tributação do ISSQN", documento.Servico.TributacaoIssqn, 2);
        DrawField(pdf, col2, row1, w, "Pelo resultado da prestação do serviço", documento.Servico.ResultadoPrestacao, 2);
        DrawField(pdf, col3, row1, w, "Município de incidência do ISSQN", documento.Servico.MunicipioIncidencia, 2);
        DrawField(pdf, col1, row2, w, "Regime especial de tributação", documento.Servico.RegimeEspecialTributacao, 2);
        DrawField(pdf, col2, row2, w, "Suspensão da exigibilidade do ISSQN", documento.Servico.SuspensaoExigibilidade, 2);
        DrawField(pdf, col3, row2, w, "Benefício municipal / responsável tributário", JoinParts(documento.Servico.BeneficioMunicipal, documento.Servico.ResponsavelTributario, " | "), 2);
        DrawField(pdf, col1, row3, w, "Valor do serviço", Currency(documento.Valores.ValorServico), 1);
        DrawField(pdf, col2, row3, w, "Desconto incondicionado / deduções", $"{Currency(documento.Valores.DescontoIncondicionado)} / {Currency(documento.Valores.Deducoes)}", 2);
        DrawField(pdf, col3, row3, w, "Base de cálculo", Currency(documento.Valores.BaseCalculo), 1);
        DrawField(pdf, col1, row4, w, "Alíquota aplicada", documento.Valores.AliquotaAplicada, 1);
        DrawField(pdf, col2, row4, w, "Retenção do ISSQN", documento.Servico.RetencaoIssqn, 1);
        DrawField(pdf, col3, row4, w, "ISSQN apurado", Currency(documento.Valores.IssqnApurado), 1);

        return bottomY;
    }

    private static float DesenharTributacaoFederal(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 66f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "TRIBUTAÇÃO FEDERAL");
        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);

        var col1 = Margin + 8f;
        var col2 = Margin + 110f;
        var col3 = Margin + 212f;
        var col4 = Margin + 314f;
        var col5 = Margin + 416f;
        var row1 = topY - 24f;
        var row2 = topY - 46f;

        DrawField(pdf, col1, row1, 90f, "IRRF", Currency(documento.Valores.Irrf), 1);
        DrawField(pdf, col2, row1, 90f, "CP", Currency(documento.Valores.Cpp), 1);
        DrawField(pdf, col3, row1, 90f, "CSLL", Currency(documento.Valores.Csll), 1);
        DrawField(pdf, col4, row1, 90f, "PIS", Currency(documento.Valores.Pis), 1);
        DrawField(pdf, col5, row1, 95f, "COFINS", Currency(documento.Valores.Cofins), 1);
        DrawField(pdf, col4, row2, 190f, "Total tributação federal", Currency(documento.Valores.TotalTributacaoFederal), 1);

        return bottomY;
    }

    private static float DesenharTotais(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 86f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "VALOR TOTAL DA NFS-e");
        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);

        var col1 = Margin + 8f;
        var col2 = Margin + 192f;
        var col3 = Margin + 376f;
        var row1 = topY - 24f;
        var row2 = topY - 46f;
        var row3 = topY - 68f;

        DrawField(pdf, col1, row1, 168f, "Valor do serviço", Currency(documento.Valores.ValorServico), 1);
        DrawField(pdf, col2, row1, 168f, "Desconto condicionado", Currency(documento.Valores.DescontoCondicionado), 1);
        DrawField(pdf, col3, row1, 145f, "ISSQN retido", Currency(documento.Valores.IssqnRetido), 1);
        DrawField(pdf, col1, row2, 168f, "IRRF, CP, CSLL, PIS/COFINS retidos", Currency(documento.Valores.TotalTributosRetidosFederais), 1);
        DrawField(pdf, col2, row2, 168f, "Desconto incondicionado", Currency(documento.Valores.DescontoIncondicionado), 1);
        DrawField(pdf, col3, row2, 145f, "Valor líquido da NFS-e", Currency(documento.Valores.ValorLiquido), 1);
        DrawField(pdf, col1, row3, 165f, "Tributos aproximados federais", Currency(documento.Valores.TributosAproxFederais), 1);
        DrawField(pdf, col2, row3, 165f, "Tributos aproximados estaduais", Currency(documento.Valores.TributosAproxEstaduais), 1);
        DrawField(pdf, col3, row3, 145f, "Tributos aproximados municipais", Currency(documento.Valores.TributosAproxMunicipais), 1);

        return bottomY;
    }

    private static void DesenharInformacoesComplementares(PdfCanvas pdf, DanfseDocumento documento, float topY)
    {
        const float height = 100f;
        var bottomY = topY - height;
        DrawSectionBand(pdf, Margin, topY, ContentWidth, "INFORMAÇÕES COMPLEMENTARES");
        pdf.DrawRectangle(Margin, bottomY, ContentWidth, height);

        var row1 = topY - 24f;
        var row2 = topY - 48f;
        var row3 = topY - 72f;

        DrawField(pdf, Margin + 8, row1, 160f, "Status fiscal", documento.StatusFiscal, 1);
        DrawField(pdf, Margin + 180, row1, 160f, "Código de retorno", documento.CodigoRetorno, 1);
        DrawField(pdf, Margin + 352, row1, 167f, "Protocolo / recibo", documento.ProtocoloOuRecibo, 2);
        DrawField(pdf, Margin + 8, row2, ContentWidth - 16f, "Mensagem do provedor fiscal", documento.MensagemRetorno, 2);
        DrawField(pdf, Margin + 8, row3, ContentWidth - 16f, "Consulte a autenticidade pelo portal nacional da NFS-e", documento.UrlConsulta, 2);
    }

    private static DanfseDocumento MontarDocumento(NotaFiscal nota, SalaoConfigFiscal? config, XmlFieldReader xml)
    {
        var valorServico = xml.GetDecimal(nota.ValorTotal, "vServ", "VServ");
        var descontoCondicionado = xml.GetDecimal(0m, "vDescCond", "VDescCond");
        var descontoIncondicionado = xml.GetDecimal(0m, "vDescIncond", "VDescIncond");
        var deducoes = xml.GetDecimal(0m, "vDedRed", "VDedRed", "vDeducao", "VDeducao");
        var baseCalculo = xml.GetDecimal(valorServico - descontoIncondicionado - deducoes, "vBC", "VBC");
        var aliquota = xml.GetDecimal(0m, "pAliq", "PAliq");
        var issqn = xml.GetDecimal(0m, "vISSQN", "VISSQN");
        var issqnRetido = xml.GetDecimal(0m, "vISSRet", "VISSRet");
        var irrf = xml.GetDecimal(0m, "vIRRF", "VIRRF");
        var pis = xml.GetDecimal(0m, "vPIS", "VPIS");
        var cofins = xml.GetDecimal(0m, "vCOFINS", "VCOFINS");
        var csll = xml.GetDecimal(0m, "vCSLL", "VCSLL");
        var cpp = xml.GetDecimal(0m, "vCPP", "VCPP", "vCP", "VCP");
        var tribFed = xml.GetDecimal(irrf + pis + cofins + csll + cpp, "vTotTribFed", "VTotTribFed");
        var tribEst = xml.GetDecimal(0m, "vTotTribEst", "VTotTribEst");
        var tribMun = xml.GetDecimal(issqn, "vTotTribMun", "VTotTribMun");
        var retFederais = irrf + pis + cofins + csll + cpp;
        var valorLiquido = xml.GetDecimal(
            valorServico - descontoCondicionado - descontoIncondicionado - issqnRetido - retFederais,
            "vLiq", "VLiq", "vLiqNfse", "VLiqNfse");

        var chave = PrimeiroPreenchido(
            nota.ChaveAcessoNacional,
            nota.ChaveAcesso,
            xml.GetText("chNFSe", "ChNFSe"),
            "-");

        return new DanfseDocumento
        {
            SubtituloMunicipio = $"Prefeitura Municipal de {PrimeiroPreenchido(config?.EnderecoCidade, "Município conveniado")}",
            AvisoValidade = nota.Ambiente == 2 ? "NFS-e sem validade jurídica - ambiente de homologação" : string.Empty,
            ChaveAcesso = chave,
            NumeroNfse = PrimeiroPreenchido(nota.NumeroNFSeNacional, nota.Numero.ToString(CultureInfo.InvariantCulture)),
            Competencia = FormatDate(xml.GetText("dCompet", "DCompet"), nota.DataEmissao),
            DataHoraEmissaoNfse = FormatDateTime(xml.GetText("dhEmi", "DhEmi", "dhrEmi", "DhProc"), nota.DataEmissao),
            NumeroDps = PrimeiroPreenchido(xml.GetText("nDPS", "NDPS"), nota.NumeroRecibo, "-"),
            SerieDps = PrimeiroPreenchido(xml.GetText("serie", "Serie"), nota.Serie.ToString(CultureInfo.InvariantCulture)),
            DataHoraEmissaoDps = FormatDateTime(xml.GetText("dhEmi", "DhEmi", "dhEmiDPS", "DhEmiDPS"), nota.DataEmissao),
            CodigoVerificacao = PrimeiroPreenchido(xml.GetText("cVerif", "CVerif", "codigoVerificacao", "CodigoVerificacao"), "-"),
            StatusFiscal = PrimeiroPreenchido(nota.Status, "Pendente"),
            CodigoRetorno = PrimeiroPreenchido(xml.GetText("cStat", "CStat"), "-"),
            ProtocoloOuRecibo = PrimeiroPreenchido(nota.ProtocoloAutorizacao, nota.NumeroRecibo, "-"),
            MensagemRetorno = PrimeiroPreenchido(nota.JustificativaRejeicao, xml.GetText("xMotivo", "XMotivo"), "Documento emitido pelo sistema."),
            UrlConsulta = BuildConsultaUrl(nota.Ambiente, chave),
            TextoAutenticidade = "Consulte a autenticidade da NFS-e pela chave de acesso no portal nacional da NFS-e.",
            Prestador = new DanfseParte
            {
                Nome = PrimeiroPreenchido(config?.RazaoSocial, xml.GetInSections(new[] { "Prest", "Emit", "Emitente" }, "xNome", "XNome"), "-"),
                Documento = FormatDocumento(PrimeiroPreenchido(config?.Cnpj, xml.GetInSections(new[] { "Prest", "Emit", "Emitente" }, "CNPJ", "CPF"))),
                Inscricoes = JoinParts(LabelIfHasValue("IM", config?.InscricaoMunicipal), LabelIfHasValue("IE", config?.InscricaoEstadual), " | "),
                Endereco = BuildEndereco(config?.EnderecoLogradouro, config?.EnderecoNumero, config?.EnderecoBairro, config?.EnderecoCep),
                MunicipioUf = JoinParts(config?.EnderecoCidade, config?.EnderecoUF, " - "),
                Contato = JoinParts(config?.Email, config?.Telefone, " | "),
                Regime = MapRegime(config?.RegimeTributario)
            },
            Tomador = new DanfseParte
            {
                Nome = PrimeiroPreenchido(xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "xNome", "XNome"), "Tomador não identificado"),
                Documento = FormatDocumento(PrimeiroPreenchido(xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "CNPJ"), xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "CPF"))),
                Inscricoes = LabelIfHasValue("IM", xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "IM", "InscricaoMunicipal")),
                Endereco = BuildEndereco(
                    xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "xLgr", "XLgr"),
                    xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "nro", "Nro"),
                    xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "xBairro", "XBairro"),
                    xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "CEP")),
                MunicipioUf = JoinParts(xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "xMun", "XMun"), xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "UF"), " - "),
                Contato = JoinParts(xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "email", "Email"), xml.GetInSections(new[] { "Toma", "Tom", "Tomador" }, "fone", "Fone"), " | "),
                Regime = "Cadastro do tomador conforme dados da DPS/NFS-e."
            },
            Servico = new DanfseServico
            {
                CodigoTributacaoNacional = PrimeiroPreenchido(xml.GetInSections(new[] { "CServ", "Serv" }, "cTribNac", "CTribNac"), "-"),
                CodigoTributacaoMunicipal = PrimeiroPreenchido(xml.GetInSections(new[] { "CServ", "Serv" }, "cTribMun", "CTribMun"), "-"),
                LocalPrestacao = PrimeiroPreenchido(xml.GetInSections(new[] { "Serv", "InfDPS" }, "xLocPrest", "XLocPrest"), config?.EnderecoCidade, "-"),
                PaisPrestacao = PrimeiroPreenchido(xml.GetInSections(new[] { "Serv" }, "xPais", "XPais"), "Brasil"),
                Descricao = PrimeiroPreenchido(xml.GetInSections(new[] { "CServ", "Serv" }, "xDescServ", "XDescServ"), "Serviço não informado."),
                TributacaoIssqn = PrimeiroPreenchido(xml.GetInSections(new[] { "TribMun", "Serv" }, "tribISSQN", "TribISSQN"), "Operação tributável"),
                ResultadoPrestacao = PrimeiroPreenchido(xml.GetInSections(new[] { "Serv" }, "indPrest", "IndPrest"), "Prestação tributável"),
                MunicipioIncidencia = PrimeiroPreenchido(xml.GetInSections(new[] { "Serv", "TribMun" }, "xMunInc", "XMunInc"), config?.EnderecoCidade, "-"),
                RegimeEspecialTributacao = PrimeiroPreenchido(xml.GetInSections(new[] { "TribMun", "Serv" }, "regEspTrib", "RegEspTrib"), "Nenhum"),
                SuspensaoExigibilidade = BoolLabel(xml.GetInSections(new[] { "TribMun", "Serv" }, "exigSusp", "ExigSusp")),
                BeneficioMunicipal = BoolLabel(xml.GetInSections(new[] { "TribMun", "Serv" }, "benefMun", "BenefMun")),
                ResponsavelTributario = PrimeiroPreenchido(xml.GetInSections(new[] { "Serv", "TribMun" }, "respTrib", "RespTrib"), "-"),
                RetencaoIssqn = PrimeiroPreenchido(xml.GetInSections(new[] { "TribMun", "Serv" }, "tpRetISSQN", "TpRetISSQN"), issqnRetido > 0 ? "Retido" : "Não retido")
            },
            Valores = new DanfseValores
            {
                ValorServico = valorServico,
                DescontoCondicionado = descontoCondicionado,
                DescontoIncondicionado = descontoIncondicionado,
                Deducoes = deducoes,
                BaseCalculo = baseCalculo,
                AliquotaAplicada = aliquota > 0 ? $"{aliquota:0.##}%" : "-",
                IssqnApurado = issqn,
                IssqnRetido = issqnRetido,
                Irrf = irrf,
                Pis = pis,
                Cofins = cofins,
                Csll = csll,
                Cpp = cpp,
                TotalTributacaoFederal = tribFed,
                TotalTributosRetidosFederais = retFederais,
                ValorLiquido = valorLiquido,
                TributosAproxFederais = tribFed,
                TributosAproxEstaduais = tribEst,
                TributosAproxMunicipais = tribMun
            }
        };
    }

    private static string BuildConsultaUrl(int ambiente, string chaveAcesso)
    {
        if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso == "-")
        {
            return "Portal Nacional da NFS-e";
        }

        var baseUrl = ambiente == 2
            ? "https://adn.producaorestrita.nfse.gov.br/danfse/"
            : "https://adn.nfse.gov.br/danfse/";
        return baseUrl + chaveAcesso;
    }

    private static string FormatDate(string? value, DateTime fallback)
    {
        return TryParseDate(value, out var parsed)
            ? parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : fallback.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(string? value, DateTime fallback)
    {
        return TryParseDate(value, out var parsed)
            ? parsed.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            : fallback.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static bool TryParseDate(string? value, out DateTime parsed)
    {
        if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return true;
        }

        parsed = default;
        return false;
    }

    private static string FormatDocumento(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 14 && ulong.TryParse(digits, out var cnpj))
        {
            return cnpj.ToString(@"00\.000\.000\/0000\-00", CultureInfo.InvariantCulture);
        }

        if (digits.Length == 11 && ulong.TryParse(digits, out var cpf))
        {
            return cpf.ToString(@"000\.000\.000\-00", CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string BuildEndereco(string? logradouro, string? numero, string? bairro, string? cep)
    {
        var endereco = JoinParts(logradouro, numero, ", ");
        var complemento = JoinParts(bairro, FormatCep(cep), " | ");
        return PrimeiroPreenchido(JoinParts(endereco, complemento, " - "), "-");
    }

    private static string FormatCep(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 8 && uint.TryParse(digits, out var cep)
            ? cep.ToString(@"00000\-000", CultureInfo.InvariantCulture)
            : value;
    }

    private static string MapRegime(int? regimeTributario)
    {
        return regimeTributario switch
        {
            1 => "Simples Nacional",
            2 => "Simples Nacional - excesso de sublimite",
            3 => "Regime normal",
            _ => "Regime tributário não informado"
        };
    }

    private static string BoolLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Não";
        }

        return value.Trim() switch
        {
            "1" or "true" or "True" or "SIM" or "Sim" => "Sim",
            _ => "Não"
        };
    }

    private static string LabelIfHasValue(string label, string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{label}: {value}";
    }

    private static string JoinParts(string? left, string? right, string separator = " ")
    {
        var parts = new[] { left, right }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .ToArray();
        return parts.Length == 0 ? string.Empty : string.Join(separator, parts);
    }

    private static bool[,] CreateQrCodeModules(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var size = data.ModuleMatrix.Count;
        var modules = new bool[size, size];

        for (var row = 0; row < size; row++)
        {
            var bitRow = data.ModuleMatrix[row];
            for (var col = 0; col < size; col++)
            {
                modules[row, col] = bitRow[col];
            }
        }

        return modules;
    }

    private static string PrimeiroPreenchido(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
    }

    private sealed class DanfseDocumento
    {
        public string SubtituloMunicipio { get; set; } = string.Empty;
        public string AvisoValidade { get; set; } = string.Empty;
        public string ChaveAcesso { get; set; } = string.Empty;
        public string NumeroNfse { get; set; } = string.Empty;
        public string Competencia { get; set; } = string.Empty;
        public string DataHoraEmissaoNfse { get; set; } = string.Empty;
        public string NumeroDps { get; set; } = string.Empty;
        public string SerieDps { get; set; } = string.Empty;
        public string DataHoraEmissaoDps { get; set; } = string.Empty;
        public string CodigoVerificacao { get; set; } = string.Empty;
        public string StatusFiscal { get; set; } = string.Empty;
        public string CodigoRetorno { get; set; } = string.Empty;
        public string ProtocoloOuRecibo { get; set; } = string.Empty;
        public string MensagemRetorno { get; set; } = string.Empty;
        public string UrlConsulta { get; set; } = string.Empty;
        public string TextoAutenticidade { get; set; } = string.Empty;
        public DanfseParte Prestador { get; set; } = new();
        public DanfseParte Tomador { get; set; } = new();
        public DanfseServico Servico { get; set; } = new();
        public DanfseValores Valores { get; set; } = new();
    }

    private sealed class DanfseParte
    {
        public string Nome { get; set; } = "-";
        public string Documento { get; set; } = "-";
        public string Inscricoes { get; set; } = "-";
        public string Endereco { get; set; } = "-";
        public string MunicipioUf { get; set; } = "-";
        public string Contato { get; set; } = "-";
        public string Regime { get; set; } = "-";
    }

    private sealed class DanfseServico
    {
        public string CodigoTributacaoNacional { get; set; } = "-";
        public string CodigoTributacaoMunicipal { get; set; } = "-";
        public string LocalPrestacao { get; set; } = "-";
        public string PaisPrestacao { get; set; } = "Brasil";
        public string Descricao { get; set; } = "-";
        public string TributacaoIssqn { get; set; } = "-";
        public string ResultadoPrestacao { get; set; } = "-";
        public string MunicipioIncidencia { get; set; } = "-";
        public string RegimeEspecialTributacao { get; set; } = "-";
        public string SuspensaoExigibilidade { get; set; } = "Não";
        public string BeneficioMunicipal { get; set; } = "Não";
        public string ResponsavelTributario { get; set; } = "-";
        public string RetencaoIssqn { get; set; } = "Não retido";
    }

    private sealed class DanfseValores
    {
        public decimal ValorServico { get; set; }
        public decimal DescontoCondicionado { get; set; }
        public decimal DescontoIncondicionado { get; set; }
        public decimal Deducoes { get; set; }
        public decimal BaseCalculo { get; set; }
        public string AliquotaAplicada { get; set; } = "-";
        public decimal IssqnApurado { get; set; }
        public decimal IssqnRetido { get; set; }
        public decimal Irrf { get; set; }
        public decimal Pis { get; set; }
        public decimal Cofins { get; set; }
        public decimal Csll { get; set; }
        public decimal Cpp { get; set; }
        public decimal TotalTributacaoFederal { get; set; }
        public decimal TotalTributosRetidosFederais { get; set; }
        public decimal ValorLiquido { get; set; }
        public decimal TributosAproxFederais { get; set; }
        public decimal TributosAproxEstaduais { get; set; }
        public decimal TributosAproxMunicipais { get; set; }
    }

    private sealed class XmlFieldReader
    {
        private readonly XmlDocument? _document;

        private XmlFieldReader(XmlDocument? document)
        {
            _document = document;
        }

        public static XmlFieldReader From(string? primary, string? fallback)
        {
            return new XmlFieldReader(Load(primary) ?? Load(fallback));
        }

        public string? GetText(params string[] names)
        {
            if (_document == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                var node = _document.SelectSingleNode($"//*[local-name()='{name}']");
                if (!string.IsNullOrWhiteSpace(node?.InnerText))
                {
                    return node.InnerText.Trim();
                }
            }

            return null;
        }

        public string? GetInSections(string[] sections, params string[] names)
        {
            if (_document == null)
            {
                return null;
            }

            foreach (var section in sections)
            {
                foreach (var name in names)
                {
                    var node = _document.SelectSingleNode($"//*[local-name()='{section}']//*[local-name()='{name}']");
                    if (!string.IsNullOrWhiteSpace(node?.InnerText))
                    {
                        return node.InnerText.Trim();
                    }
                }
            }

            return null;
        }

        public decimal GetDecimal(decimal fallback, params string[] names)
        {
            var value = GetText(names);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static XmlDocument? Load(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            try
            {
                var document = new XmlDocument
                {
                    PreserveWhitespace = false
                };
                document.LoadXml(xml);
                return document;
            }
            catch
            {
                return null;
            }
        }
    }
}
