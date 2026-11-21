using ModulusStudio.Core.Models;

namespace ModulusStudio.Core.Packing;

/// <summary>把 1 位点阵按指定扫描方式打包为字模字节。</summary>
public static class BitmapPacker
{
    public static byte[] Pack(MonoBitmap bitmap, ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(options);

        var w = bitmap.Width;
        var h = bitmap.Height;
        var xBands = (w + 7) / 8;
        var yBands = (h + 7) / 8;
        var result = new List<byte>(w * h / 8 + 8);

        switch (options.ScanMode)
        {
            case ScanMode.RowMajor:
                for (var y = 0; y < h; y++)
                for (var bx = 0; bx < xBands; bx++)
                    result.Add(HorizontalByte(bitmap, bx * 8, y, options.BitOrder));
                break;

            case ScanMode.ColumnMajor:
                for (var x = 0; x < w; x++)
                for (var by = 0; by < yBands; by++)
                    result.Add(VerticalByte(bitmap, x, by * 8, options.BitOrder));
                break;

            case ScanMode.ColumnRow:
                for (var by = 0; by < yBands; by++)
                for (var x = 0; x < w; x++)
                    result.Add(VerticalByte(bitmap, x, by * 8, options.BitOrder));
                break;

            case ScanMode.RowColumn:
                for (var bx = 0; bx < xBands; bx++)
                for (var y = 0; y < h; y++)
                    result.Add(HorizontalByte(bitmap, bx * 8, y, options.BitOrder));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }

        if (options.Invert)
            for (var i = 0; i < result.Count; i++)
                result[i] = (byte)~result[i];

        return result.ToArray();
    }

    private static byte HorizontalByte(MonoBitmap bmp, int x0, int y, BitOrder order)
    {
        byte value = 0;
        for (var i = 0; i < 8; i++)
        {
            if (!bmp.IsOn(x0 + i, y)) continue;
            var bit = order == BitOrder.MsbFirst ? 7 - i : i;
            value |= (byte)(1 << bit);
        }
        return value;
    }

    private static byte VerticalByte(MonoBitmap bmp, int x, int y0, BitOrder order)
    {
        byte value = 0;
        for (var i = 0; i < 8; i++)
        {
            if (!bmp.IsOn(x, y0 + i)) continue;
            var bit = order == BitOrder.MsbFirst ? 7 - i : i;
            value |= (byte)(1 << bit);
        }
        return value;
    }
}
