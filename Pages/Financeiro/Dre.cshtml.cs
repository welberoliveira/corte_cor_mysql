using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class DreModel : PageModel
    {
        private static readonly CultureInfo CulturaBrasil = new("pt-BR");
        private readonly FinanceiroService _financeiroService;

        public DreModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public FinanceiroDreResumo Dre { get; private set; } = new();
        public List<int> AnosDisponiveis { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string TipoPeriodo { get; set; } = "Mensal";

        [BindProperty(SupportsGet = true)]
        public int Mes { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Ano { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            await CarregarAsync();
            var bytes = GerarExcelDre();
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{NomeArquivoBase()}.xlsx");
        }

        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            await CarregarAsync();
            var bytes = GerarPdfDre();
            return File(bytes, "application/pdf", $"{NomeArquivoBase()}.pdf");
        }

        public string NomeMes(int mes)
        {
            return CulturaBrasil.DateTimeFormat.GetAbbreviatedMonthName(mes).Replace(".", "").ToUpper(CulturaBrasil);
        }

        public string NomeMesCompleto(int mes)
        {
            return CulturaBrasil.DateTimeFormat.GetMonthName(mes);
        }

        public string PeriodoSelecionado()
        {
            return TipoPeriodo == "Anual"
                ? Ano.ToString(CulturaBrasil)
                : $"{NomeMesCompleto(Mes)} de {Ano}";
        }

        public string FormatarMoeda(decimal valor)
        {
            var texto = Math.Abs(valor).ToString("N2", CulturaBrasil);
            return valor < 0 ? $"(R$ {texto})" : $"R$ {texto}";
        }

        public string FormatarPercentual(decimal valor, decimal referencia)
        {
            if (referencia == 0)
            {
                return "0,00%";
            }

            return (valor / referencia).ToString("P2", CulturaBrasil);
        }

        public string ClasseValor(decimal valor)
        {
            if (valor < 0)
            {
                return "text-danger";
            }

            return valor > 0 ? "text-success" : "text-muted";
        }

        public string ClasseLinha(FinanceiroDreLinhaDemonstrativo linha)
        {
            if (linha.ResultadoFinal)
            {
                return "dre-final";
            }

            if (linha.Subtotal)
            {
                return "dre-subtotal";
            }

            return linha.Destaque ? "dre-grupo" : string.Empty;
        }

        private async Task CarregarAsync()
        {
            var hoje = DateTime.Today;
            TipoPeriodo = string.Equals(TipoPeriodo, "Anual", StringComparison.OrdinalIgnoreCase) ? "Anual" : "Mensal";
            Ano = Ano > 0 ? Ano : hoje.Year;
            Mes = Mes is >= 1 and <= 12 ? Mes : hoje.Month;
            AnosDisponiveis = Enumerable.Range(hoje.Year - 5, 6).Reverse().ToList();
            if (!AnosDisponiveis.Contains(Ano))
            {
                AnosDisponiveis.Insert(0, Ano);
                AnosDisponiveis = AnosDisponiveis.Distinct().OrderByDescending(ano => ano).ToList();
            }

            var dataInicio = TipoPeriodo == "Anual"
                ? new DateTime(Ano, 1, 1)
                : new DateTime(Ano, Mes, 1);
            var dataFim = TipoPeriodo == "Anual"
                ? new DateTime(Ano, 12, 31)
                : dataInicio.AddMonths(1).AddDays(-1);

            Dre = await _financeiroService.ObterDreAsync(ObterIdSalao(), dataInicio, dataFim, TipoPeriodo);
        }

        private byte[] GerarExcelDre()
        {
            using var memory = new MemoryStream();
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            {
                AddZipEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""");

                AddZipEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");

                AddZipEntry(archive, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="DRE" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
""");

                AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>
""");

                AddZipEntry(archive, "xl/worksheets/sheet1.xml", GerarWorksheetXml());
            }

            return memory.ToArray();
        }

        private string GerarWorksheetXml()
        {
            var row = 1;
            var builder = new StringBuilder();
            builder.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
            builder.AppendLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
            builder.AppendLine("<sheetData>");

            builder.AppendLine($"<row r=\"{row}\">{InlineCell(row, 1, $"DRE {TipoPeriodo} - {PeriodoSelecionado()}")}</row>");
            row++;
            builder.AppendLine($"<row r=\"{row}\">{InlineCell(row, 1, "Base: lançamentos por competência, exceto cancelados")}</row>");
            row += 2;

            var headers = CabecalhosExportacao();
            builder.AppendLine($"<row r=\"{row}\">{string.Concat(headers.Select((header, index) => InlineCell(row, index + 1, header)))}</row>");
            row++;

            foreach (var linha in Dre.Linhas)
            {
                var cells = new StringBuilder();
                cells.Append(InlineCell(row, 1, $"{new string(' ', linha.Nivel * 2)}{linha.Descricao}"));
                var values = ValoresExportacao(linha).ToList();
                for (var index = 0; index < values.Count; index++)
                {
                    cells.Append(NumberCell(row, index + 2, values[index]));
                }

                builder.AppendLine($"<row r=\"{row}\">{cells}</row>");
                row++;
            }

            builder.AppendLine("</sheetData>");
            builder.AppendLine("</worksheet>");
            return builder.ToString();
        }

        private byte[] GerarPdfDre()
        {
            var pdf = new PdfCanvas();
            const float margin = 24f;
            var usableWidth = PdfCanvas.A4Width - (margin * 2);
            var valueColumns = TipoPeriodo == "Anual" ? Dre.Meses.Count + 1 : 1;
            var firstColumnWidth = TipoPeriodo == "Anual" ? 168f : 310f;
            var valueWidth = (usableWidth - firstColumnWidth) / valueColumns;
            var y = PdfCanvas.A4Height - 34f;

            void DesenharCabecalhoPagina()
            {
                pdf.SetFillColor(10, 84, 170);
                pdf.FillRectangle(margin, y - 18f, usableWidth, 24f);
                pdf.SetFillColor(255, 255, 255);
                pdf.DrawText(margin + 10f, y - 2f, $"DRE {TipoPeriodo} - {PeriodoSelecionado()}", 13f, true);
                y -= 34f;

                pdf.SetFillColor(51, 65, 85);
                pdf.DrawText(margin, y, $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", 8f);
                y -= 12f;
                pdf.DrawText(margin, y, "Base: lançamentos por competência, exceto cancelados", 8f);
                y -= 18f;

                pdf.SetFillColor(226, 232, 240);
                pdf.FillRectangle(margin, y - 12f, usableWidth, 16f);
                pdf.SetFillColor(31, 41, 55);
                pdf.DrawText(margin + 4f, y - 1f, "Linha do DRE", 6.5f, true);

                var x = margin + firstColumnWidth;
                var headers = CabecalhosExportacao().Skip(1).ToList();
                foreach (var header in headers)
                {
                    pdf.DrawText(x + valueWidth - 4f, y - 1f, header, 5.5f, true, PdfTextAlign.Right);
                    x += valueWidth;
                }

                y -= 20f;
            }

            DesenharCabecalhoPagina();

            foreach (var linha in Dre.Linhas)
            {
                if (y < 54f)
                {
                    pdf.NewPage();
                    y = PdfCanvas.A4Height - 34f;
                    DesenharCabecalhoPagina();
                }

                var rowHeight = linha.Destaque ? 15f : 13f;
                if (linha.ResultadoFinal)
                {
                    pdf.SetFillColor(219, 234, 254);
                    pdf.FillRectangle(margin, y - 10f, usableWidth, rowHeight);
                }
                else if (linha.Subtotal)
                {
                    pdf.SetFillColor(238, 242, 255);
                    pdf.FillRectangle(margin, y - 10f, usableWidth, rowHeight);
                }
                else if (linha.Destaque)
                {
                    pdf.SetFillColor(248, 250, 252);
                    pdf.FillRectangle(margin, y - 10f, usableWidth, rowHeight);
                }

                pdf.SetFillColor(30, 41, 59);
                var descricao = Truncar(linha.Descricao, TipoPeriodo == "Anual" ? 40 : 70);
                pdf.DrawText(margin + 4f + (linha.Nivel * 8f), y, descricao, linha.Destaque ? 6.5f : 6f, linha.Destaque);

                var x = margin + firstColumnWidth;
                foreach (var valor in ValoresExportacao(linha))
                {
                    pdf.DrawText(x + valueWidth - 4f, y, FormatarValorPdf(valor), TipoPeriodo == "Anual" ? 5.2f : 6.2f, linha.Destaque, PdfTextAlign.Right);
                    x += valueWidth;
                }

                pdf.SetStrokeColor(226, 232, 240);
                pdf.DrawLine(margin, y - 7f, PdfCanvas.A4Width - margin, y - 7f);
                y -= rowHeight;
            }

            return pdf.Build();
        }

        private List<string> CabecalhosExportacao()
        {
            var headers = new List<string> { "Conta contábil / linha do DRE" };
            if (TipoPeriodo == "Anual")
            {
                headers.AddRange(Dre.Meses.Select(NomeMes));
                headers.Add("Total");
            }
            else
            {
                headers.Add(NomeMes(Mes));
            }
            return headers;
        }

        private IEnumerable<decimal> ValoresExportacao(FinanceiroDreLinhaDemonstrativo linha)
        {
            if (TipoPeriodo == "Anual")
            {
                foreach (var mes in Dre.Meses)
                {
                    yield return linha.ObterValor(mes);
                }
            }
            else
            {
                yield return linha.ObterValor(Mes);
            }

            if (TipoPeriodo == "Anual")
            {
                yield return linha.Total;
            }
        }

        private string NomeArquivoBase()
        {
            var sufixo = TipoPeriodo == "Anual" ? Ano.ToString(CulturaBrasil) : $"{Ano}{Mes:00}";
            return $"dre-{TipoPeriodo.ToLowerInvariant()}-{sufixo}";
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }

        private static void AddZipEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        private static string InlineCell(int row, int column, string value)
        {
            return $"<c r=\"{ColumnName(column)}{row}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{EscapeXml(value)}</t></is></c>";
        }

        private static string NumberCell(int row, int column, decimal value)
        {
            return $"<c r=\"{ColumnName(column)}{row}\"><v>{value.ToString(CultureInfo.InvariantCulture)}</v></c>";
        }

        private static string ColumnName(int column)
        {
            var dividend = column;
            var columnName = string.Empty;
            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static string EscapeXml(string? value)
        {
            return SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
        }

        private static string Truncar(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..Math.Max(0, maxLength - 3)] + "...";
        }

        private static string FormatarValorPdf(decimal valor)
        {
            var texto = Math.Abs(valor).ToString("N2", CulturaBrasil);
            return valor < 0 ? $"({texto})" : texto;
        }
    }
}
