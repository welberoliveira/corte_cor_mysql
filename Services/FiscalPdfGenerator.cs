using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CorteCor.Models;

namespace CorteCor.Services
{
    public class FiscalPdfGenerator
    {
        public Task<byte[]> GerarPdfAsync(NotaFiscal nota)
        {
            if (nota == null)
            {
            throw new InvalidOperationException("Nota fiscal não informada.");
            }

            if (string.IsNullOrWhiteSpace(nota.XmlRetorno) && string.IsNullOrWhiteSpace(nota.XmlEnvio))
            {
            throw new InvalidOperationException("Não há conteúdo fiscal suficiente para gerar o PDF.");
            }

            var linhas = new List<string>
            {
                "Documento Fiscal",
                $"Tipo: {nota.TipoNota}",
                $"Numero: {nota.Numero}",
                $"Serie: {nota.Serie}",
                $"Status: {nota.Status}",
                $"Valor total: {nota.ValorTotal.ToString("C", new CultureInfo("pt-BR"))}",
                $"Emissao: {nota.DataEmissao:dd/MM/yyyy HH:mm}",
                $"Protocolo: {nota.ProtocoloAutorizacao ?? "-"}",
                $"Chave: {nota.ChaveAcesso ?? nota.ChaveAcessoNacional ?? "-"}"
            };

            return Task.FromResult(GerarPdfBasico(linhas));
        }

        private static byte[] GerarPdfBasico(IEnumerable<string> linhas)
        {
            var conteudoTexto = new StringBuilder();
            conteudoTexto.AppendLine("BT");
            conteudoTexto.AppendLine("/F1 12 Tf");
            conteudoTexto.AppendLine("50 790 Td");
            conteudoTexto.AppendLine("14 TL");

            var primeira = true;
            foreach (var linha in linhas)
            {
                var textoSeguro = EscaparPdf(NormalizarTextoPdf(linha));
                if (!primeira)
                {
                    conteudoTexto.AppendLine("T*");
                }

                conteudoTexto.AppendLine($"({textoSeguro}) Tj");
                primeira = false;
            }

            conteudoTexto.AppendLine("ET");
            var stream = conteudoTexto.ToString();
            var ascii = Encoding.ASCII;

            using var memory = new MemoryStream();
            var offsets = new List<long>();

            static void EscreverAscii(MemoryStream destino, string conteudo, Encoding encoding)
            {
                var bytes = encoding.GetBytes(conteudo);
                destino.Write(bytes, 0, bytes.Length);
            }

            void EscreverObjeto(int numero, string conteudo)
            {
                offsets.Add(memory.Position);
                EscreverAscii(memory, $"{numero} 0 obj\n{conteudo}\nendobj\n", ascii);
            }

            EscreverAscii(memory, "%PDF-1.4\n", ascii);
            EscreverObjeto(1, "<< /Type /Catalog /Pages 2 0 R >>");
            EscreverObjeto(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            EscreverObjeto(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            EscreverObjeto(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            EscreverObjeto(5, $"<< /Length {ascii.GetByteCount(stream)} >>\nstream\n{stream}endstream");

            var xrefStart = memory.Position;
            EscreverAscii(memory, "xref\n", ascii);
            EscreverAscii(memory, $"0 {offsets.Count + 1}\n", ascii);
            EscreverAscii(memory, "0000000000 65535 f \n", ascii);
            foreach (var offset in offsets)
            {
                EscreverAscii(memory, $"{offset:0000000000} 00000 n \n", ascii);
            }

            EscreverAscii(memory, "trailer\n", ascii);
            EscreverAscii(memory, $"<< /Size {offsets.Count + 1} /Root 1 0 R >>\n", ascii);
            EscreverAscii(memory, "startxref\n", ascii);
            EscreverAscii(memory, $"{xrefStart}\n", ascii);
            EscreverAscii(memory, "%%EOF", ascii);

            return memory.ToArray();
        }

        private static string EscaparPdf(string texto)
        {
            return texto
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static string NormalizarTextoPdf(string texto)
        {
            var formD = texto.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(formD.Length);

            foreach (var ch in formD)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (categoria == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(ch <= 127 ? ch : ' ');
            }

            return Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
        }
    }
}
