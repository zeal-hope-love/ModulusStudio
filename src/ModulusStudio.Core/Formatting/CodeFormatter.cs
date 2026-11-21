using System.Text;

namespace ModulusStudio.Core.Formatting;

/// <summary>输出代码格式参数。</summary>
public sealed class CodeFormatOptions
{
    public string BytePrefix { get; set; } = "0x";
    public string ByteSuffix { get; set; } = ",";
    public int BytesPerLine { get; set; } = 16;
    public bool UpperCase { get; set; } = true;
    public bool Decimal { get; set; }
    public string LinePrefix { get; set; } = "";
    public string LineSuffix { get; set; } = "";
    public string GlyphSeparator { get; set; } = "";
    public string Header { get; set; } = "";
    public string Footer { get; set; } = "";

    /// <summary>行尾注释的字符前缀（A51 为 <c>"</c>）。</summary>
    public string CommentPrefix { get; set; } = "";

    /// <summary>行尾注释的字符后缀（A51 为 <c>",</c>）。</summary>
    public string CommentSuffix { get; set; } = "";

    /// <summary>与原软件一致：每行最后一个字节的数据后缀去掉最后一个字符。</summary>
    public bool TrimLastByteSuffix { get; set; }

    public static CodeFormatOptions CreateC51() => new()
    {
        BytePrefix = "0x",
        ByteSuffix = ",",
        BytesPerLine = 16,
        UpperCase = true,
        LineSuffix = "",
        GlyphSeparator = "",
        Header = "",
        Footer = ""
    };

    public static CodeFormatOptions CreateA51() => new()
    {
        BytePrefix = "",
        ByteSuffix = "H ",
        BytesPerLine = 16,
        UpperCase = true,
        LinePrefix = "DB ",
        LineSuffix = ";",
        CommentPrefix = "\"",
        CommentSuffix = "\",",
        TrimLastByteSuffix = true,
        GlyphSeparator = "",
        Header = "",
        Footer = ""
    };

    public static CodeFormatOptions CreateDecimal() => new()
    {
        BytePrefix = "",
        ByteSuffix = ",",
        BytesPerLine = 16,
        Decimal = true,
        GlyphSeparator = ""
    };
}

/// <summary>把字模字节数组格式化为可直接粘贴的源代码文本。</summary>
public static class CodeFormatter
{
    public static string Format(
        IReadOnlyList<byte> data,
        CodeFormatOptions options,
        string? glyphHeader = null,
        string? trailingComment = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);

        var perLine = Math.Clamp(options.BytesPerLine, 1, 256);
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(options.Header))
        {
            sb.Append(options.Header);
            if (!options.Header.EndsWith('\n')) sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(glyphHeader))
        {
            sb.Append(glyphHeader);
            sb.AppendLine();
        }

        for (var i = 0; i < data.Count; i++)
        {
            if (i % perLine == 0)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(options.LinePrefix);
            }

            sb.Append(options.BytePrefix);
            sb.Append(options.Decimal
                ? data[i].ToString()
                : (options.UpperCase ? data[i].ToString("X2") : data[i].ToString("x2")));

            // 原软件在每行最后一个字节处把数据后缀截掉一个字符（"H " → "H"）
            var atLineEnd = i % perLine == perLine - 1 || i == data.Count - 1;
            sb.Append(options.TrimLastByteSuffix && atLineEnd && options.ByteSuffix.Length > 0
                ? options.ByteSuffix[..^1]
                : options.ByteSuffix);
        }

        if (data.Count > 0) sb.Append(options.LineSuffix);
        if (trailingComment != null) sb.Append(trailingComment);

        if (!string.IsNullOrEmpty(options.Footer))
        {
            sb.AppendLine();
            sb.Append(options.Footer);
        }

        return sb.ToString();
    }

    public static string Format(IReadOnlyList<byte[]> glyphs, CodeFormatOptions options, string glyphSeparator)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < glyphs.Count; i++)
        {
            if (i > 0) sb.Append(glyphSeparator);
            sb.Append(Format(glyphs[i], options));
        }
        return sb.ToString();
    }
}
