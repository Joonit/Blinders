using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Blinders
{
    /// <summary>
    /// The Blinders logo, drawn in code so every size is crisp:
    /// a dark rounded square, one bright "active window" with an amber title bar,
    /// and (at larger sizes) two dimmed windows peeking out at the sides.
    /// </summary>
    static class Art
    {
        public const int On = 0, Off = 1, Paused = 2;

        static readonly Color Bg = Color.FromArgb(30, 35, 48);
        static readonly Color BgOff = Color.FromArgb(70, 76, 90);
        static readonly Color Side = Color.FromArgb(62, 69, 84);
        static readonly Color Window = Color.FromArgb(246, 247, 250);
        static readonly Color Accent = Color.FromArgb(255, 176, 32);
        static readonly Color OffLine = Color.FromArgb(190, 196, 206);

        public static readonly Color AccentColor = Accent;

        static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.5f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Bitmap Draw(int s, int state)
        {
            Bitmap b = new Bitmap(s, s, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (GraphicsPath bg = Round(new RectangleF(0, 0, s, s), s * 0.22f))
                using (Brush br = new SolidBrush(state == Off ? BgOff : Bg))
                    g.FillPath(br, bg);

                if (s >= 24)
                {
                    using (Brush br = new SolidBrush(Side))
                    {
                        using (GraphicsPath p = Round(new RectangleF(s * 0.07f, s * 0.32f, s * 0.12f, s * 0.38f), s * 0.03f)) g.FillPath(br, p);
                        using (GraphicsPath p = Round(new RectangleF(s * 0.81f, s * 0.32f, s * 0.12f, s * 0.38f), s * 0.03f)) g.FillPath(br, p);
                    }
                }

                float inset = s >= 24 ? 0.24f : 0.17f;
                RectangleF win = new RectangleF(s * inset, s * 0.2f, s * (1 - 2 * inset), s * 0.6f);
                float wr = Math.Max(1f, s * 0.07f);

                if (state == Off)
                {
                    using (GraphicsPath p = Round(win, wr))
                    using (Pen pen = new Pen(OffLine, Math.Max(1f, s / 14f)))
                        g.DrawPath(pen, p);
                }
                else
                {
                    using (GraphicsPath p = Round(win, wr))
                    using (Brush br = new SolidBrush(Window))
                        g.FillPath(br, p);
                    // amber title bar: the thing you are focused on
                    RectangleF bar = new RectangleF(win.X, win.Y, win.Width, Math.Max(2f, win.Height * 0.24f));
                    using (GraphicsPath p = Round(win, wr))
                    {
                        Region old = g.Clip;
                        g.SetClip(p);
                        using (Brush br = new SolidBrush(Accent)) g.FillRectangle(br, bar);
                        g.Clip = old;
                    }
                    if (state == Paused)
                    {
                        float bw = Math.Max(1.5f, s * 0.08f), bh = win.Height * 0.42f;
                        float cy = win.Y + win.Height * 0.36f, cx = win.X + win.Width / 2f;
                        using (Brush br = new SolidBrush(Bg))
                        {
                            g.FillRectangle(br, cx - bw * 1.6f, cy, bw, bh);
                            g.FillRectangle(br, cx + bw * 0.6f, cy, bw, bh);
                        }
                    }
                }
            }
            return b;
        }

        /// <summary>A multi-resolution .ico (PNG frames).</summary>
        public static byte[] IcoBytes(int[] sizes, int state)
        {
            List<byte[]> pngs = new List<byte[]>();
            foreach (int s in sizes)
                using (Bitmap b = Draw(s, state))
                using (MemoryStream ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                }
            using (MemoryStream o = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(o))
            {
                w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = sizes[i];
                    w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
                    w.Write((byte)0); w.Write((byte)0);
                    w.Write((short)1); w.Write((short)32);
                    w.Write(pngs[i].Length); w.Write(offset);
                    offset += pngs[i].Length;
                }
                foreach (byte[] p in pngs) w.Write(p);
                w.Flush();
                return o.ToArray();
            }
        }

        public static readonly int[] AppSizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

        /// <summary>Icon at the given pixel size, picked from a multi-size icon (stays sharp at any DPI).</summary>
        public static Icon MakeIcon(int state, int size)
        {
            using (MemoryStream ms = new MemoryStream(IcoBytes(AppSizes, state)))
                return new Icon(ms, size, size);
        }
    }
}
