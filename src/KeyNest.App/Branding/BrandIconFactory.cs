using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KeyNest.App.Branding;

/// <summary>
/// 原创品牌图标：圆角巢形 + 巢内键点。
/// 优先读取内嵌的多尺寸 ICO（与 exe、快捷方式图标完全一致），资源缺失时才回退到 GDI+ 绘制。
/// </summary>
public static class BrandIconFactory
{
    private const string IconResourceName = "KeyNest.App.KeyNest.ico";

    private static readonly Color BrandColor = Color.FromArgb(46, 111, 99);
    private static readonly Color LineColor = Color.FromArgb(236, 247, 243);
    private static readonly Color DotColor = Color.FromArgb(217, 161, 91);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon CreateIcon(int size = 32)
    {
        using (var stream = typeof(BrandIconFactory).Assembly.GetManifestResourceStream(IconResourceName))
        {
            if (stream is not null)
            {
                using var loaded = new Icon(stream, size, size);
                return (Icon)loaded.Clone();
            }
        }

        using var bitmap = CreateBitmap(size);
        var handle = bitmap.GetHicon();

        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Bitmap CreateBitmap(int size)
    {
        var side = Math.Max(16, size);
        var bitmap = new Bitmap(side, side, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        float s = side;

        using (var background = new SolidBrush(BrandColor))
        using (var shape = RoundedRectangle(new RectangleF(s * 0.06f, s * 0.06f, s * 0.88f, s * 0.88f), s * 0.24f))
        {
            graphics.FillPath(background, shape);
        }

        using (var pen = new Pen(LineColor, Math.Max(1.4f, s * 0.072f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round
               })
        {
            graphics.DrawArc(pen, s * 0.235f, s * 0.40f, s * 0.53f, s * 0.34f, 0f, 180f);
            graphics.DrawArc(pen, s * 0.33f, s * 0.475f, s * 0.34f, s * 0.20f, 0f, 180f);
        }

        using (var dot = new SolidBrush(DotColor))
        {
            graphics.FillEllipse(dot, s * 0.44f, s * 0.535f, s * 0.12f, s * 0.12f);
        }

        return bitmap;
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();

        return path;
    }
}
