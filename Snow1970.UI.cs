using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Snow1970
{
    internal static class Art
    {
        internal static SolidColorBrush Brush(string hex) { SolidColorBrush b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }
        internal static readonly Brush Snow = Brush("#F2F8FF"), FarSnow = Brush("#BDCFDE"), Ice = Brush("#D7EBF7"), Panel = Brush("#191C21"), Line = Brush("#465262"), Text = Brush("#EAF0F6"), Muted = Brush("#AAB5C3"), Blue = Brush("#2D7DFF");
        internal static readonly Pen SnowPen = Pen(Snow, .85), FocusPen = Pen(Blue, 1.3), BorderPen = Pen(Line, 1);
        internal static readonly Drawing.Bitmap PowderTile = CreatePowderTile();
        private static Drawing.Bitmap CreatePowderTile()
        {
            Drawing.Bitmap tile = new Drawing.Bitmap(64, 64, Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Random random = new Random(1970);
            for (int i = 0; i < 430; i++)
            {
                int x = random.Next(64), y = random.Next(64), tone = random.Next(2) == 0 ? 175 : 255;
                tile.SetPixel(x, y, Drawing.Color.FromArgb(random.Next(7, 23), tone, tone, tone));
            }
            return tile;
        }
        internal static Pen Pen(Brush brush, double width) { Pen p = new Pen(brush, width); p.StartLineCap = PenLineCap.Round; p.EndLineCap = PenLineCap.Round; p.Freeze(); return p; }
        internal static void SnowMark(DrawingContext dc, Point center, double radius, Pen pen)
        {
            for (int i = 0; i < 3; i++)
            {
                double angle = -Math.PI * .5 + i * Math.PI / 3;
                Vector ray = new Vector(Math.Cos(angle), Math.Sin(angle));
                dc.DrawLine(pen, center - ray * radius, center + ray * radius);
            }
        }
        internal static void Snowflake(DrawingContext dc, Point c, double r, Pen pen)
        {
            for (int i = 0; i < 3; i++)
            {
                double a = i * Math.PI / 3;
                Vector v = new Vector(Math.Cos(a), Math.Sin(a));
                dc.DrawLine(pen, c - v * r, c + v * r);
                for (int side = -1; side <= 1; side += 2)
                {
                    Point tip = c + v * (r * .55 * side);
                    Vector normal = new Vector(-v.Y, v.X);
                    dc.DrawLine(pen, tip, tip - v * (r * .23 * side) + normal * (r * .22));
                    dc.DrawLine(pen, tip, tip - v * (r * .23 * side) - normal * (r * .22));
                }
            }
        }
    }

    internal sealed class SnowSwitch : ToggleButton
    {
        internal static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(double), typeof(SnowSwitch), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        internal SnowSwitch()
        {
            Width = 72; Height = 42; Cursor = Cursors.Hand; Background = Brushes.Transparent;
            OverridesDefaultStyle = true; FocusVisualStyle = null;
            Template = new ControlTemplate(typeof(SnowSwitch)) { VisualTree = new FrameworkElementFactory(typeof(Border)) };
            AutomationProperties.SetName(this, "Снег");
            ToolTip = "Снег · включить / выключить (Ctrl+Alt+S)";
            Checked += delegate { Animate(1); }; Unchecked += delegate { Animate(0); };
        }
        private void Animate(double value)
        {
            BeginAnimation(PositionProperty, new DoubleAnimation(value, TimeSpan.FromMilliseconds(140)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        }
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            double t = (double)GetValue(PositionProperty);
            bool on = IsChecked == true;
            dc.DrawRoundedRectangle(UiTheme.Get(this, on ? "OnTrack" : "OffTrack"), null, new Rect(5, 8, 56, 26), 13, 13);
            Point center = new Point(18 + t * 30, 21);
            dc.DrawEllipse(UiTheme.Get(this, on ? "OnKnob" : "OffKnob"), null, center, 10.5, 10.5);
            if (on) Art.SnowMark(dc, center, 5.25, Art.Pen(UiTheme.Get(this, "OnGlyph"), 1.25));
            if (IsKeyboardFocused) dc.DrawRoundedRectangle(null, Art.FocusPen, new Rect(1.5, 4.5, 63, 33), 16.5, 16.5);
        }
    }

    internal sealed class GearGlyph : FrameworkElement
    {
        internal GearGlyph() { Width = 18; Height = 18; IsHitTestVisible = false; }
        protected override void OnRender(DrawingContext dc)
        {
            Pen p = Art.Pen(UiTheme.Get(this, "Text"), 1.4);
            dc.DrawEllipse(null, p, new Point(9, 9), 5.1, 5.1);
            dc.DrawEllipse(null, p, new Point(9, 9), 1.8, 1.8);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                Vector v = new Vector(Math.Cos(a), Math.Sin(a));
                dc.DrawLine(p, new Point(9, 9) + v * 5.3, new Point(9, 9) + v * 7.1);
            }
        }
    }

    internal sealed class SnowCanvas : FrameworkElement
    {
        internal World World;
        internal SnowCanvas(World world) { World = world; IsHitTestVisible = false; }
        protected override void OnRender(DrawingContext dc)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix from = source == null ? Matrix.Identity : source.CompositionTarget.TransformFromDevice;
            dc.PushTransform(new MatrixTransform(from.M11, 0, 0, from.M22, -World.Bounds.X * from.M11, -World.Bounds.Y * from.M22));
            DrawWorld(dc, World); dc.Pop();
        }
        internal static void DrawWorld(DrawingContext dc, World world)
        {
            dc.PushClip(world.Sky);
            foreach (Pile p in world.Piles) if (p.Shape != null) dc.DrawGeometry(p.Ground ? Art.Ice : Art.Snow, null, p.Shape);
            for (int i = 0; i < world.Count; i++)
            {
                Flake f = world.Flakes[i];
                dc.DrawEllipse(f.Radius < 1.4 ? Art.FarSnow : Art.Snow, null, new Point(f.X, f.Y), f.Radius, f.Radius);
                if (f.Radius > 2.25)
                {
                    dc.DrawLine(Art.SnowPen, new Point(f.X - 3.2, f.Y), new Point(f.X + 3.2, f.Y));
                    dc.DrawLine(Art.SnowPen, new Point(f.X, f.Y - 3.2), new Point(f.X, f.Y + 3.2));
                }
            }
            dc.Pop();
        }
    }

    // Color-keyed native window: dirty rectangles, no full-screen alpha bitmap.
    internal sealed class SnowLayer : Forms.Form
    {
        internal World World;
        internal int PaintedFrames;
        internal GpuSnow Gpu;
        internal int RefreshHz;
        internal double Prediction;
        private bool hardware = true;
        private Drawing.Rectangle[] previous;
        private int previousCount;
        internal Dictionary<string, PileLayer> Caps = new Dictionary<string, PileLayer>();
        private Drawing.Brush erase = new Drawing.SolidBrush(Drawing.Color.FromArgb(1, 2, 3));
        private Drawing.Brush snow = new Drawing.SolidBrush(Drawing.Color.FromArgb(242, 248, 255));
        private Drawing.Brush far = new Drawing.SolidBrush(Drawing.Color.FromArgb(189, 207, 222));
        internal SnowLayer(World world)
        {
            World = world; Text = "Snow1970 layer";
            FormBorderStyle = Forms.FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            AutoScaleMode = Forms.AutoScaleMode.None;
            StartPosition = Forms.FormStartPosition.Manual;
            Bounds = new Drawing.Rectangle((int)world.Bounds.X, (int)world.Bounds.Y, (int)world.Bounds.Width, (int)world.Bounds.Height);
            SetStyle(Forms.ControlStyles.UserPaint | Forms.ControlStyles.AllPaintingInWmPaint | Forms.ControlStyles.Opaque, true);
            previous = new Drawing.Rectangle[world.Flakes.Length];
            Gpu = GpuSnow.TryCreate(Handle, Width, Height, world.Flakes.Length);
            if (Gpu != null) Native.SetLayeredWindowAttributes(Handle, 0, 255, 2);
            if (Gpu == null)
            {
                hardware = false; RecreateHandle();
                BackColor = Drawing.Color.FromArgb(1, 2, 3); TransparencyKey = BackColor;
            }
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override Forms.CreateParams CreateParams
        {
            get { Forms.CreateParams p = base.CreateParams; p.ExStyle |= (hardware ? 0x00200000 : 0) | 0x80000 | 0x20 | 0x80 | 0x08000000; return p; }
        }
        protected override void WndProc(ref Forms.Message m)
        {
            if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; }
            if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
            base.WndProc(ref m);
        }
        private Drawing.Rectangle Local(double x, double y, double w, double h)
        {
            return new Drawing.Rectangle((int)Math.Floor(x - World.Bounds.X), (int)Math.Floor(y - World.Bounds.Y), (int)Math.Ceiling(w), (int)Math.Ceiling(h));
        }
        private ExcavatorLayer excavator;
        private Dictionary<string, DecorationLayer> decorations = new Dictionary<string, DecorationLayer>();
        internal void DrawFrame()
        {
            if (!Visible || IsDisposed) return;
            SyncCaps();
            SyncDecorations();
            if (excavator == null && World.DiggerVisible) excavator = new ExcavatorLayer();
            if (excavator != null) excavator.Present(World, this);
            if (Gpu != null) { Gpu.Present(World, Prediction); PaintedFrames++; return; }
            for (int i = 0; i < previousCount; i++) Invalidate(previous[i]);
            for (int i = 0; i < World.Count; i++)
            {
                Flake f = World.Flakes[i]; Drawing.Rectangle r = Local(f.X - 5, f.Y - 5, 11, 11);
                previous[i] = r; Invalidate(r);
            }
            previousCount = World.Count;
        }
        internal void SyncCaps()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (Pile p in World.DisplayPiles())
            {
                if (p.NativeShapes.Count == 0) continue;
                seen.Add(p.Key); PileLayer cap;
                if (!Caps.TryGetValue(p.Key, out cap))
                {
                    cap = new PileLayer(World, p); Caps[p.Key] = cap; cap.Show(this);
                }
                cap.Pile = p; cap.Position();
                if (cap.Version != p.RenderVersion || cap.SceneVersion != World.SceneVersion)
                {
                    cap.Version = p.RenderVersion; cap.SceneVersion = World.SceneVersion; cap.RenderSurface();
                }
            }
            List<string> remove = new List<string>();
            foreach (string key in Caps.Keys) if (!seen.Contains(key)) remove.Add(key);
            foreach (string key in remove) { Caps[key].Close(); Caps[key].Dispose(); Caps.Remove(key); }
        }
        private void SyncDecorations()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (SnowDecoration model in World.Decorations)
            {
                seen.Add(model.Key); DecorationLayer layer;
                if (!decorations.TryGetValue(model.Key, out layer)) { layer = new DecorationLayer(); decorations[model.Key] = layer; }
                layer.Present(World, model, this);
            }
            List<string> removed = new List<string>(); foreach (string key in decorations.Keys) if (!seen.Contains(key)) removed.Add(key);
            foreach (string key in removed) { decorations[key].Close(); decorations[key].Dispose(); decorations.Remove(key); }
        }
        protected override void OnPaint(Forms.PaintEventArgs e)
        {
            if (Gpu != null || hardware) return;
            PaintedFrames++;
            Drawing.Graphics g = e.Graphics;
            g.FillRectangle(erase, e.ClipRectangle);
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.None;
            g.TranslateTransform((float)-World.Bounds.X, (float)-World.Bounds.Y);
            if (World.NativeSky != null) g.SetClip(World.NativeSky, Drawing.Drawing2D.CombineMode.Intersect);
            for (int i = 0; i < World.Count; i++)
            {
                Flake f = World.Flakes[i]; float r = (float)f.Radius;
                g.FillEllipse(r < 1.4 ? far : snow, (float)(f.X + f.VX * Prediction) - r, (float)(f.Y + f.VY * Prediction) - r, r * 2, r * 2);
            }
        }
        internal void Position()
        {
            Native.SetWindowPos(Handle, new IntPtr(-1), (int)World.Bounds.X, (int)World.Bounds.Y, (int)World.Bounds.Width, (int)World.Bounds.Height, 0x10 | 0x40);
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) { foreach (PileLayer cap in Caps.Values) cap.Hide(); if (excavator != null) excavator.Hide(); foreach (DecorationLayer toy in decorations.Values) toy.Hide(); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { foreach (DecorationLayer toy in decorations.Values) { toy.Close(); toy.Dispose(); } decorations.Clear(); if (excavator != null) { excavator.Close(); excavator.Dispose(); } if (Gpu != null) { Gpu.Dispose(); Gpu = null; } foreach (PileLayer cap in Caps.Values) { cap.Close(); cap.Dispose(); } Caps.Clear(); erase.Dispose(); snow.Dispose(); far.Dispose(); if (World.NativeSky != null) { World.NativeSky.Dispose(); World.NativeSky = null; } }
            base.Dispose(disposing);
        }
    }

    internal sealed class PileLayer : Forms.Form
    {
        internal Pile Pile;
        internal int Version = -1;
        internal int SceneVersion = -1;
        private World world;
        private IntPtr memoryDC, dib, oldDib;
        private Drawing.Bitmap surface;
        private Drawing.Graphics painter;
        internal PileLayer(World w, Pile p)
        {
            world = w; Pile = p; Text = "Snow1970 drift";
            FormBorderStyle = Forms.FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            AutoScaleMode = Forms.AutoScaleMode.None;
            StartPosition = Forms.FormStartPosition.Manual;
            SetStyle(Forms.ControlStyles.UserPaint | Forms.ControlStyles.AllPaintingInWmPaint | Forms.ControlStyles.Opaque, true);
            Bounds = FrameRect();
            RenderSurface();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override Forms.CreateParams CreateParams { get { Forms.CreateParams p = base.CreateParams; p.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000; return p; } }
        protected override void WndProc(ref Forms.Message m)
        {
            if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; }
            if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
            base.WndProc(ref m);
        }
        internal void Position()
        {
            if (IsDisposed) return;
            Drawing.Rectangle frame = FrameRect();
            if (Left != frame.X || Top != frame.Y || Width != frame.Width || Height != frame.Height) Native.SetWindowPos(Handle, IntPtr.Zero, frame.X, frame.Y, frame.Width, frame.Height, 0x14);
            if (!Visible) Show();
        }
        private Drawing.Rectangle FrameRect()
        {
            Rect box = Pile.VisualBounds();
            return new Drawing.Rectangle((int)Math.Floor(box.X) - 2, (int)Math.Floor(box.Y) - 2, Math.Max(1, (int)Math.Ceiling(box.Width) + 4), Math.Max(1, (int)Math.Ceiling(box.Height) + 4));
        }
        protected override void OnPaint(Forms.PaintEventArgs e)
        {
        }
        protected override void OnPaintBackground(Forms.PaintEventArgs e) { }
        private void FreeSurface()
        {
            if (painter != null) { painter.Dispose(); painter = null; }
            if (surface != null) { surface.Dispose(); surface = null; }
            if (memoryDC != IntPtr.Zero) { if (oldDib != IntPtr.Zero) Native.SelectObject(memoryDC, oldDib); if (dib != IntPtr.Zero) Native.DeleteObject(dib); Native.DeleteDC(memoryDC); }
            memoryDC = dib = oldDib = IntPtr.Zero;
        }
        internal void RenderSurface()
        {
            Drawing.Rectangle frame = FrameRect(); int width = frame.Width, height = frame.Height;
            if (surface == null || surface.Width != width || surface.Height != height)
            {
                FreeSurface(); Native.BITMAPINFO info = new Native.BITMAPINFO { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
                IntPtr bits; memoryDC = Native.CreateCompatibleDC(IntPtr.Zero);
                dib = Native.CreateDIBSection(IntPtr.Zero, ref info, 0, out bits, IntPtr.Zero, 0);
                if (memoryDC == IntPtr.Zero || dib == IntPtr.Zero) { FreeSurface(); return; }
                oldDib = Native.SelectObject(memoryDC, dib);
                surface = new Drawing.Bitmap(width, height, width * 4, Drawing.Imaging.PixelFormat.Format32bppPArgb, bits);
                painter = Drawing.Graphics.FromImage(surface);
            }
            painter.ResetTransform(); painter.ResetClip(); painter.Clear(Drawing.Color.Transparent);
            painter.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            painter.PixelOffsetMode = Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            painter.TranslateTransform(-Left, -Top);
            if (world.NativeSky != null && !Pile.Dynamic) painter.SetClip(world.NativeSky, Drawing.Drawing2D.CombineMode.Intersect);
            painter.TranslateTransform((float)(Pile.X - Pile.ShapeX), (float)(Pile.Y - Pile.ShapeY));
            if (Pile.SmoothPath != null && Pile.SmoothPath.PointCount > 0)
            {
                using (Drawing.Drawing2D.LinearGradientBrush snow = new Drawing.Drawing2D.LinearGradientBrush(new Drawing.RectangleF((float)Pile.ShapeX, (float)(Pile.ShapeY - Pile.MaxHeight - 1), (float)Math.Max(1, Pile.Width), (float)(Pile.MaxHeight + 3)), Drawing.Color.FromArgb(248, 250, 251), Drawing.Color.FromArgb(232, 236, 238), 90f)) painter.FillPath(snow, Pile.SmoothPath);
                using (Drawing.TextureBrush grain = new Drawing.TextureBrush(Art.PowderTile, Drawing.Drawing2D.WrapMode.Tile)) painter.FillPath(grain, Pile.SmoothPath);
            }
            painter.Flush(Drawing.Drawing2D.FlushIntention.Sync);
            Native.POINT destination = new Native.POINT { X = Left, Y = Top }, source = new Native.POINT();
            Native.SIZE size = new Native.SIZE { Width = width, Height = height };
            Native.BLEND blend = new Native.BLEND { Alpha = 255, Format = 1 };
            Native.UpdateLayeredWindow(Handle, IntPtr.Zero, ref destination, ref size, memoryDC, ref source, 0, ref blend, 2);
        }
        internal void ExportSurface(string path) { RenderSurface(); if (surface != null) surface.Save(path, Drawing.Imaging.ImageFormat.Png); }
        protected override void Dispose(bool disposing) { if (disposing) FreeSurface(); base.Dispose(disposing); }
    }

    internal sealed class Toolbar : Window
    {
        internal SnowSwitch Snow;
        internal System.Windows.Shapes.Ellipse Indicator;
        internal Action<bool> ToggleSnow;
        internal Action OpenSettings, ExitApp;
        internal Action<bool> ToggleExcavator;
        internal ToggleButton Tractor;
        internal Action DisplayFrame;
        internal FrameworkElement SettingsAnchor;
        internal IntPtr Handle;
        internal Toolbar()
        {
            UiTheme.Apply(this, true);
            Title = "Snow1970"; Width = 152; Height = 42;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            Topmost = false; ShowInTaskbar = Program.TestUI; ShowActivated = false;
            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal }; Content = row;
            Snow = new SnowSwitch(); row.Children.Add(Snow);
            Indicator = new System.Windows.Shapes.Ellipse { Width = 5, Height = 5, Fill = Art.Blue, Opacity = .4, Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Снег выключен" };
            row.Children.Add(Indicator);
            Button gear = new Button { Width = 28, Height = 34, VerticalAlignment = VerticalAlignment.Center, Content = new GearGlyph(), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, ToolTip = "Настройки снега" };
            gear.OverridesDefaultStyle = true;
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content); template.VisualTree = border; gear.Template = template;
            AutomationProperties.SetName(gear, "Настройки снега"); row.Children.Add(gear);
            Tractor = new ToggleButton { Width = 33, Height = 34, VerticalAlignment = VerticalAlignment.Center, Content = new ExcavatorGlyph(), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, ToolTip = "Экскаватор · разбирать большие сугробы", Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ToggleButton'><Border Background='Transparent'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border></ControlTemplate>") };
            row.Children.Remove(gear); row.Children.Add(Tractor); row.Children.Add(gear);
            AutomationProperties.SetName(Tractor, "Экскаватор");
            Tractor.Checked += delegate { Tractor.Opacity = 1; if (ToggleExcavator != null) ToggleExcavator(true); };
            Tractor.Unchecked += delegate { Tractor.Opacity = .5; if (ToggleExcavator != null) ToggleExcavator(false); };
            Tractor.Opacity = .5;
            SettingsAnchor = gear;
            gear.Click += delegate { if (OpenSettings != null) OpenSettings(); };
            Snow.Checked += delegate { if (ToggleSnow != null) ToggleSnow(true); };
            Snow.Unchecked += delegate { if (ToggleSnow != null) ToggleSnow(false); };
            ContextMenu menu = new ContextMenu();
            MenuItem settings = new MenuItem { Header = "Настройки" }; settings.Click += delegate { if (OpenSettings != null) OpenSettings(); };
            MenuItem anchor = new MenuItem { Header = "Вернуть в правый верхний угол" }; anchor.Click += delegate { Anchor(); };
            MenuItem exit = new MenuItem { Header = "Выход · Ctrl+Alt+Q" }; exit.Click += delegate { if (ExitApp != null) ExitApp(); };
            menu.Items.Add(settings); menu.Items.Add(anchor); menu.Items.Add(exit); ContextMenu = menu;
            UiTheme.Apply(menu, true); UiTheme.Bind(menu, Control.BackgroundProperty, "Panel"); UiTheme.Bind(menu, Control.ForegroundProperty, "Text"); UiTheme.Bind(menu, Control.BorderBrushProperty, "Line");
            foreach (MenuItem item in menu.Items) UiTheme.Bind(item, Control.ForegroundProperty, "Text");
            SourceInitialized += delegate
            {
                Handle = new WindowInteropHelper(this).Handle;
                int ex = Native.GetWindowLong(Handle, -20);
                Native.SetWindowLong(Handle, -20, Program.TestUI ? ex & ~0x80 : ex | 0x80);
                HwndSource.FromHwnd(Handle).AddHook(Hotkey);
                Native.RegisterHotKey(Handle, 1, 0x4003, 0x53); Native.RegisterHotKey(Handle, 2, 0x4003, 0x51);
            };
            L10n.BindTree(this); L10n.BindTree(menu);
            Loaded += delegate { Anchor(); RefreshTheme(true); };
            Closed += delegate { Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2); };
        }
        private IntPtr Hotkey(IntPtr hwnd, int message, IntPtr wp, IntPtr lp, ref bool handled)
        {
            if (message == DisplayClock.FrameMessage) { handled = true; if (DisplayFrame != null) DisplayFrame(); return IntPtr.Zero; }
            if (message == 0x312)
            {
                if (wp.ToInt32() == 1) Snow.IsChecked = Snow.IsChecked != true;
                if (wp.ToInt32() == 2 && ExitApp != null) ExitApp();
                handled = true;
            }
            return IntPtr.Zero;
        }
        private ThemeDecision theme = new ThemeDecision();
        internal void RefreshTheme(bool immediate)
        {
            if (Handle == IntPtr.Zero || !IsVisible) return;
            Native.RECT r; if (!Native.GetWindowRect(Handle, out r)) return;
            double value; if (!BackgroundTone.TryRead(r.Value, true, out value) || !theme.Observe(value, immediate)) return;
            ApplyTheme(theme.Dark);
        }
        internal void ApplyTheme(bool dark)
        {
            UiTheme.Apply(this, dark); if (ContextMenu != null) UiTheme.Apply(ContextMenu, dark);
            Snow.InvalidateVisual(); ((FrameworkElement)Tractor.Content).InvalidateVisual(); SettingsAnchor.InvalidateVisual();
            Button gear = SettingsAnchor as Button; if (gear != null && gear.Content is FrameworkElement) ((FrameworkElement)gear.Content).InvalidateVisual();
        }
        internal void Anchor()
        {
            if (Handle == IntPtr.Zero) return;
            Native.RECT r; Native.GetWindowRect(Handle, out r);
            Drawing.Rectangle work = Forms.Screen.PrimaryScreen.WorkingArea;
            double scale = 1; try { scale = Math.Max(1, Native.GetDpiForWindow(Handle) / 96.0); } catch (EntryPointNotFoundException) { }
            int x = Math.Max(work.Left + 4, work.Right - (r.Right - r.Left) - (int)Math.Round(18 * scale));
            int y = work.Top + (int)Math.Round(2 * scale);
            Native.SetWindowPos(Handle, IntPtr.Zero, x, y, 0, 0, 0x15);
            Raise();
        }
        internal void Raise()
        {
            if (Handle == IntPtr.Zero) return;
            // Stay immediately above Explorer's desktop, below ordinary app windows.
            IntPtr lastApp = IntPtr.Zero;
            Native.EnumWindows(delegate(IntPtr window, IntPtr data)
            {
                if (window == Handle || !Native.IsWindowVisible(window) || Native.IsIconic(window)) return true;
                StringBuilder cls = new StringBuilder(128); Native.GetClassName(window, cls, cls.Capacity);
                if (cls.ToString() == "Progman" || cls.ToString() == "WorkerW") return false;
                if ((Native.GetWindowLong(window, -20) & 8) == 0) lastApp = window;
                return true;
            }, IntPtr.Zero);
            if ((Native.GetWindowLong(Handle, -20) & 8) != 0) Native.SetWindowPos(Handle, new IntPtr(-2), 0, 0, 0, 0, 0x13);
            Native.SetWindowPos(Handle, lastApp, 0, 0, 0, 0, 0x13);
        }
        internal void Status(bool enabled, bool paused, string details)
        {
            Indicator.Opacity = !enabled ? .35 : paused ? .6 : 1;
            Indicator.ToolTip = L10n.T(!enabled ? "Снег выключен" : paused ? "Снег включён · рабочая область закрыта окнами" : "Снег идёт") + "\n" + details;
        }
    }

    internal sealed class SettingsPanel : UserControl
    {
        private static readonly ControlTemplate SliderStyle = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
            "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Slider'><Grid><Track x:Name='PART_Track' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{Binding Value, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' IsDirectionReversed='{TemplateBinding IsDirectionReversed}'>" +
            "<Track.DecreaseRepeatButton><RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Height='3' Background='{DynamicResource Ui.Text}' CornerRadius='1.5' VerticalAlignment='Center'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton>" +
            "<Track.IncreaseRepeatButton><RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Height='3' Background='{DynamicResource Ui.ControlLine}' CornerRadius='1.5' VerticalAlignment='Center'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>" +
            "<Track.Thumb><Thumb Width='13' Height='13' Cursor='Hand'><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Background='{DynamicResource Ui.Text}' CornerRadius='6.5'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb></Track></Grid></ControlTemplate>");
        private Settings config;
        private Action apply;
        private DispatcherTimer debounce;
        private Popup popup;
        internal event EventHandler Closed;
        internal bool IsOpen { get { return popup.IsOpen; } }
        internal SettingsPanel(Settings settings, Action onChange, Action clear, Action exit, Action languageChanged = null)
        {
            config = settings; apply = onChange; L10n.Use(config.Language); UiTheme.Apply(this, true);
            Width = 308; Foreground = Art.Text; FontFamily = new FontFamily("Segoe UI"); FontSize = 12;
            StackPanel stack = new StackPanel { Margin = new Thickness(17) };
            Content = new Border { Background = Art.Panel, BorderBrush = Art.Brush("#36404C"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = new ScrollViewer { Content = stack, MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 90), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
            Grid heading = new Grid { Margin = new Thickness(0,0,0,7) };
            heading.ColumnDefinitions.Add(new ColumnDefinition()); heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.Children.Add(new TextBlock { Text = "Настройки снега", FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            StackPanel languages = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            string[] codes = { "ru", "en" };
            foreach (string language in codes)
            {
                string code = language;
                RadioButton option = new RadioButton { Content = code.ToUpperInvariant(), GroupName = "InterfaceLanguage", IsChecked = config.Language == code, Width = 30, Height = 26, Margin = new Thickness(3,0,0,0), Cursor = Cursors.Hand, Background = Brushes.Transparent, BorderBrush = Art.Brush("#3C4754"), Foreground = Art.Text, Template = FlatTemplate(typeof(RadioButton), true), ToolTip = code == "en" ? "English" : "Русский" };
                AutomationProperties.SetName(option, code == "en" ? "English" : "Русский");
                option.Checked += delegate { config.Language = code; L10n.Use(code); Save(); if (languageChanged != null) languageChanged(); };
                L10n.BindChoice(option, code); languages.Children.Add(option);
            }
            Grid.SetColumn(languages,1); heading.Children.Add(languages); stack.Children.Add(heading);
            debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(230) };
            debounce.Tick += delegate { debounce.Stop(); apply(); Save(); };
            StackPanel interaction = Group(stack, "Взаимодействие", true);
            CheckBox cursor = Check("Сдвигать снег курсором", config.Interactive && config.CursorInteraction);
            cursor.Margin = new Thickness(0, 0, 0, 11);
            cursor.Click += delegate { config.CursorInteraction = cursor.IsChecked == true; config.Interactive = true; Changed(); }; interaction.Children.Add(cursor);
            interaction.Children.Add(new TextBlock { Text = "Проведи вдоль кучки, чтобы сдвинуть снег.", Foreground = Art.Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 13) });
            CheckBox gestures = Check("Жесты Shift и Ctrl", config.Interactive && config.GestureInteraction);
            gestures.Margin = new Thickness(0, 0, 0, 8);
            gestures.Click += delegate { config.GestureInteraction = gestures.IsChecked == true; config.Interactive = true; Changed(); }; interaction.Children.Add(gestures);
            interaction.Children.Add(new TextBlock { Text = "Ctrl — подбросить в стороны\nShift — слепить снежок; отпустить — бросить", Foreground = Art.Muted, FontSize = 10, LineHeight = 15 });
            StackPanel snow = Group(stack, "Снег и накопление", false);
            SliderRow(snow, "Интенсивность", config.Rate, 4, 48, " / с", delegate(double value) { config.Rate = value; Changed(); });
            SliderRow(snow, "Высота сугробов", config.GroundHeight, 6, 48, " px", delegate(double value)
            {
                config.GroundHeight = value; config.WindowHeight = Math.Max(3, Math.Min(20, value * .40625)); config.IconHeight = Math.Max(3, Math.Min(15, value * .3125)); Changed();
            });
            CheckBox icons = Check("Снег на значках", config.SnowOnIcons); icons.Margin = new Thickness(0, 0, 0, 12);
            icons.Click += delegate { config.SnowOnIcons = icons.IsChecked == true; Changed(); }; snow.Children.Add(icons);
            CheckBox extreme = Check("Экстремальный снегопад · ×2", config.Extreme);
            extreme.ToolTip = "Двойная скорость падения и двойной предел высоты сугробов";
            extreme.Click += delegate { config.Extreme = extreme.IsChecked == true; Changed(); }; snow.Children.Add(extreme);
            CheckBox winter = Check("Снеговички и елочки", config.Decorations); winter.Margin = new Thickness(0, 12, 0, 0);
            winter.ToolTip = "При заполнении 80% участка · до двух фигурок на пять шагов сетки значков";
            winter.Click += delegate { config.Decorations = winter.IsChecked == true; Changed(); }; snow.Children.Add(winter);
            StackPanel load = Group(stack, "Нагрузка и экран", false);
            UniformGrid qualities = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 15) };
            string[] keys = { "Auto", "Eco", "Smooth" }, names = { "Авто", "Экономно", "Плавно" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                RadioButton option = new RadioButton { Content = names[i], GroupName = "SnowQuality", Height = 30, Margin = new Thickness(0, 0, i == 2 ? 0 : 6, 0), IsChecked = config.Quality == key, Background = Brushes.Transparent, BorderBrush = Art.Brush("#3C4754"), Foreground = Art.Text, Cursor = Cursors.Hand, Template = FlatTemplate(typeof(RadioButton), true) };
                option.Checked += delegate { config.Quality = key; Changed(); }; qualities.Children.Add(option);
            }
            load.Children.Add(qualities);
            CheckBox sync = Check("Плавность по частоте экрана", config.SyncDisplay); sync.Margin = new Thickness(0, 0, 0, 12);
            sync.Click += delegate { config.SyncDisplay = sync.IsChecked == true; Changed(); }; load.Children.Add(sync);
            CheckBox monitors = Check("Снег на всех мониторах", config.AllMonitors); monitors.Margin = new Thickness(0, 0, 0, 12);
            monitors.Click += delegate { config.AllMonitors = monitors.IsChecked == true; Changed(); }; load.Children.Add(monitors);
            CheckBox enabled = Check("Включать сразу при запуске", config.StartEnabled);
            enabled.Click += delegate { config.StartEnabled = enabled.IsChecked == true; Changed(); }; load.Children.Add(enabled);
            stack.Children.Add(new Border { Height = 1, Background = Art.Brush("#36404C"), Margin = new Thickness(0, 0, 0, 13) });
            Grid buttons = new Grid(); buttons.ColumnDefinitions.Add(new ColumnDefinition()); buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            Button clearButton = Button("Убрать снег"); clearButton.Margin = new Thickness(0, 0, 8, 0); clearButton.Click += delegate { clear(); }; buttons.Children.Add(clearButton);
            Button quit = Button("Выход"); quit.Click += delegate { exit(); }; Grid.SetColumn(quit, 1); buttons.Children.Add(quit); stack.Children.Add(buttons);
            stack.Children.Add(new TextBlock { Text = "Ctrl+Alt+S — снег     Ctrl+Alt+Q — выход", Foreground = Art.Muted, FontSize = 10, Margin = new Thickness(0, 12, 0, 0) });
            popup = new Popup { Child = this, AllowsTransparency = true, StaysOpen = false, Placement = PlacementMode.Bottom, VerticalOffset = 7, HorizontalOffset = -280, PopupAnimation = PopupAnimation.None };
            UiTheme.BindTree(this); L10n.BindTree(this);
            popup.Closed += delegate { debounce.Stop(); apply(); Save(); if (Closed != null) Closed(this, EventArgs.Empty); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
        }
        private static StackPanel Group(StackPanel root, string title, bool expanded)
        {
            StackPanel body = new StackPanel { Margin = new Thickness(0, 3, 0, 16) };
            Expander group = new Expander { Header = new TextBlock { Text = title, Foreground = Art.Text, FontWeight = FontWeights.SemiBold }, IsExpanded = expanded, Content = body, Foreground = Art.Text, Margin = new Thickness(0, 6, 0, 7) };
            group.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Expander'><StackPanel><ToggleButton IsChecked='{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' Focusable='True' Cursor='Hand'><ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='Transparent' Padding='0,9,0,12'><DockPanel><TextBlock x:Name='arrow' DockPanel.Dock='Right' Text='›' Foreground='{DynamicResource Ui.Muted}' FontSize='17' RenderTransformOrigin='0.5,0.5'><TextBlock.RenderTransform><RotateTransform Angle='0'/></TextBlock.RenderTransform></TextBlock><ContentPresenter VerticalAlignment='Center'/></DockPanel></Border><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='arrow' Property='RenderTransform'><Setter.Value><RotateTransform Angle='90'/></Setter.Value></Setter></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template><ContentPresenter Content='{TemplateBinding Header}'/></ToggleButton><ContentPresenter x:Name='body' ContentSource='Content' Visibility='Collapsed'/><Border Height='1' Background='{DynamicResource Ui.Line}'/></StackPanel><ControlTemplate.Triggers><Trigger Property='IsExpanded' Value='True'><Setter TargetName='body' Property='Visibility' Value='Visible'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            root.Children.Add(group); return body;
        }
        private ThemeDecision theme = new ThemeDecision();
        internal void Show(FrameworkElement anchor)
        {
            Measure(new Size(Width, double.PositiveInfinity));
            Rect origin = BackgroundTone.ScreenRect(anchor); double scale = 1;
            PresentationSource source = PresentationSource.FromVisual(anchor); if (source != null) scale = source.CompositionTarget.TransformToDevice.M11;
            Rect area = new Rect(origin.Left - 280 * scale, origin.Bottom + 7 * scale, Width * scale, DesiredSize.Height * scale);
            double value; if (BackgroundTone.TryRead(area, false, out value)) { theme.Observe(value, true); UiTheme.Apply(this, theme.Dark); }
            popup.PlacementTarget = anchor; popup.IsOpen = true;
        }
        internal void RefreshTheme()
        {
            if (!IsOpen || !IsVisible || PresentationSource.FromVisual(this) == null) return;
            double value; if (BackgroundTone.TryRead(BackgroundTone.ScreenRect(this), true, out value) && theme.Observe(value, false)) UiTheme.Apply(this, theme.Dark);
        }
        internal void Close() { popup.IsOpen = false; }
        private static Button Button(string text)
        {
            return new Button { Content = text, Height = 31, Background = Brushes.Transparent, Foreground = Art.Text, BorderBrush = Art.Brush("#3C4754"), Cursor = Cursors.Hand, Template = FlatTemplate(typeof(Button), false) };
        }
        private static ControlTemplate FlatTemplate(Type type, bool radio)
        {
            ControlTemplate t = new ControlTemplate(type);
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border)); border.Name = "frame";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6)); border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetBinding(Border.BackgroundProperty, ParentBinding("Background")); border.SetBinding(Border.BorderBrushProperty, ParentBinding("BorderBrush"));
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter)); content.Name = "caption"; UiTheme.Bind(content, TextElement.ForegroundProperty, "Text"); content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content); t.VisualTree = border;
            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true }; hover.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("Ui.Muted"), "frame")); t.Triggers.Add(hover);
            Trigger focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true }; focus.Setters.Add(new Setter(Border.BorderBrushProperty, Art.Blue, "frame")); t.Triggers.Add(focus);
            if (radio)
            {
                Trigger selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                selected.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("Ui.Selection"), "frame")); selected.Setters.Add(new Setter(TextElement.ForegroundProperty, new DynamicResourceExtension("Ui.SelectionText"), "caption")); t.Triggers.Add(selected);
            }
            return t;
        }
        private static Binding ParentBinding(string property) { return new Binding(property) { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) }; }
        private static CheckBox Check(string text, bool value)
        {
            CheckBox box = new CheckBox { Content = text, IsChecked = value, Foreground = Art.Text, Cursor = Cursors.Hand };
            ControlTemplate t = new ControlTemplate(typeof(CheckBox)); FrameworkElementFactory dock = new FrameworkElementFactory(typeof(DockPanel));
            FrameworkElementFactory square = new FrameworkElementFactory(typeof(Border)); square.Name = "square";
            square.SetValue(Border.WidthProperty, 14.0); square.SetValue(Border.HeightProperty, 14.0); square.SetValue(Border.CornerRadiusProperty, new CornerRadius(4)); square.SetValue(Border.BorderThicknessProperty, new Thickness(1)); UiTheme.Bind(square, Border.BorderBrushProperty, "Muted");
            FrameworkElementFactory tick = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path)); tick.Name = "tick";
            tick.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M2,6 L5,9 L10,3")); UiTheme.Bind(tick, System.Windows.Shapes.Path.StrokeProperty, "SelectionText"); tick.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 1.6); tick.SetValue(UIElement.VisibilityProperty, Visibility.Hidden);
            square.AppendChild(tick); dock.AppendChild(square);
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter)); content.SetValue(FrameworkElement.MarginProperty, new Thickness(9, 0, 0, 0)); content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center); dock.AppendChild(content); t.VisualTree = dock;
            Trigger selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true }; selected.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("Ui.Selection"), "square")); selected.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "tick")); t.Triggers.Add(selected);
            box.Template = t; return box;
        }
        private void Save()
        {
            try { config.Save(Program.Folder); }
            catch (Exception e) { MessageBox.Show(L10n.T("Не удалось сохранить настройки: ") + e.Message, "Snow1970"); }
        }
        private void Changed() { debounce.Stop(); debounce.Start(); }
        private static void Label(StackPanel stack, string text) { stack.Children.Add(new TextBlock { Text = text, Margin = new Thickness(0, 0, 0, 6) }); }
        private static void SliderRow(StackPanel stack, string title, double value, double min, double max, string suffix, Action<double> change)
        {
            DockPanel label = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            TextBlock number = new TextBlock { Text = Math.Round(value) + suffix, Foreground = Art.Muted, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(number, Dock.Right); label.Children.Add(number); label.Children.Add(new TextBlock { Text = title }); stack.Children.Add(label);
            Slider slider = new Slider { Minimum = min, Maximum = max, Value = value, TickFrequency = 1, IsSnapToTickEnabled = true, Height = 23, Margin = new Thickness(0, 0, 0, 11) };
            slider.IsMoveToPointEnabled = true;
            slider.Template = SliderStyle;
            AutomationProperties.SetName(slider, title);
            L10n.BindNumber(number, slider, suffix);
            slider.ValueChanged += delegate { change(slider.Value); }; stack.Children.Add(slider);
        }
    }

    internal sealed class Controller : IDisposable
    {
        private sealed class SupportMotion { internal double X, Y, Time, VX; }
        private Dictionary<string, SupportMotion> supportMotion = new Dictionary<string, SupportMotion>();
        internal Toolbar Bar;
        internal List<SnowLayer> Layers = new List<SnowLayer>();
        internal Settings Config;
        internal bool Enabled;
        private SettingsPanel settingsWindow;
        private DispatcherTimer maintenance;
        private DisplayClock presentation;
        private Stopwatch clock = Stopwatch.StartNew();
        private double lastFrame, accumulator, lastSnapshot, lastBudget;
        private Point lastCursor;
        private bool cursorKnown, disposed, paused, allMonitors;
        private bool supportMoved;
        private double lastSupportMove, lastIconWake;
        private IntPtr moveHook;
        private IntPtr minimizeHook;
        private Dictionary<string, double> hiddenUntil = new Dictionary<string, double>();
        private Native.WinEventProc moveCallback;
        private Dictionary<IntPtr, string> trackedWindows = new Dictionary<IntPtr, string>();
        private int processId = Process.GetCurrentProcess().Id, fps, particleBudget;
        private double rate;
        private Process process = Process.GetCurrentProcess();
        private TimeSpan lastCpu;
        private string monitorSignature = "", sceneSignature = "", iconStatus = "Чтение значков…";
        private List<Body> iconBodies = new List<Body>();
        private object iconLock = new object();
        private Thread iconThread;
        private AutoResetEvent iconWake = new AutoResetEvent(false);
        private volatile bool workerEnabled, stopping;
        private Forms.NotifyIcon tray;
        internal Controller(Settings config)
        {
            Config = config; L10n.Use(Config.Language); SetQuality();
            ThreadPool.QueueUserWorkItem(delegate { try { DecorationArt.Warm(); ExcavatorArt.Warm(); } catch { } });
            Bar = new Toolbar(); Bar.ToggleSnow = Enable; Bar.OpenSettings = OpenSettings;
            Bar.Tractor.IsChecked = Config.Excavator;
            Bar.ToggleExcavator = delegate(bool enabled) { Config.Excavator = enabled; try { Config.Save(Program.Folder); } catch { } };
            Bar.ExitApp = delegate { Application.Current.Shutdown(); };
            Bar.Closed += delegate { Application.Current.Shutdown(); }; Bar.Show();
            presentation = new DisplayClock(Bar.Handle);
            Bar.DisplayFrame = delegate { presentation.Acknowledge(); Frame(null, EventArgs.Empty); };
            maintenance = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(550) };
            maintenance.Tick += Maintain; maintenance.Start(); RebuildMonitors();
            tray = new Forms.NotifyIcon { Icon = Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location), Text = "Snow1970 — снег", Visible = true };
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Снег: включить / выключить", null, delegate { Bar.Dispatcher.BeginInvoke(new Action(delegate { Bar.Snow.IsChecked = Bar.Snow.IsChecked != true; })); });
            menu.Items.Add("Настройки", null, delegate { Bar.Dispatcher.BeginInvoke(new Action(OpenSettings)); });
            menu.Items.Add("Вернуть панель", null, delegate { Bar.Dispatcher.BeginInvoke(new Action(Bar.Anchor)); });
            menu.Items.Add("Выход", null, delegate { Bar.Dispatcher.BeginInvoke(new Action(delegate { Application.Current.Shutdown(); })); });
            tray.ContextMenuStrip = menu; UpdateLanguage(); tray.DoubleClick += delegate { Bar.Dispatcher.BeginInvoke(new Action(OpenSettings)); };
            iconThread = new Thread(IconLoop) { IsBackground = true, Name = "Desktop icon geometry" }; iconThread.Start();
            lastCpu = process.TotalProcessorTime;
            moveCallback = SupportMoved;
            moveHook = Native.SetWinEventHook(0x8001, 0x800B, IntPtr.Zero, moveCallback, 0, 0, 2);
            minimizeHook = Native.SetWinEventHook(0x16, 0x17, IntPtr.Zero, moveCallback, 0, 0, 2);
            if (Config.StartEnabled) Bar.Snow.IsChecked = true; else UpdateStatus();
        }
        private void SupportMoved(IntPtr hook, uint ev, IntPtr window, int obj, int child, uint thread, uint time)
        {
            if (disposed || obj != 0 || child != 0) return;
            string key = "w" + window.ToInt64().ToString(CultureInfo.InvariantCulture);
            if (!trackedWindows.ContainsKey(window) && !hiddenUntil.ContainsKey(key)) return;
            try
            {
                if (ev == 0x8001 || ev == 0x8003 || ev == 0x16)
                {
                    hiddenUntil[key] = clock.Elapsed.TotalSeconds + .8;
                    RefreshScene(); supportMoved = true; return;
                }
                if (ev == 0x8002 || ev == 0x17)
                {
                    hiddenUntil.Remove(key); RefreshScene(); return;
                }
                if (ev != 0x800B) return;
                Native.RECT r; if (Native.DwmRect(window, 9, out r, 16) != 0 && !Native.GetWindowRect(window, out r)) return;
                double now = clock.Elapsed.TotalSeconds; SupportMotion motion;
                if (supportMotion.TryGetValue(key, out motion))
                {
                    double elapsed = now - motion.Time;
                    double vx = elapsed > .25 ? 0 : (r.Left - motion.X) / Math.Max(.006, elapsed);
                    if (elapsed < .18 && Math.Abs(vx) > 180 && Math.Abs(motion.VX) > 180 && Math.Sign(vx) != Math.Sign(motion.VX))
                    {
                        double strength = Settings.Clamp((Math.Abs(vx) + Math.Abs(motion.VX)) / 2200, .12, .5);
                        foreach (SnowLayer layer in Layers) layer.World.ShakeSupport(key, motion.VX, strength);
                    }
                    motion.X = r.Left; motion.Y = r.Top; motion.Time = now; motion.VX = vx;
                }
                else supportMotion[key] = new SupportMotion { X = r.Left, Y = r.Top, Time = now };
                foreach (SnowLayer layer in Layers)
                    foreach (Pile p in layer.World.Piles)
                        if (p.Key == key)
                        {
                            p.X = Math.Max(r.Left + 7, layer.World.Bounds.Left); p.Y = r.Top;
                            PileLayer cap; if (layer.Caps.TryGetValue(key, out cap)) cap.Position();
                        }
                supportMoved = true; lastSupportMove = clock.Elapsed.TotalSeconds;
            }
            catch { }
        }
        internal void WriteStatsLater(string path)
        {
            TimeSpan startCpu = process.TotalProcessorTime; Stopwatch elapsed = Stopwatch.StartNew();
            DispatcherTimer sample = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            sample.Tick += delegate
            {
                sample.Stop(); StringBuilder report = new StringBuilder();
                report.AppendLine("Enabled=" + Enabled + "; presentationHz=" + fps + "; physicsHz=" + Math.Max(120, Math.Min(600, fps * 2)));
                report.AppendLine("CPU (one core)=" + ((process.TotalProcessorTime - startCpu).TotalSeconds / elapsed.Elapsed.TotalSeconds * 100).ToString("F2", CultureInfo.InvariantCulture) + "%");
                report.AppendLine("Memory MB=" + (process.WorkingSet64 / 1048576.0).ToString("F1", CultureInfo.InvariantCulture));
                foreach (SnowLayer layer in Layers) report.AppendLine("Renderer=" + (layer.Gpu != null ? "DirectComposition GPU" : "GDI compatibility") + "; monitorHz=" + layer.RefreshHz + "; visible=" + layer.Visible + "; flakes=" + layer.World.Count + "; frames=" + layer.PaintedFrames + "; size=" + layer.Width + "x" + layer.Height);
                lock (iconLock) report.AppendLine(iconStatus);
                try { File.WriteAllText(path, report.ToString()); } catch { }
            };
            sample.Start();
        }
        private void Clear()
        {
            foreach (SnowLayer layer in Layers) layer.World.Clear(); Invalidate();
        }
        private void OpenSettings()
        {
            if (settingsWindow != null && settingsWindow.IsOpen) { settingsWindow.Close(); return; }
            settingsWindow = new SettingsPanel(Config, ApplySettings, Clear, delegate { Application.Current.Shutdown(); }, UpdateLanguage);
            settingsWindow.Closed += delegate { settingsWindow = null; }; settingsWindow.Show(Bar.SettingsAnchor);
        }
        private void ApplySettings()
        {
            if (disposed) return;
            SetQuality();
            iconWake.Set(); sceneSignature = ""; if (Enabled) RefreshScene();
            if (allMonitors != Config.AllMonitors) { RebuildMonitors(); if (Enabled) RefreshScene(); }
            foreach (SnowLayer layer in Layers)
            {
                foreach (Pile p in layer.World.Piles)
                {
                    p.MaxHeight = Config.HeightLimit(p.Ground ? Config.GroundHeight : p.Key.StartsWith("i", StringComparison.Ordinal) ? Math.Min(Config.IconHeight, p.Width * .28) : Config.WindowHeight);
                    p.BuildShape();
                }
            }
            Invalidate();
        }
        private string ScreenSignature()
        {
            string s = ""; foreach (Forms.Screen screen in Forms.Screen.AllScreens) s += screen.DeviceName + screen.Bounds.ToString() + screen.WorkingArea.ToString() + DisplayClock.RefreshRate(screen); return s;
        }
        private void RebuildMonitors()
        {
            foreach (SnowLayer layer in Layers) layer.Close(); Layers.Clear();
            Forms.Screen[] screens = Config.AllMonitors ? Forms.Screen.AllScreens : new Forms.Screen[] { Forms.Screen.PrimaryScreen };
            double width = 0; foreach (Forms.Screen screen in screens) width += screen.Bounds.Width;
            foreach (Forms.Screen screen in screens)
            {
                Drawing.Rectangle b = screen.Bounds, w = screen.WorkingArea;
                int max = Math.Max(16, (int)(Config.MaxFlakes * 2 * b.Width / width));
                World world = new World(new Rect(b.X, b.Y, b.Width, b.Height), new Rect(w.X, w.Y, w.Width, w.Height), Config, max, rate * b.Width / width, b.X + b.Y + 1970);
                world.VehicleAllowed = screen.Primary;
                world.DecorationIconPitch = Math.Max(48, Forms.SystemInformation.IconSpacingSize.Width);
                world.Limit = Math.Min(max, Math.Max(16, (int)(particleBudget * b.Width / width)));
                SnowLayer layer = new SnowLayer(world); layer.RefreshHz = DisplayClock.RefreshRate(screen); Layers.Add(layer);
                if (Enabled) { layer.Show(); world.SeedAir(); }
            }
            allMonitors = Config.AllMonitors; monitorSignature = ScreenSignature(); sceneSignature = ""; SetQuality(); Bar.Anchor(); Bar.Raise();
        }
        private void SetQuality()
        {
            bool weak = Environment.ProcessorCount <= 2 || (RenderCapability.Tier >> 16) == 0;
            double pixels = 0;
            foreach (Forms.Screen display in Forms.Screen.AllScreens)
                if (Config.AllMonitors || display.Primary) pixels += (double)display.Bounds.Width * display.Bounds.Height;
            bool largeDisplay = pixels > 2500000;
            int displayRate = 24;
            foreach (Forms.Screen display in Forms.Screen.AllScreens)
                if (Config.AllMonitors || display.Primary) displayRate = Math.Max(displayRate, DisplayClock.RefreshRate(display));
            fps = Config.SyncDisplay ? displayRate : Math.Min(displayRate, Math.Max(24, Config.Fps));
            if (Config.Quality == "Eco") { particleBudget = Math.Min(Config.MaxFlakes, 32); rate = Math.Min(Config.Rate, 5); }
            else if (Config.Quality == "Smooth") { particleBudget = Math.Min(Config.MaxFlakes, 180); rate = Config.Rate; }
            else { particleBudget = Math.Min(Config.MaxFlakes, weak || largeDisplay ? 65 : 90); rate = Math.Min(Config.Rate, weak || largeDisplay ? 7 : 10); }
            if (Config.Extreme) { particleBudget = Math.Min(360, particleBudget * 2); rate *= 2; }
            if (presentation != null) presentation.Configure(fps, Enabled);
            ShareBudget(); UpdateStatus();
        }
        private void ShareBudget()
        {
            double totalWidth = 0; foreach (SnowLayer l in Layers) totalWidth += l.World.Bounds.Width;
            foreach (SnowLayer l in Layers)
            {
                l.World.Limit = Math.Min(l.World.Flakes.Length, Math.Max(16, (int)(particleBudget * l.World.Bounds.Width / totalWidth)));
                l.World.Rate = rate * l.World.Bounds.Width / totalWidth; l.World.Count = Math.Min(l.World.Count, l.World.Limit);
            }
        }
        internal void Enable(bool enabled)
        {
            Enabled = enabled; workerEnabled = enabled; iconWake.Set();
            if (!enabled) { presentation.Configure(fps, false); foreach (SnowLayer layer in Layers) { layer.World.Clear(); layer.Hide(); } }
            else
            {
                sceneSignature = ""; RefreshScene();
                foreach (SnowLayer layer in Layers) { layer.Show(); layer.Position(); layer.World.SeedAir(); }
                Bar.Raise(); lastFrame = clock.Elapsed.TotalSeconds; accumulator = 0;
                presentation.Configure(fps, true);
            }
            UpdateStatus();
        }
        private void IconLoop()
        {
            while (!stopping)
            {
                if (!workerEnabled || !Config.SnowOnIcons) { iconWake.WaitOne(2000); continue; }
                try
                {
                    string status; List<Body> result = Native.DesktopIcons(out status);
                    lock (iconLock) { if (result != null) iconBodies = result; iconStatus = status; }
                }
                catch { lock (iconLock) { iconStatus = "Геометрия значков временно недоступна"; } }
                iconWake.WaitOne(1800);
            }
        }
        private void RefreshScene()
        {
            List<Body> scene = Native.Windows(processId);
            double now = clock.Elapsed.TotalSeconds;
            scene.RemoveAll(delegate(Body body) { double until; return hiddenUntil.TryGetValue(body.Key, out until) && now < until; });
            List<string> expired = new List<string>(); foreach (KeyValuePair<string, double> entry in hiddenUntil) if (now >= entry.Value) expired.Add(entry.Key);
            foreach (string key in expired) hiddenUntil.Remove(key);
            if (Config.SnowOnIcons)
                lock (iconLock)
                {
                    // A conservative portrait envelope prevents snow floating over
                    // transparent padding of narrow thumbnails such as 9:16 videos.
                    double smallest = double.MaxValue;
                    foreach (Body icon in iconBodies) smallest = Math.Min(smallest, Math.Min(icon.Rect.Width, icon.Rect.Height * 9.0 / 16));
                    foreach (Body icon in iconBodies)
                    {
                        Rect r = icon.Rect; double width = Math.Max(8, Math.Min(smallest, r.Width));
                        scene.Add(new Body(icon.Key, new Rect(r.X + (r.Width - width) * .5, r.Y, width, r.Height), true, true));
                    }
                }
            string signature = ""; foreach (Body b in scene) signature += b.Key + b.Rect.ToString(CultureInfo.InvariantCulture);
            if (signature == sceneSignature) return;
            sceneSignature = signature; foreach (SnowLayer layer in Layers) layer.World.Sync(scene);
            trackedWindows.Clear();
            foreach (Body body in scene)
                if (!body.Icon && body.Roof) trackedWindows[new IntPtr(long.Parse(body.Key.Substring(1), CultureInfo.InvariantCulture))] = body.Key;
        }
        private void Maintain(object sender, EventArgs args)
        {
            if (disposed) return;
            if (ScreenSignature() != monitorSignature) RebuildMonitors();
            Bar.Anchor(); Bar.RefreshTheme(false); if (settingsWindow != null) settingsWindow.RefreshTheme();
            if (!Enabled) return;
            RefreshScene(); lastSnapshot = clock.Elapsed.TotalSeconds;
            paused = false;
            foreach (SnowLayer layer in Layers) if (!layer.Visible) { layer.Show(); layer.Position(); Bar.Raise(); }
            Budget(); UpdateStatus();
        }
        private void Budget()
        {
            double now = clock.Elapsed.TotalSeconds, elapsed = now - lastBudget;
            if (elapsed < 4) return;
            TimeSpan cpu = process.TotalProcessorTime;
            double oneCore = (cpu - lastCpu).TotalSeconds / elapsed; lastCpu = cpu; lastBudget = now;
            if (Config.Quality == "Auto" && oneCore > .10 && particleBudget > 24)
            {
                particleBudget = Math.Max(24, particleBudget * 4 / 5); rate = Math.Max(4, rate * .85); ShareBudget();
            }
        }
        private void Frame(object sender, EventArgs args)
        {
            if (!Enabled || disposed) return;
            double now = clock.Elapsed.TotalSeconds;
            double dt = Math.Min(.12, Math.Max(0, now - lastFrame)); lastFrame = now;
            bool mouseDown = (Native.GetAsyncKeyState(1) & 0x8000) != 0;
            if (mouseDown && Config.SnowOnIcons && now - lastIconWake > .16) { iconWake.Set(); lastIconWake = now; }
            if (supportMoved || (mouseDown && now - lastSnapshot > .09)) { RefreshScene(); lastSnapshot = now; supportMoved = false; }
            Native.POINT pos; Native.GetCursorPos(out pos); Point cursor = new Point(pos.X, pos.Y);
            Vector speed = cursorKnown && dt > .001 ? (cursor - lastCursor) / dt : new Vector();
            lastCursor = cursor; cursorKnown = true;
            int gesture = (Native.GetAsyncKeyState(0x11) & 0x8000) != 0 ? 2 : (Native.GetAsyncKeyState(0x10) & 0x8000) != 0 ? 1 : 0;
            foreach (SnowLayer layer in Layers)
            {
                layer.World.Cursor = cursor; layer.World.CursorSpeed = speed; layer.World.Gesture = gesture;
                layer.World.SupportDrag = mouseDown && now - lastSupportMove < .2;
                if (Config.Excavator && layer.World.VehicleAllowed && !layer.World.DiggerVisible)
                {
                    Point origin = Bar.Tractor.PointToScreen(new Point(Bar.Tractor.ActualWidth * .5, Bar.Tractor.ActualHeight + 4));
                    layer.World.DiggerOrigin = new Point(origin.X, origin.Y + 55);
                }
            }
            double physicsStep = 1.0 / Math.Max(120, Math.Min(600, fps * 2));
            int maxSteps = 16;
            accumulator = Math.Min(accumulator + dt, maxSteps * physicsStep);
            int steps = 0;
            while (accumulator >= physicsStep && steps++ < maxSteps)
            {
                foreach (SnowLayer layer in Layers) layer.World.Step(physicsStep);
                accumulator -= physicsStep;
            }
            foreach (SnowLayer layer in Layers) layer.Prediction = Math.Min(physicsStep, accumulator);
            Invalidate();
        }
        private void Invalidate() { foreach (SnowLayer layer in Layers) layer.DrawFrame(); }
        private void UpdateLanguage()
        {
            if (tray != null)
            {
                tray.Text = L10n.T("Snow1970 — снег");
                string[] captions = { "Снег: включить / выключить", "Настройки", "Вернуть панель", "Выход" };
                for (int i = 0; i < captions.Length; i++) tray.ContextMenuStrip.Items[i].Text = L10n.T(captions[i]);
            }
            UpdateStatus();
        }
        private void UpdateStatus()
        {
            if (Bar != null) Bar.Status(Enabled, paused, L10n.T(Config.SnowOnIcons ? iconStatus : "Снег на значках выключен") + "\n" + L10n.Format("Частота экрана: {0} Гц · лимит {1} снежинок", fps, particleBudget));
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; stopping = true; workerEnabled = false; iconWake.Set();
            presentation.Dispose(); maintenance.Stop();
            if (moveHook != IntPtr.Zero) Native.UnhookWinEvent(moveHook);
            if (minimizeHook != IntPtr.Zero) Native.UnhookWinEvent(minimizeHook);
            foreach (SnowLayer l in Layers) l.Close();
            if (settingsWindow != null) settingsWindow.Close();
            if (tray != null) { tray.Visible = false; if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose(); if (tray.Icon != null) tray.Icon.Dispose(); tray.Dispose(); }
            process.Dispose();
        }
    }

    internal static class Program
    {
        internal static string Folder = AppDomain.CurrentDomain.BaseDirectory;
        internal static bool TestUI;
        private static void ClosePrevious()
        {
            string root = Path.GetFullPath(Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            int own = Process.GetCurrentProcess().Id;
            Native.EnumWindows(delegate(IntPtr window, IntPtr data)
            {
                StringBuilder title = new StringBuilder(128); Native.GetWindowText(window, title, title.Capacity);
                if (title.ToString() != "Snow1970") return true;
                uint id; Native.GetWindowThreadProcessId(window, out id);
                if (id == own) return true;
                StringBuilder cls = new StringBuilder(256); Native.GetClassName(window, cls, cls.Capacity);
                if (cls.ToString().IndexOf("HwndWrapper[Snow1970", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    IntPtr ignored; Native.SendMessageTimeout(window, 0x10, IntPtr.Zero, IntPtr.Zero, 2, 700, out ignored); return true;
                }
                try
                {
                    using (Process previous = Process.GetProcessById((int)id))
                    {
                        string path = previous.MainModule.FileName;
                        if (previous.ProcessName == "Snow1970" && Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                            Native.PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch { }
                return true;
            }, IntPtr.Zero);
        }
        private static FileStream AcquireLease(bool replace)
        {
            string path = Path.Combine(Path.GetTempPath(), "Snow1970." + Environment.UserName + ".run.lock");
            Stopwatch watch = Stopwatch.StartNew();
            while (true)
            {
                try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read); }
                catch (IOException) { if (!replace || watch.Elapsed.TotalSeconds > 5) return null; Thread.Sleep(80); }
            }
        }
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                TestUI = Array.IndexOf(args, "--test-ui") >= 0;
                int data = Array.IndexOf(args, "--data-dir");
                if (data >= 0 && data + 1 < args.Length && Directory.Exists(args[data + 1])) Folder = Path.GetFullPath(args[data + 1]);
                if (args.Length > 0 && args[0] == "--self-test") { Checks.Run(args.Length > 1 ? args[1] : Folder); return; }
                if (args.Length > 0 && args[0] == "--preview") { Checks.Preview(args.Length > 1 ? args[1] : Path.Combine(Folder, "Preview.png")); return; }
                if (args.Length > 0 && args[0] == "--renderer-check") { Checks.RendererProbe(args.Length > 1 ? args[1] : Folder); return; }
                if (args.Length > 0 && args[0] == "--appearance") { Checks.Appearance(args.Length > 1 ? args[1] : Folder); return; }
                L10n.Use(Settings.Read(Folder).Language);
                bool replace = Array.IndexOf(args, "--keep-existing") < 0;
                if (replace) ClosePrevious();
                using (FileStream lease = AcquireLease(replace))
                {
                if (lease == null) { MessageBox.Show(L10n.T("Snow1970 уже работает. Второй снежный слой не запущен."), "Snow1970"); return; }
                using (Mutex mutex = new Mutex(false, "Local\\Snow1970.Portable"))
                {
                    bool owns;
                    try { owns = mutex.WaitOne(replace ? 5000 : 0); }
                    catch (AbandonedMutexException) { owns = true; }
                    if (!owns) { MessageBox.Show(L10n.T("Snow1970 уже запущен. Закройте прежнюю версию через её меню или Ctrl+Alt+Q, затем откройте новую."), "Snow1970"); return; }
                    try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
                    Application app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    Controller controller = null;
                    app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                    {
                        Log(e.Exception); e.Handled = true;
                        MessageBox.Show(L10n.T("Snow1970 остановлен. Подробности: Snow1970.log рядом с программой."), "Snow1970"); app.Shutdown(1);
                    };
                    app.Exit += delegate { if (controller != null) controller.Dispose(); };
                    try
                    {
                        Settings settings = Settings.Read(Folder);
                        if (Array.IndexOf(args, "--optimize") >= 0)
                        {
                            settings.Quality = "Auto"; settings.Fps = Math.Min(20, settings.Fps);
                            settings.MaxFlakes = Math.Min(180, settings.MaxFlakes); settings.Rate = Math.Min(17, settings.Rate);
                        }
                        controller = new Controller(settings);
                        if (Array.IndexOf(args, "--snow") >= 0) controller.Bar.Snow.IsChecked = true;
                        int stats = Array.IndexOf(args, "--stats");
                        if (stats >= 0 && stats + 1 < args.Length) controller.WriteStatsLater(args[stats + 1]);
                        app.Run();
                    }
                    finally { mutex.ReleaseMutex(); }
                }
                }
            }
            catch (Exception e) { Log(e); MessageBox.Show(L10n.T("Не удалось запустить Snow1970: ") + e.Message, "Snow1970"); }
        }
        private static void Log(Exception e) { try { File.AppendAllText(Path.Combine(Folder, "Snow1970.log"), DateTime.Now.ToString("s") + " " + e + Environment.NewLine); } catch { } }
    }
}

