using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Snow1970
{
    internal sealed class SnowDecoration
    {
        internal string Key, Support;
        internal bool Tree, Retiring;
        internal int Variant;
        internal double LocalX, X, Y, Opacity, Age;
    }

    internal sealed partial class World
    {
        internal List<SnowDecoration> Decorations = new List<SnowDecoration>();
        internal double DecorationIconPitch = 75;
        private double decorationClock;
        internal double DecorationSpan { get { return Settings.Clamp(DecorationIconPitch, 48, 192) * 5; } }
        internal int DecorationBudget { get { return Config.Quality == "Eco" ? 8 : Config.Quality == "Smooth" ? 32 : 20; } }
        internal double PeakCoverage(Pile pile) { return RangeCoverage(pile, 0, pile.Width); }
        private double RangeCoverage(Pile pile, double left, double right)
        {
            double exposed = 0, peak = 0;
            for (int i = Math.Max(0, (int)(left / pile.Cell)); i < pile.Height.Length && i * pile.Cell < right; i++)
                if (pile.Exposed[i])
                {
                    exposed += pile.Cell;
                    if (pile.Height[i] >= CapacityAt(pile, i) * .9) peak += pile.Cell;
                }
            return exposed < 90 ? 0 : peak / exposed;
        }
        private bool FitsDecorationDensity(double x, double y, List<SnowDecoration> existing)
        {
            List<double> row = new List<double> { x };
            foreach (SnowDecoration d in existing)
            {
                if (d.Retiring || Math.Abs(d.Y - y) > DecorationIconPitch) continue;
                double dx = Math.Abs(d.X - x);
                if (dx < 54 && Math.Abs(d.Y - y) < 54) return false;
                if (dx < DecorationSpan) row.Add(d.X);
            }
            row.Sort();
            for (int i = 0; i + 2 < row.Count; i++) if (row[i + 2] - row[i] < DecorationSpan - .5) return false;
            return true;
        }
        private double DecorationAnchor(Pile pile, double ideal, double left, double right)
        {
            double best = -1, distance = double.MaxValue;
            for (int i = Math.Max(0, (int)(left / pile.Cell)); i < pile.Height.Length && i * pile.Cell < right; i++)
            {
                double local = (i + .5) * pile.Cell;
                if (local < left + 25 || local > right - 25 || !pile.Exposed[i] || pile.Height[i] < CapacityAt(pile, i) * .7) continue;
                bool clear = true;
                for (int j = Math.Max(0, i - 5); j <= Math.Min(pile.Height.Length - 1, i + 5); j++) if (!pile.Exposed[j]) clear = false;
                double y = pile.Y - pile.At(pile.X + local) + 1;
                if (clear && Math.Abs(local - ideal) < distance && FitsDecorationDensity(pile.X + local, y, Decorations)) { best = local; distance = Math.Abs(local - ideal); }
            }
            return best;
        }
        private void StepDecorations(double dt)
        {
            List<SnowDecoration> accepted = new List<SnowDecoration>();
            for (int i = 0; i < Decorations.Count; i++)
            {
                SnowDecoration d = Decorations[i]; Pile support = SupportFor(d.Support); bool visible = Config.Decorations && support != null && !d.Retiring;
                if (visible)
                {
                    int cell = Math.Max(0, Math.Min(support.Height.Length - 1, (int)(d.LocalX / support.Cell)));
                    visible = d.LocalX < support.Width - 24 && support.Exposed[cell] && support.Height[cell] >= CapacityAt(support, cell) * .55;
                    d.X = Settings.Clamp(support.X + d.LocalX, Bounds.Left + 24, Bounds.Right - 24);
                    d.Y = Math.Max(Bounds.Top + 54, support.Y - support.At(d.X) + 1);
                    if (!FitsDecorationDensity(d.X, d.Y, accepted) || accepted.Count >= DecorationBudget) { d.Retiring = true; visible = false; }
                }
                if (visible) accepted.Add(d);
                d.Age += dt; d.Opacity = Settings.Clamp(d.Opacity + (visible ? dt * 2.8 : -dt * 4), 0, 1);
            }
            Decorations.RemoveAll(delegate(SnowDecoration d) { return d.Opacity <= 0 && d.Age > .1; });
            decorationClock -= dt;
            if (decorationClock > 0) return;
            decorationClock = .5;
            if (!Config.Decorations) return;
            double span = DecorationSpan, zoneWidth = span + 16;
            foreach (Pile pile in Piles)
            {
                if (pile.Width < span * .5 || pile.Y - Bounds.Top < 55) continue;
                int available = (int)Math.Floor(pile.Width / span * 2);
                for (int zone = 0; zone * zoneWidth < pile.Width && available > 0; zone++)
                {
                    double left = zone * zoneWidth, right = Math.Min(pile.Width, left + zoneWidth), width = right - left;
                    if (width < span * .3 || RangeCoverage(pile, left, right) < .8) continue;
                    int count = available >= 2 && width > span * .65 ? 2 : 1;
                    if (Decorations.Count + count > DecorationBudget) break;
                    uint seed = 2166136261; foreach (char c in pile.Key) seed = (seed ^ c) * 16777619;
                    double center = (left + right) * .5 + (seed % 5 - 2.0) * 2;
                    for (int slot = 0; slot < count; slot++)
                    {
                        string key = pile.Key + ":winter:" + zone + ":" + slot;
                        if (Decorations.Exists(delegate(SnowDecoration d) { return d.Key == key; })) continue;
                        double ideal = count == 1 ? center : center + (slot == 0 ? -38 : 38);
                        double local = DecorationAnchor(pile, ideal, left, right); if (local < 0) continue;
                        bool tree = ((slot + (int)(seed % 2) + zone) % 2) == 1;
                        Decorations.Add(new SnowDecoration { Key = key, Support = pile.Key, LocalX = local, Tree = tree, Variant = ((int)(seed % 3) + zone + (tree ? 1 : 0)) % (tree ? 2 : 3), X = pile.X + local, Y = pile.Y - pile.At(pile.X + local) + 1 });
                    }
                    available -= count;
                }
            }
        }
    }

    internal static class DecorationArt
    {
        private static Drawing.Bitmap[] snowmen = new Drawing.Bitmap[3], firs = new Drawing.Bitmap[2];
        private static readonly object gate = new object();
        internal static void Warm()
        {
            lock (gate)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (snowmen[i] == null) snowmen[i] = Load("Snow1970.Snowman" + (i + 1), i == 2 ? 34 : 32, i == 1 ? 46 : 43);
                    if (i < 2 && firs[i] == null) firs[i] = Load("Snow1970.Fir" + (i + 1), i == 1 ? 29 : 34, i == 1 ? 48 : 44);
                }
            }
        }
        internal static Drawing.Bitmap Sprite(bool tree, int variant = 0) { Warm(); return (tree ? firs : snowmen)[Math.Abs(variant) % (tree ? 2 : 3)]; }
        internal static Drawing.Bitmap Load(string name, int width, int height, int tileWidth = 48, int tileHeight = 54)
        {
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new FileNotFoundException("Missing winter sprite: " + name);
                using (Drawing.Bitmap image = new Drawing.Bitmap(stream))
                {
                    // Fit the generated alpha silhouette into the small cached render tile.
                    Drawing.Rectangle area = new Drawing.Rectangle(0, 0, image.Width, image.Height);
                    Drawing.Imaging.BitmapData pixels = image.LockBits(area, Drawing.Imaging.ImageLockMode.ReadOnly, Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte[] data = new byte[Math.Abs(pixels.Stride) * pixels.Height]; Marshal.Copy(pixels.Scan0, data, 0, data.Length); image.UnlockBits(pixels);
                    int left = image.Width, top = image.Height, right = 0, bottom = 0;
                    for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                        if (data[y * Math.Abs(pixels.Stride) + x * 4 + 3] > 24) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
                    Drawing.Rectangle source = new Drawing.Rectangle(left, top, Math.Max(1, right - left + 1), Math.Max(1, bottom - top + 1));
                    Drawing.Bitmap tile = new Drawing.Bitmap(tileWidth, tileHeight, Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Drawing.Graphics g = Drawing.Graphics.FromImage(tile))
                    {
                        g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        double scale = Math.Min((double)width / source.Width, (double)height / source.Height);
                        float w = (float)(source.Width * scale), h = (float)(source.Height * scale);
                        g.DrawImage(image, new Drawing.RectangleF((tileWidth - w) * .5f, tileHeight - 1 - h, w, h), source, Drawing.GraphicsUnit.Pixel);
                    }
                    return tile;
                }
            }
        }
    }

    internal sealed class DecorationLayer : Forms.Form
    {
        private IntPtr memoryDC, dib, oldDib;
        private Drawing.Bitmap surface;
        private Drawing.Graphics painter;
        private int alpha = -1, scene = -1;
        internal DecorationLayer()
        {
            Text = "Snow1970 winter toy"; FormBorderStyle = Forms.FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            AutoScaleMode = Forms.AutoScaleMode.None; StartPosition = Forms.FormStartPosition.Manual; Size = new Drawing.Size(48, 54);
            Native.BITMAPINFO info = new Native.BITMAPINFO { Size = 40, Width = Width, Height = -Height, Planes = 1, Bits = 32 };
            IntPtr bits; memoryDC = Native.CreateCompatibleDC(IntPtr.Zero); dib = Native.CreateDIBSection(IntPtr.Zero, ref info, 0, out bits, IntPtr.Zero, 0);
            if (dib == IntPtr.Zero) throw new InvalidOperationException("Cannot create winter sprite");
            oldDib = Native.SelectObject(memoryDC, dib); surface = new Drawing.Bitmap(Width, Height, Width * 4, Drawing.Imaging.PixelFormat.Format32bppPArgb, bits); painter = Drawing.Graphics.FromImage(surface);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override Forms.CreateParams CreateParams { get { Forms.CreateParams p = base.CreateParams; p.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000; return p; } }
        protected override void WndProc(ref Forms.Message m) { if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; } if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } base.WndProc(ref m); }
        protected override void OnPaint(Forms.PaintEventArgs e) { }
        protected override void OnPaintBackground(Forms.PaintEventArgs e) { }
        internal void Present(World world, SnowDecoration model, Forms.Form owner)
        {
            int x = (int)Math.Round(model.X - 24), y = (int)Math.Round(model.Y - 53);
            if (Left != x || Top != y) Native.SetWindowPos(Handle, IntPtr.Zero, x, y, 0, 0, 0x15);
            int opacity = (int)(Settings.Clamp(model.Opacity, 0, 1) * 15) * 17;
            if (opacity != alpha || scene != world.SceneVersion)
            {
                alpha = opacity; scene = world.SceneVersion;
                painter.ResetTransform(); painter.ResetClip(); painter.Clear(Drawing.Color.Transparent);
                if (world.NativeSky != null)
                {
                    painter.TranslateTransform(-Left, -Top); painter.SetClip(world.NativeSky, Drawing.Drawing2D.CombineMode.Intersect); painter.ResetTransform();
                }
                painter.DrawImageUnscaled(DecorationArt.Sprite(model.Tree, model.Variant), 0, 0); painter.Flush(Drawing.Drawing2D.FlushIntention.Sync);
                Native.POINT destination = new Native.POINT { X = Left, Y = Top }, source = new Native.POINT();
                Native.SIZE size = new Native.SIZE { Width = Width, Height = Height }; Native.BLEND blend = new Native.BLEND { Alpha = (byte)alpha, Format = 1 };
                Native.UpdateLayeredWindow(Handle, IntPtr.Zero, ref destination, ref size, memoryDC, ref source, 0, ref blend, 2);
            }
            if (!Visible) Show(owner);
        }
        internal void ExportSurface(string path) { surface.Save(path, Drawing.Imaging.ImageFormat.Png); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (painter != null) painter.Dispose(); if (surface != null) surface.Dispose(); if (memoryDC != IntPtr.Zero) { Native.SelectObject(memoryDC, oldDib); Native.DeleteObject(dib); Native.DeleteDC(memoryDC); memoryDC = IntPtr.Zero; } }
            base.Dispose(disposing);
        }
    }
}
