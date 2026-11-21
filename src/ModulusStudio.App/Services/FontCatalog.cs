using System.Windows.Media;
using ModulusStudio.Core.Fonts;

namespace ModulusStudio.App.Services;

public static class FontCatalog
{
    private static IReadOnlyList<string>? _cached;

    /// <summary>枚举系统字体（优先中文名称，GDI 与 Skia 均可识别）。</summary>
    public static IReadOnlyList<string> GetFamilies()
    {
        if (_cached is not null) return _cached;

        var names = new List<string>();
        foreach (var family in System.Windows.Media.Fonts.SystemFontFamilies)
        {
            var name = family.FamilyNames.TryGetValue(
                System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn"), out var zh)
                ? zh
                : family.Source;

            if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
        }

        _cached = names.Distinct().OrderBy(n => n, StringComparer.CurrentCulture).ToList();
        return _cached;
    }
}
