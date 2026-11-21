using System.Text;

namespace ModulusStudio.Core.Fonts;

/// <summary>字符集与 GB2312 相关工具。</summary>
public static class CharsetUtil
{
    private static bool _providerRegistered;

    public static Encoding Gb2312
    {
        get
        {
            if (!_providerRegistered)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _providerRegistered = true;
            }
            return Encoding.GetEncoding(936);
        }
    }

    /// <summary>按 Unicode 文本元素拆分为字符（正确处理代理对）。</summary>
    public static IReadOnlyList<string> SplitChars(string text)
    {
        var list = new List<string>(text.Length);
        foreach (var rune in text.EnumerateRunes())
            list.Add(rune.ToString());
        return list;
    }

    /// <summary>生成 GB2312 一级+二级汉字表（按区位顺序）。</summary>
    public static IReadOnlyList<string> GetGb2312Hanzi()
    {
        var encoding = Gb2312;
        var list = new List<string>(6763);
        for (var hi = 0xB0; hi <= 0xF7; hi++)
        for (var lo = 0xA1; lo <= 0xFE; lo++)
        {
            var bytes = new[] { (byte)hi, (byte)lo };
            var text = encoding.GetString(bytes);
            if (text.Length == 0 || text[0] == '\uFFFD') continue;
            list.Add(text);
        }
        return list;
    }

    /// <summary>GB2312 全角符号区（A1A1–A9FE）。</summary>
    public static IReadOnlyList<string> GetGb2312Symbols()
    {
        var encoding = Gb2312;
        var list = new List<string>(846);
        for (var hi = 0xA1; hi <= 0xA9; hi++)
        for (var lo = 0xA1; lo <= 0xFE; lo++)
        {
            var text = encoding.GetString(new[] { (byte)hi, (byte)lo });
            if (text.Length == 0 || text[0] == '\uFFFD') continue;
            list.Add(text);
        }
        return list;
    }
}
