using ModulusStudio.Core.Imaging;
using ModulusStudio.Core.Models;
using ModulusStudio.Core.Packing;

namespace ModulusStudio.Core.Tests;

public class BitmapPackerTests
{
    [Fact]
    public void RowMajor_MsbFirst_PacksHorizontalBits()
    {
        var bmp = MonoBitmap.FromArt(
            "#.......",
            "........");

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions
        {
            ScanMode = ScanMode.RowMajor,
            BitOrder = BitOrder.MsbFirst
        });

        Assert.Equal(new byte[] { 0x80, 0x00 }, bytes);
    }

    [Fact]
    public void RowMajor_LsbFirst_ReversesBitOrder()
    {
        var bmp = MonoBitmap.FromArt(
            "#.......",
            "........");

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions
        {
            ScanMode = ScanMode.RowMajor,
            BitOrder = BitOrder.LsbFirst
        });

        Assert.Equal(new byte[] { 0x01, 0x00 }, bytes);
    }

    [Fact]
    public void ColumnMajor_PacksVertically()
    {
        var bmp = MonoBitmap.FromArt(
            ".......#",
            "........");

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions
        {
            ScanMode = ScanMode.ColumnMajor,
            BitOrder = BitOrder.MsbFirst
        });

        // 8 列，只有第 8 列有像素 → 前 7 字节为 0，最后一字节 0x80
        Assert.Equal(8, bytes.Length);
        Assert.Equal(0x80, bytes[7]);
    }

    [Fact]
    public void ColumnRow_UsesRowBandsFirst()
    {
        var bmp = new MonoBitmap(8, 16);
        bmp.Set(0, 8, true);

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions
        {
            ScanMode = ScanMode.ColumnRow,
            BitOrder = BitOrder.MsbFirst
        });

        // 第一段 8 行 8 字节全 0；第二段第一个字节为 0x80
        Assert.Equal(16, bytes.Length);
        Assert.Equal(0, bytes[0]);
        Assert.Equal(0x80, bytes[8]);
    }

    [Fact]
    public void Invert_ProducesPositiveCode()
    {
        var bmp = MonoBitmap.FromArt(
            "#.......",
            "........");

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions
        {
            ScanMode = ScanMode.RowMajor,
            BitOrder = BitOrder.MsbFirst,
            Invert = true
        });

        Assert.Equal(new byte[] { 0x7F, 0xFF }, bytes);
    }

    [Fact]
    public void NonMultipleOfEight_PadsWithBackground()
    {
        var bmp = MonoBitmap.FromArt("###");

        var bytes = BitmapPacker.Pack(bmp, new ScanOptions { ScanMode = ScanMode.RowMajor });

        Assert.Equal(new byte[] { 0xE0 }, bytes);
    }
}

public class ImagePackerTests
{
    [Fact]
    public void Rgb565_LowByteFirst_MatchesSample()
    {
        var frame = new ImageFrame(1, 1, new byte[]
        {
            0, 0, 255, 255 // BGR = 红
        });

        var bytes = ImagePacker.Pack(frame, new ImagePackOptions
        {
            Depth = ImageColorDepth.Color16Bit,
            LowByteFirst = true
        });

        Assert.Equal(new byte[] { 0x00, 0xF8 }, bytes);
    }

    [Fact]
    public void Monochrome_ThresholdsLuma()
    {
        var frame = new ImageFrame(8, 1, new byte[]
        {
            0, 0, 0, 255,
            0, 0, 0, 255,
            0, 0, 0, 255,
            0, 0, 0, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255
        });

        var bytes = ImagePacker.Pack(frame, new ImagePackOptions { Depth = ImageColorDepth.Monochrome });

        Assert.Equal(new byte[] { 0xF0 }, bytes);
    }

    [Fact]
    public void ReverseByte_ReversesBits()
    {
        Assert.Equal((byte)0x01, ImagePacker.ReverseByte(0x80));
        Assert.Equal((byte)0xAA, ImagePacker.ReverseByte(0x55));
    }
}
