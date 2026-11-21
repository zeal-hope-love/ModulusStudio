using ModulusStudio.Core.Fonts;
using ModulusStudio.Core.Models;

namespace ModulusStudio.Fonts;

/// <summary>字体点阵光栅化后端。</summary>
public interface IFontRasterizer : IDisposable
{
    /// <summary>后端名称，用于界面展示。</summary>
    string Name { get; }

    /// <summary>把一段文本渲染为 width × height 的单色点阵（超出部分裁切）。</summary>
    MonoBitmap Rasterize(string text, FontSpec spec);
}
