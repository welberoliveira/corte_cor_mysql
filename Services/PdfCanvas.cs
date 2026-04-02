using System.Globalization;
using System.Text;

namespace CorteCor.Services;

internal enum PdfTextAlign
{
    Left,
    Center,
    Right
}

internal sealed class PdfCanvas
{
    public const float A4Width = 595f;
    public const float A4Height = 842f;

    private static readonly Encoding Latin1Encoding = CreateLatin1Encoding();
    private readonly StringBuilder _content = new();

    public void SetStrokeColor(byte r, byte g, byte b)
    {
        _content.AppendLine($"{ColorComponent(r)} {ColorComponent(g)} {ColorComponent(b)} RG");
    }

    public void SetFillColor(byte r, byte g, byte b)
    {
        _content.AppendLine($"{ColorComponent(r)} {ColorComponent(g)} {ColorComponent(b)} rg");
    }

    public void SetLineWidth(float width)
    {
        _content.AppendLine($"{Format(width)} w");
    }

    public void DrawRectangle(float x, float y, float width, float height, bool fill = false)
    {
        _content.AppendLine($"{Format(x)} {Format(y)} {Format(width)} {Format(height)} re {(fill ? "B" : "S")}");
    }

    public void FillRectangle(float x, float y, float width, float height)
    {
        _content.AppendLine($"{Format(x)} {Format(y)} {Format(width)} {Format(height)} re f");
    }

    public void DrawLine(float x1, float y1, float x2, float y2)
    {
        _content.AppendLine($"{Format(x1)} {Format(y1)} m {Format(x2)} {Format(y2)} l S");
    }

    public void DrawText(
        float x,
        float y,
        string? text,
        float size = 9f,
        bool bold = false,
        PdfTextAlign align = PdfTextAlign.Left)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var normalized = NormalizeWhitespace(text);
        var actualX = align switch
        {
            PdfTextAlign.Center => x - (MeasureWidth(normalized, size, bold) / 2f),
            PdfTextAlign.Right => x - MeasureWidth(normalized, size, bold),
            _ => x
        };

        _content.AppendLine("BT");
        _content.AppendLine($"/{(bold ? "F2" : "F1")} {Format(size)} Tf");
        _content.AppendLine($"{Format(actualX)} {Format(y)} Td");
        _content.AppendLine($"<{EncodeHex(normalized)}> Tj");
        _content.AppendLine("ET");
    }

    public float DrawParagraph(
        float x,
        float y,
        float width,
        string? text,
        float size = 9f,
        float leading = 11f,
        bool bold = false,
        int maxLines = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return y;
        }

        var lines = WrapText(text, width, size, bold, maxLines).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            DrawText(x, y - (i * leading), lines[i], size, bold);
        }

        return y - (lines.Count * leading);
    }

    public float DrawUrlParagraph(
        float x,
        float y,
        float width,
        string? text,
        float size = 9f,
        float leading = 11f,
        bool bold = false,
        int maxLines = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return y;
        }

        var lines = WrapUrlText(text, width, size, bold, maxLines).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            DrawText(x, y - (i * leading), lines[i], size, bold);
        }

        return y - (lines.Count * leading);
    }

    public void DrawQrCode(float x, float topY, float size, bool[,] modules, int quietZoneModules = 4)
    {
        var rowCount = modules.GetLength(0);
        var colCount = modules.GetLength(1);
        var totalModules = Math.Max(rowCount, colCount) + (quietZoneModules * 2);
        var moduleSize = size / totalModules;

        SetFillColor(255, 255, 255);
        FillRectangle(x, topY - size, size, size);
        SetFillColor(0, 0, 0);

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                if (!modules[row, col])
                {
                    continue;
                }

                var moduleX = x + ((col + quietZoneModules) * moduleSize);
                var moduleY = topY - ((row + quietZoneModules + 1) * moduleSize);
                FillRectangle(moduleX, moduleY, moduleSize, moduleSize);
            }
        }

        SetStrokeColor(110, 118, 130);
        DrawRectangle(x, topY - size, size, size);
    }

    public byte[] Build()
    {
        var ascii = Encoding.ASCII;
        var stream = _content.ToString();

        using var memory = new MemoryStream();
        var offsets = new List<long>();

        static void WriteAscii(MemoryStream streamTarget, string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            streamTarget.Write(bytes, 0, bytes.Length);
        }

        void WriteObject(int number, string content)
        {
            offsets.Add(memory.Position);
            WriteAscii(memory, $"{number} 0 obj\n{content}\nendobj\n", ascii);
        }

        WriteAscii(memory, "%PDF-1.4\n", ascii);
        WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        WriteObject(
            3,
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Format(A4Width)} {Format(A4Height)}] " +
            "/Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
        WriteObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        WriteObject(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        WriteObject(6, $"<< /Length {ascii.GetByteCount(stream)} >>\nstream\n{stream}endstream");

        var xrefStart = memory.Position;
        WriteAscii(memory, "xref\n", ascii);
        WriteAscii(memory, $"0 {offsets.Count + 1}\n", ascii);
        WriteAscii(memory, "0000000000 65535 f \n", ascii);
        foreach (var offset in offsets)
        {
            WriteAscii(memory, $"{offset:0000000000} 00000 n \n", ascii);
        }

        WriteAscii(memory, "trailer\n", ascii);
        WriteAscii(memory, $"<< /Size {offsets.Count + 1} /Root 1 0 R >>\n", ascii);
        WriteAscii(memory, "startxref\n", ascii);
        WriteAscii(memory, $"{xrefStart}\n", ascii);
        WriteAscii(memory, "%%EOF", ascii);

        return memory.ToArray();
    }

    private static IEnumerable<string> WrapText(string text, float width, float size, bool bold, int maxLines)
    {
        var normalized = NormalizeWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (MeasureWidth(candidate, size, bold) <= width)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current))
            {
                lines.Add(current);
            }

            current = word;
            if (lines.Count >= maxLines)
            {
                break;
            }
        }

        if (!string.IsNullOrEmpty(current) && lines.Count < maxLines)
        {
            lines.Add(current);
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
        }

        if (lines.Count == maxLines && words.Length > 0)
        {
            var consumed = string.Join(' ', lines);
            if (!string.Equals(consumed, normalized, StringComparison.Ordinal))
            {
                lines[^1] = Ellipsize(lines[^1], width, size, bold);
            }
        }

        return lines;
    }

    private static IEnumerable<string> WrapUrlText(string text, float width, float size, bool bold, int maxLines)
    {
        var normalized = NormalizeWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>();
        var current = new StringBuilder();
        var consumedAll = true;

        for (var index = 0; index < normalized.Length; index++)
        {
            var nextChar = normalized[index];
            current.Append(nextChar);

            if (MeasureWidth(current.ToString(), size, bold) <= width)
            {
                continue;
            }

            if (current.Length == 1)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            else
            {
                var breakIndex = FindPreferredBreak(current);
                if (breakIndex < 0)
                {
                    breakIndex = current.Length - 2;
                }

                var line = current.ToString(0, breakIndex + 1);
                var remainder = current.ToString(breakIndex + 1, current.Length - breakIndex - 1);
                lines.Add(line);
                current.Clear();
                current.Append(remainder);
            }

            if (lines.Count >= maxLines)
            {
                consumedAll = false;
                break;
            }
        }

        if (current.Length > 0 && lines.Count < maxLines)
        {
            lines.Add(current.ToString());
        }
        else if (current.Length > 0)
        {
            consumedAll = false;
        }

        if (!consumedAll && lines.Count > 0)
        {
            lines[^1] = Ellipsize(lines[^1], width, size, bold);
        }

        return lines;
    }

    private static int FindPreferredBreak(StringBuilder text)
    {
        for (var index = text.Length - 2; index >= 0; index--)
        {
            if (text[index] is '/' or '?' or '&' or '=' or '-' or '_')
            {
                return index;
            }
        }

        return -1;
    }

    private static string Ellipsize(string text, float width, float size, bool bold)
    {
        var candidate = text.TrimEnd();
        while (candidate.Length > 1 && MeasureWidth(candidate + "...", size, bold) > width)
        {
            candidate = candidate[..^1];
        }

        return candidate + "...";
    }

    private static float MeasureWidth(string text, float size, bool bold)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        var factor = bold ? 0.56f : 0.52f;
        return text.Length * size * factor;
    }

    private static string NormalizeWhitespace(string text)
    {
        return string.Join(' ', text
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string EncodeHex(string text)
    {
        var bytes = Latin1Encoding.GetBytes(text);
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string ColorComponent(byte component)
    {
        return (component / 255f).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Format(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static Encoding CreateLatin1Encoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }
}
