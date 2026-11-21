using System.Buffers.Binary;

namespace ModulusStudio.Core.Imaging;

/// <summary>把图像像素缓冲打包为取模数据。</summary>
public static class ImagePacker
{
    public static byte[] Pack(ImageFrame source, ImagePackOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        var frame = Prepare(source, options);
        var bytes = options.Depth switch
        {
            ImageColorDepth.Monochrome => PackMonochrome(frame, options),
            ImageColorDepth.Gray4 => PackGray2Bits(frame, options),
            ImageColorDepth.Gray16 => PackGrayNibbles(frame, options),
            ImageColorDepth.Gray256 => PackGray(frame, options),
            ImageColorDepth.Color4096 => PackColor(frame, options, 12),
            ImageColorDepth.Color16Bit => PackColor(frame, options, 16),
            ImageColorDepth.Color18Bit => PackColor(frame, options, 18),
            ImageColorDepth.Color24Bit => PackColor(frame, options, 24),
            ImageColorDepth.Color32Bit => PackColor(frame, options, 32),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };

        if (options.ReverseBits)
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = ReverseByte(bytes[i]);

        if (options.IncludeHeader)
            return WrapBmp(frame, bytes, options);

        return bytes;
    }

    private static ImageFrame Prepare(ImageFrame source, ImagePackOptions options)
    {
        var frame = source;
        if (options.MaxWidth > 0 || options.MaxHeight > 0)
        {
            var maxW = options.MaxWidth > 0 ? options.MaxWidth : source.Width;
            var maxH = options.MaxHeight > 0 ? options.MaxHeight : source.Height;
            if (source.Width > maxW || source.Height > maxH)
            {
                var scale = Math.Min((double)maxW / source.Width, (double)maxH / source.Height);
                var w = Math.Max(1, (int)(source.Width * scale));
                var h = Math.Max(1, (int)(source.Height * scale));
                frame = source.Resize(w, h);
            }
        }

        if (options.Brightness == 0 && options.Contrast == 0 && !options.InvertColors) return frame;

        var copy = frame.Clone();
        var contrast = (options.Contrast + 100) / 100.0;
        var brightness = options.Brightness * 255 / 100;
        for (var i = 0; i < copy.Bgra.Length; i += 4)
        {
            for (var c = 0; c < 3; c++)
            {
                var v = (copy.Bgra[i + c] - 128) * contrast + 128 + brightness;
                if (options.InvertColors) v = 255 - v;
                copy.Bgra[i + c] = Clamp(v);
            }
        }
        return copy;
    }

    private static byte Clamp(double v) => v <= 0 ? (byte)0 : v >= 255 ? (byte)255 : (byte)v;

    private static IEnumerable<int> Walk(int width, int height, ImagePackOptions options)
    {
        var vertical = options.ScanDirection == ImageScanDirection.Vertical;
        if (!vertical)
        {
            for (var r = 0; r < height; r++)
            {
                var y = options.BottomToTop ? height - 1 - r : r;
                for (var c = 0; c < width; c++)
                {
                    var x = options.RightToLeft ? width - 1 - c : c;
                    yield return y * width + x;
                }
            }
        }
        else
        {
            for (var c = 0; c < width; c++)
            {
                var x = options.RightToLeft ? width - 1 - c : c;
                for (var r = 0; r < height; r++)
                {
                    var y = options.BottomToTop ? height - 1 - r : r;
                    yield return y * width + x;
                }
            }
        }
    }

    private static byte PackBits(IReadOnlyList<bool> bits, ImagePackOptions options)
    {
        byte value = 0;
        for (var i = 0; i < bits.Count && i < 8; i++)
        {
            if (!bits[i]) continue;
            var bit = options.MsbFirst ? 7 - i : i;
            value |= (byte)(1 << bit);
        }
        return value;
    }

    private static byte[] PackMonochrome(ImageFrame frame, ImagePackOptions options)
    {
        var output = new List<byte>();
        var buffer = new List<bool>(8);
        foreach (var index in Walk(frame.Width, frame.Height, options))
        {
            var (b, g, r, _) = frame.GetPixel(index % frame.Width, index / frame.Width);
            var luma = (r * 77 + g * 150 + b * 29) >> 8;
            var on = luma < options.Threshold;
            if (options.InvertMono) on = !on;
            buffer.Add(on);
            if (buffer.Count == 8)
            {
                output.Add(PackBits(buffer, options));
                buffer.Clear();
            }
        }
        if (buffer.Count > 0)
        {
            while (buffer.Count < 8) buffer.Add(false);
            output.Add(PackBits(buffer, options));
        }
        return output.ToArray();
    }

    private static byte[] PackGrayNibbles(ImageFrame frame, ImagePackOptions options)
    {
        var output = new List<byte>();
        var nibbles = new List<int>(2);
        foreach (var index in Walk(frame.Width, frame.Height, options))
        {
            var (b, g, r, _) = frame.GetPixel(index % frame.Width, index / frame.Width);
            var luma = (r * 77 + g * 150 + b * 29) >> 8;
            nibbles.Add(luma >> 4);
            if (nibbles.Count == 2)
            {
                var high = nibbles[0] & 0xF;
                var low = nibbles[1] & 0xF;
                output.Add(options.MsbFirst ? (byte)((high << 4) | low) : (byte)((low << 4) | high));
                nibbles.Clear();
            }
        }
        if (nibbles.Count == 1)
            output.Add(options.MsbFirst ? (byte)((nibbles[0] & 0xF) << 4) : (byte)(nibbles[0] & 0xF));
        return output.ToArray();
    }

    private static byte[] PackGray2Bits(ImageFrame frame, ImagePackOptions options)
    {
        var output = new List<byte>();
        var levels = new List<int>(4);
        foreach (var index in Walk(frame.Width, frame.Height, options))
        {
            var (b, g, r, _) = frame.GetPixel(index % frame.Width, index / frame.Width);
            var luma = (r * 77 + g * 150 + b * 29) >> 8;
            levels.Add(luma * 3 / 255);
            if (levels.Count == 4)
            {
                var packed = options.MsbFirst
                    ? (levels[0] << 6) | (levels[1] << 4) | (levels[2] << 2) | levels[3]
                    : (levels[3] << 6) | (levels[2] << 4) | (levels[1] << 2) | levels[0];
                output.Add((byte)packed);
                levels.Clear();
            }
        }
        if (levels.Count > 0)
        {
            while (levels.Count < 4) levels.Add(0);
            var packed = options.MsbFirst
                ? (levels[0] << 6) | (levels[1] << 4) | (levels[2] << 2) | levels[3]
                : (levels[3] << 6) | (levels[2] << 4) | (levels[1] << 2) | levels[0];
            output.Add((byte)packed);
        }
        return output.ToArray();
    }

    private static byte[] PackGray(ImageFrame frame, ImagePackOptions options)
    {
        var output = new List<byte>(frame.Width * frame.Height);
        foreach (var index in Walk(frame.Width, frame.Height, options))
        {
            var (b, g, r, _) = frame.GetPixel(index % frame.Width, index / frame.Width);
            output.Add((byte)((r * 77 + g * 150 + b * 29) >> 8));
        }
        return output.ToArray();
    }

    private static byte[] PackColor(ImageFrame frame, ImagePackOptions options, int bits)
    {
        var output = new List<byte>(frame.Width * frame.Height * bits / 8);
        foreach (var index in Walk(frame.Width, frame.Height, options))
        {
            var (b, g, r, a) = frame.GetPixel(index % frame.Width, index / frame.Width);
            switch (bits)
            {
                case 12:
                {
                    var value = (ushort)(((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4));
                    AddWord(output, value, options);
                    break;
                }
                case 16:
                {
                    var value = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
                    AddWord(output, value, options);
                    break;
                }
                case 18:
                {
                    output.Add((byte)(r & 0xFC));
                    output.Add((byte)(g & 0xFC));
                    output.Add((byte)(b & 0xFC));
                    break;
                }
                case 24:
                {
                    if (options.LowByteFirst)
                        AddRgb(output, b, g, r);
                    else
                        AddRgb(output, r, g, b);
                    break;
                }
                case 32:
                {
                    if (options.LowByteFirst)
                        AddRgb(output, b, g, r);
                    else
                        AddRgb(output, r, g, b);
                    output.Add(a);
                    break;
                }
            }
        }
        return output.ToArray();
    }

    private static void AddRgb(List<byte> output, byte c0, byte c1, byte c2)
    {
        output.Add(c0);
        output.Add(c1);
        output.Add(c2);
    }

    private static void AddWord(List<byte> output, ushort value, ImagePackOptions options)
    {
        if (options.LowByteFirst)
        {
            output.Add((byte)(value & 0xFF));
            output.Add((byte)(value >> 8));
        }
        else
        {
            output.Add((byte)(value >> 8));
            output.Add((byte)(value & 0xFF));
        }
    }

    public static byte ReverseByte(byte value)
    {
        value = (byte)((value & 0xF0) >> 4 | (value & 0x0F) << 4);
        value = (byte)((value & 0xCC) >> 2 | (value & 0x33) << 2);
        value = (byte)((value & 0xAA) >> 1 | (value & 0x55) << 1);
        return value;
    }

    private static byte[] WrapBmp(ImageFrame frame, byte[] data, ImagePackOptions options)
    {
        var bitCount = options.Depth switch
        {
            ImageColorDepth.Monochrome => 1,
            ImageColorDepth.Gray256 => 8,
            ImageColorDepth.Color24Bit => 24,
            _ => 0
        };
        if (bitCount == 0) return data;

        var paletteSize = bitCount switch
        {
            1 => 2,
            8 => 256,
            _ => 0
        };
        var headerSize = 14 + 40 + paletteSize * 4;
        var rowSize = ((frame.Width * bitCount + 31) / 32) * 4;
        var expected = rowSize * frame.Height;

        var output = new byte[headerSize + expected];
        var span = output.AsSpan();

        span[0] = (byte)'B';
        span[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(span[2..], output.Length);
        BinaryPrimitives.WriteInt32LittleEndian(span[10..], headerSize);

        BinaryPrimitives.WriteInt32LittleEndian(span[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(span[18..], frame.Width);
        BinaryPrimitives.WriteInt32LittleEndian(span[22..], frame.Height);
        BinaryPrimitives.WriteInt16LittleEndian(span[26..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[28..], (short)bitCount);
        BinaryPrimitives.WriteInt32LittleEndian(span[34..], expected);
        BinaryPrimitives.WriteInt32LittleEndian(span[36..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(span[46..], paletteSize);

        if (bitCount == 1)
        {
            span[54 + 0] = 0;
            span[54 + 1] = 0;
            span[54 + 2] = 0;
            span[54 + 4] = 255;
            span[54 + 5] = 255;
            span[54 + 6] = 255;
        }
        else if (bitCount == 8)
        {
            for (var i = 0; i < 256; i++)
            {
                var p = 54 + i * 4;
                span[p] = (byte)i;
                span[p + 1] = (byte)i;
                span[p + 2] = (byte)i;
            }
        }

        // 数据按 BMP 习惯自底向上写入，行尾补齐
        var srcStride = (frame.Width * bitCount + 7) / 8;
        for (var y = 0; y < frame.Height; y++)
        {
            var srcRow = (frame.Height - 1 - y) * srcStride;
            var dstRow = headerSize + y * rowSize;
            Array.Copy(data, srcRow, output, dstRow, Math.Min(srcStride, rowSize));
        }

        return output;
    }
}
