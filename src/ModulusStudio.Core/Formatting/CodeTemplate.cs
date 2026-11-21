namespace ModulusStudio.Core.Formatting;

/// <summary>
/// 可编辑的输出模板。模板中形如 <c>/*_xxx_*/</c> 的占位注释会被替换为实际数据，
/// 其余内容原样输出，因此模板本身就是合法的 C/A51 代码。
/// </summary>
public static class CodeTemplate
{
    /// <summary>默认 C51 模板。</summary>
    public const string C51 = """
// 字符 /*_char_*/（索引 /*_index_*/）
// /*_w_*/ x /*_h_*/，共 /*_len_*/ 字节
const unsigned char glyph_/*_index_*/[] = {
/*_data_*/
};
""";

    /// <summary>默认 A51 模板。</summary>
    public const string A51 = "/*_data_*/";

    public const string PlaceholderHint =
        "/*_char_*/ 字符  /*_index_*/ 序号  /*_w_*/ 宽  /*_h_*/ 高  " +
        "/*_len_*/ 字节数  /*_data_*/ 字模数据  /*_comment_*/ 行尾注释";

    public static string GetDefault(int formatIndex) => formatIndex == 1 ? A51 : C51;

    /// <summary>按模板生成一个字符的输出文本。</summary>
    public static string Apply(
        string template,
        IReadOnlyList<byte> data,
        CodeFormatOptions options,
        int width,
        int height,
        string glyph,
        int index,
        string? trailingComment = null)
    {
        if (string.IsNullOrWhiteSpace(template)) template = C51;

        var block = CodeFormatter.Format(data, options, null, trailingComment);

        // 数据块多行时，沿用占位符所在行的缩进，保证生成代码的排版
        const string marker = "/*_data_*/";
        var at = template.IndexOf(marker, StringComparison.Ordinal);
        if (at >= 0)
        {
            var lineStart = template.LastIndexOf('\n', Math.Max(0, at - 1)) + 1;
            var indent = template[lineStart..at];
            if (indent.Trim().Length != 0) indent = string.Empty;
            if (indent.Length > 0)
                block = block.Replace("\r\n", "\r\n" + indent).Replace("\n", "\n" + indent);
        }

        return template
            .Replace(marker, block)
            .Replace("/*_w_*/", width.ToString())
            .Replace("/*_h_*/", height.ToString())
            .Replace("/*_len_*/", data.Count.ToString())
            .Replace("/*_char_*/", glyph)
            .Replace("/*_index_*/", index.ToString())
            .Replace("/*_comment_*/", trailingComment ?? string.Empty);
    }
}
