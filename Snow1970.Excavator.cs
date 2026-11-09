using System;
using System.Windows;
using System.Windows.Media;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Snow1970
{
    internal sealed partial class World
    {
        internal bool VehicleAllowed = true;
        internal double DiggerX, DiggerY, DiggerBucket, DiggerAge, DiggerClearedMass, DiggerLift, DiggerCurl;
        internal int DiggerDirection = -1, DiggerMode;
        internal bool DiggerVisible;
        internal Point DiggerOrigin = new Point(double.NaN, double.NaN);
        private double diggerTarget, diggerSearch, diggerScoopClock, diggerUnloadClock, diggerDeployY;
        private void SelectDigTarget(Pile floor)
        {
            double score = -1, aim = DiggerX;
            for (int i = 2; i < floor.Height.Length - 2; i += 3)
            {
                if (!floor.Exposed[i]) continue;
                double x = floor.X + (i + .5) * floor.Cell;
                if (x < Bounds.Left + 45 || x > Bounds.Right - 45 || Math.Abs(x - DiggerX) < 45) continue;
                double h = (floor.Height[i - 2] + floor.Height[i - 1] + floor.Height[i] * 2 + floor.Height[i + 1] + floor.Height[i + 2]) / 6;
                double value = h - Math.Abs(x - DiggerX) * .002;
                if (value > score) { score = value; aim = x; }
            }
            if (score < floor.MaxHeight * .21)
                diggerTarget = DiggerDirection < 0 ? Bounds.Left + 38 : Bounds.Right - 38;
            else diggerTarget = Settings.Clamp(aim + (aim > DiggerX ? 75 : -75), Bounds.Left + 38, Bounds.Right - 38);
            if (Math.Abs(diggerTarget - DiggerX) < 12)
                diggerTarget = DiggerX < Bounds.X + Bounds.Width * .5 ? Bounds.Right - 38 : Bounds.Left + 38;
            diggerSearch = 2.2;
        }
        private void StepExcavator(double dt)
        {
            if (!Config.Excavator || !VehicleAllowed)
            {
                if (DiggerBucket > .01) DiggerClearedMass += DiggerBucket;
                DiggerVisible = false; DiggerAge = DiggerBucket = 0; DiggerMode = 0; return;
            }
            Pile floor = Piles[0]; double capacity = Config.Extreme ? 900 : 600;
            if (!DiggerVisible)
            {
                DiggerVisible = true; DiggerAge = 0;
                DiggerX = double.IsNaN(DiggerOrigin.X) ? Bounds.Right - 76 : Settings.Clamp(DiggerOrigin.X, Bounds.Left + 37, Bounds.Right - 37);
                DiggerY = double.IsNaN(DiggerOrigin.Y) ? Bounds.Top + 110 : DiggerOrigin.Y;
                diggerDeployY = DiggerY; diggerSearch = diggerScoopClock = 0; DiggerDirection = -1; DiggerMode = 0;
            }
            DiggerAge += dt;
            double surface = floor.Y - floor.At(DiggerX);
            if (DiggerAge < 1)
            {
                double t = DiggerAge, ease = t * t * (3 - 2 * t);
                DiggerY = diggerDeployY + (surface - diggerDeployY) * ease; return;
            }
            if (DiggerMode == 0 && DiggerBucket >= capacity * .95)
            {
                DiggerMode = 1;
                diggerTarget = DiggerX - Bounds.Left < Bounds.Right - DiggerX ? Bounds.Left - 52 : Bounds.Right + 52;
            }
            if (DiggerMode == 0)
            {
                diggerSearch -= dt;
                if (diggerSearch <= 0 || Math.Abs(DiggerX - diggerTarget) < 6) SelectDigTarget(floor);
            }
            double targetLift = DiggerMode == 0 ? .1 + .1 * Math.Sin(Time * 4) : .95;
            double targetCurl = DiggerMode == 0 ? .15 + .18 * Math.Sin(Time * 3.1) : DiggerMode == 2 ? -.8 : DiggerMode == 3 ? 0 : .8;
            DiggerLift += (targetLift - DiggerLift) * Math.Min(1, dt * 8);
            DiggerCurl += (targetCurl - DiggerCurl) * Math.Min(1, dt * 8);
            double delta = diggerTarget - DiggerX;
            if (Math.Abs(delta) > 2) DiggerDirection = delta < 0 ? -1 : 1;
            if (DiggerMode != 2)
            {
                double x = DiggerX + Math.Sign(delta) * Math.Min(Math.Abs(delta), dt * (DiggerMode == 0 ? 88 : 118));
                DiggerX = DiggerMode == 0 ? Settings.Clamp(x, Bounds.Left + 37, Bounds.Right - 37) : x;
            }
            if (DiggerMode == 1 && Math.Abs(DiggerX - diggerTarget) < 1) { DiggerMode = 2; diggerUnloadClock = 0; }
            double track = floor.Y - (floor.At(DiggerX - 12) + floor.At(DiggerX) * 2 + floor.At(DiggerX + 12)) * .25;
            DiggerY += (track - DiggerY) * Math.Min(1, dt * 16);
            if (DiggerMode == 2)
            {
                diggerUnloadClock += dt;
                // The entire sprite and its load are beyond this monitor before unloading.
                if (diggerUnloadClock > .35 && DiggerBucket > 0) { DiggerClearedMass += DiggerBucket; DiggerBucket = 0; }
                if (diggerUnloadClock > .8)
                {
                    DiggerMode = 3; DiggerDirection = -DiggerDirection;
                    diggerTarget = DiggerX < Bounds.Left ? Bounds.Left + 38 : Bounds.Right - 38;
                }
                return;
            }
            if (DiggerMode == 3 && Math.Abs(DiggerX - diggerTarget) < 1) { DiggerMode = 0; SelectDigTarget(floor); }
            if (DiggerMode != 0) return;
            diggerScoopClock += dt; if (diggerScoopClock < .05) return;
            double scoopDt = Math.Min(.1, diggerScoopClock); diggerScoopClock = 0;
            double tip = DiggerX + DiggerDirection * 29, changed = 0;
            int first = Math.Max(0, (int)((tip - 16 - floor.X) / floor.Cell)), last = Math.Min(floor.Height.Length - 1, (int)((tip + 16 - floor.X) / floor.Cell));
            double keep = Math.Max(2, floor.MaxHeight * .2);
            for (int i = first; i <= last; i++)
            {
                if (!floor.Exposed[i]) continue;
                double take = Math.Min(Math.Max(0, floor.Height[i] - keep), Math.Min(scoopDt * 34, (capacity - DiggerBucket) / floor.Cell));
                floor.Height[i] -= take; DiggerBucket += take * floor.Cell; changed += take;
            }
            if (changed > .001) { floor.BuildShape(); TerrainVersion++; }
        }
    }

    internal sealed class ExcavatorGlyph : FrameworkElement
    {
        internal ExcavatorGlyph() { Width = 21; Height = 20; IsHitTestVisible = false; }
        protected override void OnRender(DrawingContext dc)
        {
            Pen line = Art.Pen(UiTheme.Get(this, "Text"), 1.35);
            dc.DrawRoundedRectangle(null, line, new Rect(2, 14, 13, 4), 2, 2);
            dc.DrawRoundedRectangle(null, line, new Rect(4, 4, 7, 10), 1.5, 1.5);
            dc.DrawLine(line, new Point(11, 7), new Point(16, 5));
            dc.DrawLine(line, new Point(16, 5), new Point(19, 13));
            dc.DrawLine(line, new Point(16, 14), new Point(20, 14));
            dc.DrawLine(line, new Point(6, 9), new Point(9, 9));
        }
    }

    internal static class ExcavatorArt
    {
        private static Drawing.Bitmap sprite, body, boom, bucket;
        private static readonly object gate = new object();
        internal static void Warm()
        {
            lock (gate) if (sprite == null)
            {
                sprite = DecorationArt.Load("Snow1970.ExcavatorSanta", 79, 54, 82, 56);
                body = Part(new Drawing.PointF[] { new Drawing.PointF(0,0), new Drawing.PointF(39,0), new Drawing.PointF(41,28), new Drawing.PointF(54,36), new Drawing.PointF(54,56), new Drawing.PointF(0,56) });
                boom = Part(new Drawing.PointF[] { new Drawing.PointF(39,13), new Drawing.PointF(60,9), new Drawing.PointF(71,18), new Drawing.PointF(75,39), new Drawing.PointF(66,43), new Drawing.PointF(59,25), new Drawing.PointF(40,33) });
                bucket = Part(new Drawing.PointF[] { new Drawing.PointF(66,39), new Drawing.PointF(82,39), new Drawing.PointF(82,56), new Drawing.PointF(55,56), new Drawing.PointF(57,48), new Drawing.PointF(64,43) });
            }
        }
        private static Drawing.Bitmap Part(Drawing.PointF[] points)
        {
            Drawing.Bitmap image = new Drawing.Bitmap(82, 58, Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Drawing.Graphics g = Drawing.Graphics.FromImage(image)) using (Drawing.Drawing2D.GraphicsPath clip = new Drawing.Drawing2D.GraphicsPath())
            { clip.AddPolygon(points); g.SetClip(clip); g.DrawImageUnscaled(sprite, 0, 0); }
            return image;
        }
        internal static Drawing.Bitmap Sprite() { Warm(); return sprite; }
        internal static void Draw(Drawing.Graphics g, double lift, double curl, bool loaded)
        {
            Warm(); g.DrawImageUnscaled(body, 0, 0); Drawing.Drawing2D.GraphicsState original = g.Save();
            using (Drawing.Drawing2D.Matrix joint = new Drawing.Drawing2D.Matrix()) { joint.RotateAt((float)(-16 * lift), new Drawing.PointF(40,32)); g.MultiplyTransform(joint, Drawing.Drawing2D.MatrixOrder.Prepend); }
            g.DrawImageUnscaled(boom, 0, 0);
            using (Drawing.Drawing2D.Matrix joint = new Drawing.Drawing2D.Matrix()) { joint.RotateAt((float)(-28 * curl), new Drawing.PointF(68,41)); g.MultiplyTransform(joint, Drawing.Drawing2D.MatrixOrder.Prepend); }
            g.DrawImageUnscaled(bucket, 0, 0);
            if (loaded) using (Drawing.SolidBrush snow = new Drawing.SolidBrush(Drawing.Color.FromArgb(246,250,252))) { g.FillEllipse(snow,64,43,13,8); g.FillEllipse(snow,67,40,8,7); }
            g.Restore(original);
        }
    }

    // A tiny cached alpha sprite. Motion moves its window without repainting the desktop.
    internal sealed class ExcavatorLayer : Forms.Form
    {
        private IntPtr memoryDC, dib, oldDib;
        private Drawing.Bitmap surface;
        private Drawing.Graphics painter;
        private int pose = -1;
        internal ExcavatorLayer()
        {
            Text = "Snow1970 excavator"; FormBorderStyle = Forms.FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            AutoScaleMode = Forms.AutoScaleMode.None; StartPosition = Forms.FormStartPosition.Manual; Size = new Drawing.Size(90, 66);
            Native.BITMAPINFO info = new Native.BITMAPINFO { Size = 40, Width = Width, Height = -Height, Planes = 1, Bits = 32 };
            IntPtr bits; memoryDC = Native.CreateCompatibleDC(IntPtr.Zero); dib = Native.CreateDIBSection(IntPtr.Zero, ref info, 0, out bits, IntPtr.Zero, 0);
            if (dib == IntPtr.Zero) throw new InvalidOperationException("Cannot create excavator sprite");
            oldDib = Native.SelectObject(memoryDC, dib);
            surface = new Drawing.Bitmap(Width, Height, Width * 4, Drawing.Imaging.PixelFormat.Format32bppPArgb, bits); painter = Drawing.Graphics.FromImage(surface);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override Forms.CreateParams CreateParams { get { Forms.CreateParams p = base.CreateParams; p.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000; return p; } }
        protected override void WndProc(ref Forms.Message m) { if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; } if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } base.WndProc(ref m); }
        protected override void OnPaint(Forms.PaintEventArgs e) { }
        protected override void OnPaintBackground(Forms.PaintEventArgs e) { }
        internal void Present(World world, Forms.Form owner)
        {
            if (!world.DiggerVisible) { if (Visible) Hide(); return; }
            int x = (int)Math.Round(world.DiggerX - 45), y = (int)Math.Round(world.DiggerY - 55);
            if (Left != x || Top != y) Native.SetWindowPos(Handle, IntPtr.Zero, x, y, 0, 0, 0x15);
            bool outside = world.DiggerX + 45 <= world.Bounds.Left || world.DiggerX - 45 >= world.Bounds.Right;
            if (outside) { if (Visible) Hide(); return; }
            int edge = x < world.Bounds.Left || x + Width > world.Bounds.Right ? x : 0;
            int next = ((int)(world.DiggerLift * 18) + 32 * (int)((world.DiggerCurl + 1) * 16)) * 8 + (world.DiggerDirection < 0 ? 1 : 0) + (world.DiggerBucket > 2 ? 2 : 0) + edge * 100000;
            if (pose != next)
            {
                pose = next; painter.ResetClip(); painter.Clear(Drawing.Color.Transparent);
                painter.SetClip(new Drawing.RectangleF((float)(world.Bounds.Left - x), (float)(world.Bounds.Top - y), (float)world.Bounds.Width, (float)world.Bounds.Height));
                PaintRig(painter, world.DiggerDirection, world.DiggerLift, world.DiggerCurl, world.DiggerBucket > 2); Upload();
            }
            if (!Visible) Show(owner);
        }
        private void Upload()
        {
            painter.Flush(Drawing.Drawing2D.FlushIntention.Sync);
            Native.POINT destination = new Native.POINT { X = Left, Y = Top }, source = new Native.POINT();
            Native.SIZE size = new Native.SIZE { Width = Width, Height = Height }; Native.BLEND blend = new Native.BLEND { Alpha = 255, Format = 1 };
            Native.UpdateLayeredWindow(Handle, IntPtr.Zero, ref destination, ref size, memoryDC, ref source, 0, ref blend, 2);
        }
        internal static void PaintSprite(Drawing.Graphics g, int direction, int frame, bool loaded) { PaintRig(g, direction, .1 + frame / 12.0, .15 + frame / 14.0, loaded); }
        internal static void PaintRig(Drawing.Graphics g, int direction, double lift, double curl, bool loaded)
        {
            g.ResetTransform(); g.Clear(Drawing.Color.Transparent); g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TranslateTransform(4,0);
            if (direction < 0) { g.TranslateTransform(82,0); g.ScaleTransform(-1,1); }
            ExcavatorArt.Draw(g, lift, curl, loaded); g.ResetTransform();
        }
        internal void Export(string path) { PaintSprite(painter, 1, 0, true); surface.Save(path, Drawing.Imaging.ImageFormat.Png); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (painter != null) painter.Dispose(); if (surface != null) surface.Dispose(); if (memoryDC != IntPtr.Zero) { Native.SelectObject(memoryDC, oldDib); Native.DeleteObject(dib); Native.DeleteDC(memoryDC); memoryDC = IntPtr.Zero; } }
            base.Dispose(disposing);
        }
    }
}
