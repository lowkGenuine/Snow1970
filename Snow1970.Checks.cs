using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snow1970
{
    internal static class Checks
    {
        private sealed class ToneProbe : System.Windows.Forms.Form
        {
            internal ToneProbe(){FormBorderStyle=System.Windows.Forms.FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=System.Windows.Forms.FormStartPosition.Manual;}
            protected override bool ShowWithoutActivation{get{return true;}}
        }
        private sealed class ClockReceiver : System.Windows.Forms.NativeWindow, IDisposable
        {
            internal DisplayClock Clock;
            internal int Count;
            internal ClockReceiver() { CreateHandle(new System.Windows.Forms.CreateParams { Caption = "Snow clock check", Parent = new IntPtr(-3) }); }
            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == DisplayClock.FrameMessage) { Count++; if (Clock != null) Clock.Acknowledge(); return; }
                base.WndProc(ref m);
            }
            public void Dispose() { if (Clock != null) Clock.Dispose(); DestroyHandle(); }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint MsgWaitForMultipleObjectsEx(uint count, IntPtr handles, uint timeout, uint wakeMask, uint flags);
        internal static void RendererProbe(string folder)
        {
            Directory.CreateDirectory(folder); StringBuilder report = new StringBuilder();
            try
            {
                World world = new World(new Rect(-32000, -32000, 64, 64), new Rect(-32000, -32000, 64, 64), new Settings(), 32, 5, 4);
                using (SnowLayer layer = new SnowLayer(world))
                {
                    report.AppendLine("GPU=" + (layer.Gpu != null)); report.AppendLine("GPU status=" + (GpuSnow.LastError ?? "OK"));
                    uint windowProcess; Native.GetWindowThreadProcessId(layer.Handle, out windowProcess);
                    report.AppendLine("Window belongs to render process=" + (windowProcess == Process.GetCurrentProcess().Id));
                    foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
                        report.AppendLine("Display refresh=" + DisplayClock.RefreshRate(screen));
                    world.SeedAir();
                    if (layer.Gpu != null)
                        for (int i = 0; i < 120; i++) { world.Step(1.0 / 120); layer.Gpu.Present(world, .003); }
                    else
                        using (GpuSnow offscreen = GpuSnow.TryCreate(IntPtr.Zero, 64, 64, 32))
                        {
                            report.AppendLine("Offscreen compositor=" + (offscreen != null));
                            report.AppendLine("Offscreen status=" + (GpuSnow.LastError ?? "OK"));
                            if (offscreen != null) for (int i = 0; i < 120; i++) { world.Step(1.0 / 120); offscreen.Present(world, .003); }
                        }
                    System.Drawing.Rectangle screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                    layer.Location = new System.Drawing.Point(screenBounds.Right - 64, screenBounds.Top);
                    layer.Show(); System.Windows.Forms.Application.DoEvents();
                    Native.POINT hit = new Native.POINT { X = layer.Left + 12, Y = layer.Top + 12 };
                    bool clickThrough = Native.WindowFromPoint(hit) != layer.Handle;
                    report.AppendLine("Click-through=" + clickThrough);
                    if (!clickThrough) throw new Exception("Snow window intercepts desktop input");
                    World toys = new World(new Rect(screenBounds.Right - 128, screenBounds.Top, 128, 128), new Rect(screenBounds.Right - 128, screenBounds.Top, 128, 128), new Settings(), 32, 0, 1);
                    using (ExcavatorLayer vehicle = new ExcavatorLayer())
                    {
                        toys.DiggerVisible = true; toys.DiggerX = screenBounds.Right - 43; toys.DiggerY = screenBounds.Top + 57;
                        vehicle.Present(toys, layer); System.Windows.Forms.Application.DoEvents();
                        Native.POINT glass = new Native.POINT { X = vehicle.Left + 34, Y = vehicle.Top + 24 };
                        bool pass = Native.WindowFromPoint(glass) != vehicle.Handle; report.AppendLine("Excavator click-through=" + pass);
                        if (!pass) throw new Exception("Excavator intercepts input");
                    }
                    using (DecorationLayer decoration = new DecorationLayer())
                    {
                        SnowDecoration snowman = new SnowDecoration { X = screenBounds.Right - 90, Y = screenBounds.Top + 108, Opacity = 1 };
                        decoration.Present(toys, snowman, layer); System.Windows.Forms.Application.DoEvents();
                        decoration.ExportSurface(Path.Combine(folder, "native-snowman.png"));
                        Native.POINT body = new Native.POINT { X = decoration.Left + 24, Y = decoration.Top + 36 };
                        bool pass = Native.WindowFromPoint(body) != decoration.Handle; report.AppendLine("Winter toys click-through=" + pass);
                        if (!pass) throw new Exception("Winter toy intercepts input");
                    }
                    layer.Hide();
                    report.AppendLine("Presentation probe completed");
                }
                using(ToneProbe surface=new ToneProbe())
                {
                    System.Drawing.Rectangle workArea=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    surface.Bounds=new System.Drawing.Rectangle(workArea.Left+20,workArea.Top+90,140,120);surface.BackColor=System.Drawing.Color.White;surface.Show();surface.Refresh();System.Windows.Forms.Application.DoEvents();
                    Rect sample=new Rect(surface.Left+20,surface.Top+20,80,70);double white,black;
                    bool a=BackgroundTone.TryRead(sample,false,out white);surface.BackColor=System.Drawing.Color.Black;surface.Refresh();System.Windows.Forms.Application.DoEvents();bool b=BackgroundTone.TryRead(sample,false,out black);
                    report.AppendLine("Background sampling own white/black surfaces="+(a&&b&&white>.95&&black<.01));
                    if(!a||!b||white<.95||black>.01)throw new Exception("Native background tone sampling failed");
                }
                Toolbar panel = new Toolbar(); panel.Show(); panel.Anchor();
                Native.RECT before; Native.GetWindowRect(panel.Handle,out before);
                System.Drawing.Rectangle work = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                using(System.Windows.Forms.Form probe = new System.Windows.Forms.Form())
                {
                    probe.Text="Snow1970 own panel probe";probe.ShowInTaskbar=false;probe.FormBorderStyle=System.Windows.Forms.FormBorderStyle.None;probe.StartPosition=System.Windows.Forms.FormStartPosition.Manual;
                    probe.Bounds=new System.Drawing.Rectangle(before.Left,before.Top,180,60);probe.Show();panel.Anchor();System.Windows.Forms.Application.DoEvents();
                    Native.RECT after;Native.GetWindowRect(panel.Handle,out after);
                    Native.POINT point=new Native.POINT{X=before.Left+24,Y=before.Top+20};
                    bool stable=before.Left==after.Left&&before.Top==after.Top;
                    bool inside=after.Top>=work.Top&&after.Bottom<=work.Bottom&&after.Left>=work.Left&&after.Right<=work.Right;
                    bool under=Native.WindowFromPoint(point)!=panel.Handle&&(Native.GetWindowLong(panel.Handle,-20)&8)==0;
                    report.AppendLine("Desktop panel stable="+stable+"; inside screen="+inside+"; below apps="+under);
                    if(!stable||!inside||!under)throw new Exception("Desktop panel anchoring failed");
                }
                panel.Close();
                using (ClockReceiver receiver = new ClockReceiver())
                {
                    int hz = DisplayClock.RefreshRate(System.Windows.Forms.Screen.PrimaryScreen);
                    receiver.Clock = new DisplayClock(receiver.Handle); receiver.Clock.Configure(hz, true);
                    Stopwatch pacing = Stopwatch.StartNew();
                    while (pacing.Elapsed.TotalSeconds < 1.2) { MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 50, 0x04FF, 4); System.Windows.Forms.Application.DoEvents(); }
                    double actual = receiver.Count / pacing.Elapsed.TotalSeconds;
                    report.AppendLine("Clock requested=" + hz + "; delivered=" + actual.ToString("F1", CultureInfo.InvariantCulture) + " Hz");
                    if (actual < hz * .7 || actual > hz * 1.2) throw new Exception("Display pacing failed");
                }
            }
            catch (Exception e) { report.AppendLine(e.ToString()); Environment.ExitCode = 1; }
            File.WriteAllText(Path.Combine(folder, "renderer-probe.txt"), report.ToString());
        }
        internal static void Appearance(string folder)
        {
            Directory.CreateDirectory(folder);
            World world = new World(new Rect(0, 0, 660, 180), new Rect(0, 0, 660, 150), new Settings(), 32, 0, 2);
            Pile mound = world.Piles[0];
            for (int i = 0; i < mound.Height.Length; i++)
            {
                double x = (i + .5) * mound.Cell;
                mound.Height[i] = 25 * Math.Pow(Math.Max(0, 1 - Math.Pow((x - 145) / 76, 2)), .72) + 17 * Math.Pow(Math.Max(0, 1 - Math.Pow((x - 365) / 94, 2)), .85);
            }
            using (ExcavatorLayer sprite = new ExcavatorLayer()) sprite.Export(Path.Combine(folder, "excavator.png"));
            SaveControl(new SettingsPanel(new Settings(), delegate { }, delegate { }, delegate { }), 308, Path.Combine(folder, "settings.png"));
            Toolbar bar = new Toolbar(); SaveControl((FrameworkElement)bar.Content, 152, Path.Combine(folder, "toolbar.png")); bar.Close();
            mound.BuildShape(); string moundPath = Path.Combine(folder, "native-mounds.png"), ballPath = Path.Combine(folder, "native-ball.png");
            using (PileLayer layer = new PileLayer(world, mound)) layer.ExportSurface(moundPath);
            Pile ball = new Pile("preview-ball", 0, 100, 72, 66, false) { Snowball = true, BallRadius = 18, BallMass = Math.PI * 18 * 18 };
            ball.BuildShape(); using (PileLayer layer = new PileLayer(world, ball)) layer.ExportSurface(ballPath);
            using (System.Drawing.Bitmap sheet = new System.Drawing.Bitmap(720, 210))
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(sheet))
            using (System.Drawing.Font title = new System.Drawing.Font("Segoe UI", 12))
            using (System.Drawing.Image heaps = System.Drawing.Image.FromFile(moundPath))
            using (System.Drawing.Image sphere = System.Drawing.Image.FromFile(ballPath))
            {
                graphics.Clear(System.Drawing.Color.FromArgb(30, 35, 41));
                graphics.DrawString("Матовый рыхлый снег", title, System.Drawing.Brushes.WhiteSmoke, 24, 18);
                graphics.DrawImageUnscaled(heaps, 24, 56);
                graphics.DrawString("Shift — снежок; отпустить — бросок", title, System.Drawing.Brushes.WhiteSmoke, 24, 117);
                graphics.DrawImageUnscaled(sphere, 560, 110);
                graphics.DrawImageUnscaled(DecorationArt.Sprite(false), 600, 135);
                graphics.DrawImageUnscaled(DecorationArt.Sprite(true), 654, 135);
                using (System.Drawing.Image machine = System.Drawing.Image.FromFile(Path.Combine(folder, "excavator.png"))) graphics.DrawImageUnscaled(machine, 452, 136);
                sheet.Save(Path.Combine(folder, "appearance.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        private static void SaveControl(FrameworkElement element, double width, string path)
        {
            element.Measure(new Size(width, double.PositiveInfinity)); element.Arrange(new Rect(0, 0, width, element.DesiredSize.Height)); element.UpdateLayout();
            RenderTargetBitmap pixels = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32); pixels.Render(element);
            PngBitmapEncoder png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(pixels)); using (FileStream stream = File.Create(path)) png.Save(stream);
        }
        private static int passed;
        private static void Assert(bool condition, string message, StringBuilder report)
        {
            if (!condition) throw new InvalidOperationException("FAILED: " + message);
            passed++; report.AppendLine("PASS " + message);
        }
        private static World EmptyWorld(int seed)
        {
            Settings settings = new Settings();
            return new World(new Rect(0, 0, 960, 640), new Rect(0, 0, 960, 610), settings, 180, 18, seed);
        }
        internal static void Run(string folder)
        {
            Directory.CreateDirectory(folder);
            StringBuilder report = new StringBuilder();
            report.AppendLine("Snow1970 — deterministic engine and Windows integration checks");
            report.AppendLine(DateTime.Now.ToString("s"));
            try
            {
                passed = 0;
                World w = EmptyWorld(1970);
                w.Rate = 0;
                List<Body> bodies = new List<Body>();
                bodies.Add(new Body("browser", new Rect(280, 240, 420, 230), true, false));
                bodies.Add(new Body("icon", new Rect(80, 140, 48, 48), true, true));
                w.Sync(bodies);
                Assert(w.Piles.Count == 3, "Creates a floor, a window roof and an icon roof", report);
                w.Add(104, 131, 0, 70, 2);
                for (int i = 0; i < 10; i++) w.Step(1.0 / 45);
                Assert(w.Piles.Find(delegate(Pile p) { return p.Key == "icon"; }).Mass() > 15, "Swept particle lands on the icon graphic", report);
                w.Clear(); w.Add(460, 231, 0, 80, 2);
                for (int i = 0; i < 10; i++) w.Step(1.0 / 45);
                Assert(w.Piles.Find(delegate(Pile p) { return p.Key == "browser"; }).Mass() > 15, "Snow accumulates on the browser title-bar edge", report);
                w.Clear(); w.Add(200, 580, 0, 100, 2);
                for (int i = 0; i < 25; i++) w.Step(1.0 / 45);
                Assert(w.Piles[0].Mass() > 15, "Snow in the gap between colliders reaches the floor", report);
                w.Clear(); w.Add(276, 300, 160, 25, 2); w.Step(1.0 / 45);
                Assert(w.Count == 1 && w.Flakes[0].VX < 0 && w.Flakes[0].X < 280, "A particle bounces off the side of a window", report);
                Pile roof = w.Piles.Find(delegate(Pile p) { return p.Key == "browser"; });
                roof.Deposit(490, 450);
                w.Sync(new List<Body>());
                Assert(w.Piles.Count == 1 && w.Falling.Count > 0, "Minimizing/closing a window detaches intact snow from its frame", report);
                double startY = w.Falling[0].Y;
                for (int i = 0; i < 18; i++) w.Step(1.0 / 90);
                Assert(w.Falling.Count > 0 && w.Falling[0].Y > startY && w.Falling[0].FallAge < .3, "Detached mounds fall as cohesive pieces before breaking into flakes", report);
                for (int i = 0; i < 110; i++) w.Step(1.0 / 90);
                Assert(w.Falling.Count == 0 && w.Count > 0, "Falling pieces progressively fragment into snowflakes", report);
                w = EmptyWorld(3); w.Rate = 0;
                Pile floor = w.Piles[0];
                for (int i = 0; i < floor.Height.Length; i++) floor.Height[i] = 14;
                double before = floor.Mass();
                for (int i = 0; i < 120; i++) w.Step(1.0 / 45);
                Assert(floor.Mass() < before, "Natural sublimation prevents unbounded accumulation", report);
                w = EmptyWorld(4);
                w.Sync(new List<Body> { new Body("front", new Rect(250, 190, 500, 260), true, false), new Body("back", new Rect(100, 240, 500, 260), true, false) });
                Assert(!w.Sky.FillContains(new Point(400, 300)), "Overlapping app windows do not create clipping holes", report);
                Pile back = w.Piles.Find(delegate(Pile p) { return p.Key == "back"; });
                Assert(!back.Exposed[(int)((400 - back.X) / back.Cell)] && back.Exposed[0], "Only the exposed section of a rear window catches snow", report);
                w = EmptyWorld(12);
                Body maximized = new Body("maximized", new Rect(0, 0, 960, 610), false, false); maximized.Solid = false;
                w.Sync(new List<Body> { maximized, new Body("hidden-window", new Rect(220, 240, 500, 220), true, false), new Body("hidden-icon", new Rect(80, 140, 48, 48), true, true) });
                Assert(w.Sky.FillContains(new Point(400, 300)), "Maximized browser preserves the visible snowfall layer", report);
                Assert(!w.Piles.Find(delegate(Pile p) { return p.Key == "hidden-window"; }).Exposed[10], "Windows behind a maximized browser do not catch visible snow", report);
                w.Rate = 0; w.Add(460, 20, 0, 50, 2);
                for (int i = 0; i < 45; i++) w.Step(1.0 / 45);
                Assert(w.Count == 1 && w.Flakes[0].Y > 65, "Snow keeps falling in front of a maximized browser", report);
                w.Clear(); w.Add(460, 595, 0, 80, 2);
                for (int i = 0; i < 20; i++) w.Step(1.0 / 45);
                Assert(w.Piles[0].Mass() > 10, "Snow can accumulate at the screen bottom over a maximized app", report);
                w = EmptyWorld(7); w.Cursor = new Point(-9999, -9999);
                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < 45 * 600; i++) w.Step(1.0 / 45);
                watch.Stop();
                Assert(w.Count <= w.Limit, "Ten simulated minutes keep the particle pool bounded", report);
                double max = 0;
                foreach (Pile p in w.Piles) foreach (double h in p.Height)
                {
                    if (double.IsNaN(h) || double.IsInfinity(h) || h < 0) throw new Exception("Invalid terrain height");
                    max = Math.Max(max, h);
                }
                Assert(max <= w.Config.GroundHeight + .001, "Long-running snowfall stays below the configured drift limit", report);
                Assert(w.Piles[0].Mass() > 1000, "Sustained snowfall produces visible ground drifts", report);
                report.AppendLine("600 simulated seconds: " + watch.ElapsedMilliseconds + " ms; active flakes=" + w.Count + "; highest drift=" + max.ToString("F2", CultureInfo.InvariantCulture) + " px.");
                // Actual Windows integration: read geometry without changing the desktop.
                string status; List<Body> icons = Native.DesktopIcons(out status);
                report.AppendLine("Desktop: " + status + "; returned=" + (icons == null ? "deferred" : icons.Count.ToString()));
                Assert(icons != null, "Explorer geometry query completes within its time budget", report);
                List<Body> windows = Native.Windows(Process.GetCurrentProcess().Id);
                Assert(windows.Count > 0, "Native window discovery returns visible desktop obstacles", report);
                report.AppendLine("Visible window obstacles=" + windows.Count);
                foreach (Body obstacle in windows)
                {
                    StringBuilder cls = new StringBuilder(256);
                    Native.GetClassName(new IntPtr(long.Parse(obstacle.Key.Substring(1), CultureInfo.InvariantCulture)), cls, cls.Capacity);
                    report.AppendLine("Obstacle: " + cls + "; " + obstacle.Rect.ToString(CultureInfo.InvariantCulture) + "; solid=" + obstacle.Solid + "; roof=" + obstacle.Roof);
                }
                foreach (Body icon in icons)
                    if (icon.Rect.IsEmpty || icon.Rect.Width < 8 || icon.Rect.Height < 8) throw new Exception("Invalid icon rectangle");
                w.Clear();
                Assert(w.Count == 0 && w.Piles[0].Mass() == 0, "Off/clear releases all visible snow", report);
                Toolbar toolbar = new Toolbar();
                FrameworkElement control = (FrameworkElement)toolbar.Content;
                control.Measure(new Size(152, 42)); control.Arrange(new Rect(0, 0, 152, 42)); control.UpdateLayout();
                Assert(toolbar.Width == 152 && ((SolidColorBrush)toolbar.Background).Color.A == 0, "Compact toolbar has no background plate", report);
                Assert(((SolidColorBrush)toolbar.Indicator.Fill).Color == (Color)ColorConverter.ConvertFromString("#2D7DFF"), "Status indicator uses the requested saturated blue", report);
                toolbar.Close();
                Settings saved = new Settings { Quality = "Eco", Rate = 19, GroundHeight = 25, AllMonitors = false, CursorInteraction = false, Excavator = true };
                string settingsFolder = Path.Combine(folder, "settings-check"); Directory.CreateDirectory(settingsFolder); saved.Save(settingsFolder);
                Settings loaded = Settings.Read(settingsFolder);
                Assert(loaded.Quality == "Eco" && loaded.Rate == 19 && loaded.GroundHeight == 25 && !loaded.AllMonitors && !loaded.CursorInteraction && loaded.GestureInteraction && loaded.Excavator, "Settings persist and reload correctly", report);
                SettingsPanel popover = new SettingsPanel(new Settings(), delegate { }, delegate { }, delegate { });
                popover.Measure(new Size(308, double.PositiveInfinity)); popover.Arrange(new Rect(new Point(), popover.DesiredSize)); popover.UpdateLayout();
                Assert(popover.ActualHeight > 250 && popover.ActualHeight < 700, "Spartan settings and slider templates render as a compact popover", report);
                World play = EmptyWorld(23); play.Rate = 0;
                Pile playFloor = play.Piles[0];
                for (int i = 0; i < playFloor.Height.Length; i++) { double x = playFloor.X + (i + .5) * playFloor.Cell; playFloor.Height[i] = 10 * Math.Max(0, 1 - Math.Pow((x - 460) / 45, 2)); }
                play.Cursor = new Point(440, playFloor.Y - 3); play.CursorSpeed = new Vector(240, 0); play.Gesture = 0;
                double original = playFloor.Mass();
                double initialCenter = CenterOfMass(playFloor);
                for (int i = 0; i < 24; i++) { play.Cursor = play.Cursor + new Vector(2, 0); play.Step(1.0 / 120); }
                double center = CenterOfMass(playFloor);
                report.AppendLine("Cursor displacement=" + (center - initialCenter) + "; mass delta=" + (playFloor.Mass() - original));
                Assert(play.Sliding == null && center > initialCenter + 8 && center < initialCenter + 45 && Math.Abs(playFloor.Mass() - original) < original * .01, "A cursor sweep visibly shifts a bounded portion of snow with conserved mass", report);
                play.Config.CursorInteraction = false; double[] stationary = (double[])playFloor.Height.Clone();
                play.Time += .01; play.Interact(.01);
                bool intact = true; for (int i = 0; i < stationary.Length; i++) if (Math.Abs(stationary[i] - playFloor.Height[i]) > .00001) intact = false;
                Assert(intact, "Disabling the cursor leaves snow untouched by pointer motion", report);
                play.CursorSpeed = new Vector();
                for (int i = 0; i < 120; i++) play.Step(1.0 / 120);
                double restingCenter = CenterOfMass(playFloor);
                for (int i = 0; i < 120; i++) play.Step(1.0 / 120);
                Assert(Math.Abs(CenterOfMass(playFloor) - restingCenter) < .01 && !play.MovingSnow, "A resting drift stays still rather than oscillating like water", report);
                for (int i = 0; i < playFloor.Height.Length; i++) { playFloor.Height[i] = 6; playFloor.Flow[i] = 0; }
                play.Cursor = new Point(480, playFloor.Y - 3);
                play.Gesture = 1; play.CursorSpeed = new Vector();
                original = playFloor.Mass();
                for (int i = 0; i < 30; i++) { play.Time += 1.0 / 120; play.Interact(1.0 / 120); play.Advect(1.0 / 120); }
                Assert(play.HeldBall != null && play.HeldBall.Snowball && play.HeldBall.BallRadius > 3 && Math.Abs(playFloor.Mass() + play.HeldBall.Mass() - original) < .01, "Shift packs existing snow into a round held snowball with conserved mass", report);
                play.Gesture = 2; play.Interact(.01);
                Assert(play.HeldBall == null && play.Falling.Count > 0 && play.Falling[0].Snowball && play.Falling[0].FallVY < 0, "Ctrl launches a held snowball intact", report);
                World loft = EmptyWorld(99); loft.Rate = 0; Pile loose = loft.Piles[0];
                for (int i = 0; i < loose.Height.Length; i++) loose.Height[i] = 8;
                loft.Cursor = new Point(480, loose.Y - 2); loft.Gesture = 2; loft.Interact(.01);
                Assert(loft.Falling.Count > 0 && loft.Falling[0].CohesiveLaunch && loft.Falling[0].FallVY < -200 && loft.Count == 0, "Ctrl lofts a whole mound instead of immediately dissolving it into particles", report);
                Assert(loft.Falling.Count == 2 && loft.Falling[0].FallVX < 0 && loft.Falling[1].FallVX > 0, "Ctrl launches two cohesive portions sideways rather than a central fountain", report);
                World moving = EmptyWorld(71);
                moving.Sync(new List<Body> { new Body("browser", new Rect(200, 240, 400, 200), true, false) });
                Pile carried = moving.Piles.Find(delegate(Pile p) { return p.Key == "browser"; }); carried.Deposit(400, 200); double carriedMass = carried.Mass();
                moving.Sync(new List<Body> { new Body("browser", new Rect(270, 295, 400, 200), true, false) });
                Assert(carried.X == 277 && carried.Y == 295 && Math.Abs(carried.Mass() - carriedMass) < .01 && moving.Count == 0, "Window snow follows its inset rim without a horizontal snap", report);
                for (int i = 0; i < carried.Height.Length; i++) carried.Height[i] = 8;
                moving.ShakeSupport("browser", 700, .4);
                Assert(moving.Falling.Count > 0 && moving.Falling[0].FallVX > 0, "Reversing a shaken window sheds cohesive snow with inertia", report);
                World edge = EmptyWorld(61); edge.Rate = 0;
                edge.Sync(new List<Body> { new Body("i1", new Rect(100, 150, 18, 36), true, true) });
                Pile narrow = edge.Piles.Find(delegate(Pile p) { return p.Key == "i1"; });
                narrow.RightSpill = 30; edge.Settle(.1);
                Assert(edge.Count > 0 && edge.Flakes[0].Ballistic && edge.Flakes[0].VX > 0 && edge.Flakes[0].VY < 0, "Crossing an icon rim launches snow upward into a ballistic arc", report);
                World pushEdge = EmptyWorld(15); pushEdge.Rate = 0;
                pushEdge.Sync(new List<Body> { new Body("narrow", new Rect(100, 150, 18, 36), true, true) });
                Pile tiny = pushEdge.Piles.Find(delegate(Pile p) { return p.Key == "narrow"; });
                for (int i = 0; i < tiny.Height.Length; i++) { tiny.Height[i] = 7; tiny.Flow[i] = 260; } tiny.ContactUntil = 100;
                for (int i = 0; i < 12; i++) pushEdge.Advect(1.0 / 120);
                pushEdge.Settle(.1);
                Assert(pushEdge.Count > 0 && pushEdge.Flakes[0].Ballistic && tiny.Mass() < 18 * 7, "Actually pushing a narrow cap past its width produces an arc, not an invisible clipped wedge", report);
                Settings stormSettings = new Settings { Extreme = true };
                World storm = new World(new Rect(0, 0, 960, 640), new Rect(0, 0, 960, 610), stormSettings, 180, 0, 4);
                Assert(storm.Piles[0].MaxHeight == stormSettings.GroundHeight * 2, "Extreme mode doubles the drift height ceiling", report);
                Assert(DisplayClock.RefreshRate(System.Windows.Forms.Screen.PrimaryScreen) >= 24, "Presentation cadence reads the actual display mode", report);
                CollisionChecks(report, folder);
                WinterChecks(report);
                RefinementChecks(report);
                After23Checks(report, folder);
                ThemeChecks(report, folder);
                report.AppendLine("ALL " + passed + " CHECKS PASSED");
                File.WriteAllText(Path.Combine(folder, "test-results.txt"), report.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                report.AppendLine(e.ToString()); File.WriteAllText(Path.Combine(folder, "test-results.txt"), report.ToString(), Encoding.UTF8);
                Environment.ExitCode = 1;
            }
        }
        private static Pile Ball(string key, double x, double y, double radius, double vx, double vy)
        {
            Pile p = new Pile(key, x - 36, y + radius, 72, 66, false) { Snowball = true, Dynamic = true, CohesiveLaunch = true, BallRadius = radius, BallMass = Math.PI * radius * radius, FallVX = vx, FallVY = vy };
            for (int i = 0; i < p.Height.Length; i++)
            {
                double d = (i + .5) * p.Cell - p.Width * .5;
                p.Height[i] = Math.Abs(d) >= radius ? 0 : radius + Math.Sqrt(radius * radius - d * d);
            }
            p.BuildShape(); return p;
        }
        private static void CollisionChecks(StringBuilder report, string folder)
        {
            World bounds = EmptyWorld(90); bounds.Rate = 0; bounds.Config.Interactive = false;
            Pile actor = Ball("bounds", 25, 50, 14, -5000, -5000); bounds.Falling.Add(actor); bounds.Step(.025);
            Rect r = actor.VisualBounds();
            Assert(r.Left >= -.01 && r.Top >= -.01 && actor.FallVX >= 0 && actor.FallVY >= 0, "Fast impacts stay within the real left and top screen edges", report);
            actor = Ball("right-edge", 935, 120, 14, 9000, 0); bounds.Falling.Add(actor); bounds.Step(.025); r = actor.VisualBounds();
            Assert(r.Right <= bounds.Bounds.Right + .01 && actor.FallVX < 0, "The whole irregular shape stays inside the right screen boundary", report);
            World obstacle = EmptyWorld(91); obstacle.Rate = 0;
            obstacle.Sync(new List<Body> { new Body("front", new Rect(330, 200, 220, 260), true, false), new Body("back", new Rect(480, 250, 230, 260), true, false) });
            Pile thrown = Ball("fast", 260, 320, 13, 6000, 0); obstacle.Falling.Add(thrown); obstacle.Step(.025);
            Assert(!obstacle.Falling.Contains(thrown) && obstacle.Count > 5 && thrown.VisualBounds().Right < 331, "A fast throw shatters at an app side instead of tunneling across adjacent windows", report);
            World gap = EmptyWorld(911); gap.Rate = 0;
            gap.Sync(new List<Body> { new Body("left", new Rect(200, 200, 220, 300), true, false), new Body("right", new Rect(440, 220, 220, 280), true, false) });
            Pile squeezed = Ball("gap", 430, 320, 18, 0, 30); gap.Falling.Add(squeezed); gap.Step(.01);
            Rect gapBox = squeezed.VisualBounds();
            bool collision = gapBox.IntersectsWith(new Rect(200.5, 200.5, 219, 299)) || gapBox.IntersectsWith(new Rect(440.5, 220.5, 219, 279));
            Assert(!gap.Falling.Contains(squeezed) || !collision, "A clod wider than an app gap is pushed to a free outside edge rather than trapped in the seam", report);
            World join = EmptyWorld(92); join.Rate = 0;
            Pile a = Ball("one", 300, 120, 12, 70, 0), b = Ball("two", 317, 120, 12, -70, 0);
            double total = a.Mass() + b.Mass(); join.Falling.Add(a); join.Falling.Add(b); join.Step(.005);
            Assert(join.Falling.Count == 1 && Math.Abs(join.Falling[0].Mass() - total) < .01 && Math.Abs(join.Falling[0].FallVX) < .01, "Touching airborne pieces join immediately with conserved mass and momentum", report);
            World touch = EmptyWorld(93); touch.Rate = 0; Pile ground = touch.Piles[0];
            for (int i = 113; i < 127; i++) ground.Height[i] = 15;
            ground.BuildShape(); Pile side = Ball("side", ground.X + 112 * ground.Cell - 8, ground.Y - 8, 10, 300, 0);
            touch.Falling.Add(side); touch.Step(.01);
            Assert(touch.Falling.Count == 0 && ground.Mass() > 15 * 14 * ground.Cell, "Side contact joins the snowbank in the same step without a center-contact delay", report);
            World cap = EmptyWorld(94); cap.Rate = 0; Pile bank = cap.Piles[0];
            for (int i = 105; i < 135; i++) bank.Height[i] = bank.MaxHeight * .96;
            bank.BuildShape(); double old = bank.Mass(); Pile packed = Ball("packed", 600, bank.Y - 40, 22, 0, 1500); double added = packed.Mass();
            cap.Falling.Add(packed); cap.Step(.02);
            double highest = 0; foreach (double h in bank.Height) highest = Math.Max(highest, h);
            Assert(cap.Falling.Count == 0 && highest <= bank.MaxHeight && Math.Abs(bank.Mass() + cap.OverflowMass - old - added) < 2 && bank.Height[100] > 0, "Merging full mounds broadens the bank and accounts for surplus instead of cropping a ceiling", report);
            World hidden = EmptyWorld(95); hidden.Rate = 0;
            Body background = new Body("max", new Rect(0, 0, 960, 610), false, false); background.Solid = false;
            hidden.Sync(new List<Body> { new Body("roof", new Rect(200, 180, 400, 230), true, false), background });
            Pile roof = hidden.Piles.Find(delegate(Pile p) { return p.Key == "roof"; }); roof.Deposit(390, 600); roof.BuildShape();
            hidden.Sync(new List<Body> { background }); double y = hidden.Falling[0].Y;
            for (int i = 0; i < 24; i++) hidden.Step(1.0 / 120);
            Assert(hidden.Falling.Count > 0 && hidden.Falling[0].Dynamic && hidden.Falling[0].Y > y + 5, "Hiding a frame drops its snow over a maximized background instead of erasing it", report);
            using (PileLayer layer = new PileLayer(hidden, hidden.Falling[0])) layer.ExportSurface(System.IO.Path.Combine(folder, "detached-visible.png"));
            World ctrl = EmptyWorld(96); ctrl.Rate = 0; Pile snow = ctrl.Piles[0]; for (int i = 0; i < snow.Height.Length; i++) snow.Height[i] = 8;
            ctrl.Cursor = new Point(480, snow.Y - 2); ctrl.Gesture = 2; ctrl.Interact(.01); double launchY = ctrl.Falling[0].Y;
            for (int i = 0; i < 18; i++) ctrl.Step(1.0 / 120);
            Assert(ctrl.Falling.Count == 2 && ctrl.Falling[0].Y < launchY - 12, "Ctrl portions actually leave the parent bank and stay airborne", report);
            World machine = EmptyWorld(97); machine.Rate = 0; machine.Config.Excavator = true; Pile field = machine.Piles[0];
            for (int i = 0; i < field.Height.Length; i++) field.Height[i] = 18;
            World baseline = EmptyWorld(97); baseline.Rate = 0; for (int i = 0; i < baseline.Piles[0].Height.Length; i++) baseline.Piles[0].Height[i] = 18;
            for (int i = 0; i < 360; i++) { machine.Step(1.0 / 120); baseline.Step(1.0 / 120); }
            double initial = baseline.Piles[0].Mass();
            report.AppendLine("Excavator: x=" + machine.DiggerX + "; age=" + machine.DiggerAge + "; bucket=" + machine.DiggerBucket + "; conservation delta=" + (field.Mass() + machine.DiggerBucket + machine.DiggerClearedMass - initial));
            Assert(machine.DiggerVisible && machine.DiggerAge > 2 && (machine.DiggerBucket > 0 || machine.DiggerClearedMass > 0) && machine.DiggerBucket <= 601 && Math.Abs(field.Mass() + machine.DiggerBucket + machine.DiggerClearedMass - initial) < 10, "The excavator deploys, seeks deep snow and transfers bounded scoopfuls", report);
            using (ExcavatorLayer sprite = new ExcavatorLayer()) sprite.Export(System.IO.Path.Combine(folder, "excavator.png"));
        }
        private static void WinterChecks(StringBuilder report)
        {
            World winter = EmptyWorld(120); winter.Rate = 0; Pile ground = winter.Piles[0];
            for (int i = 0; i < ground.Height.Length; i++)
            {
                double x = (i + .5) * ground.Cell, zone = winter.DecorationSpan + 16;
                double left = Math.Floor(x / zone) * zone, width = Math.Min(zone, ground.Width - left);
                if (x - left < width * .77) ground.Height[i] = ground.MaxHeight * .95;
            }
            winter.Step(.01);
            Assert(winter.Decorations.Count == 0, "Winter toys do not appear below 80% surface peak coverage", report);
            for (int i = 0; i < ground.Height.Length; i++) ground.Height[i] = ground.MaxHeight * .95;
            for (int i = 0; i < 80; i++) winter.Step(1.0 / 120);
            Assert(winter.Decorations.Count >= 4 && winter.Decorations.Exists(delegate(SnowDecoration d) { return d.Tree; }) && winter.Decorations[0].Opacity > .2, "Locally filled regions grow several spaced groups of snowmen and firs", report);
            winter.Config.Decorations = false; for (int i = 0; i < 40; i++) winter.Step(1.0 / 120);
            Assert(winter.Decorations.Count == 0, "The winter-toy setting removes decorations cleanly", report);
            System.Drawing.Bitmap man = DecorationArt.Sprite(false), tree = DecorationArt.Sprite(true);
            Assert(man.Width == 48 && tree.Height == 54 && man.GetPixel(0, 0).A == 0 && tree.GetPixel(0, 0).A == 0, "Generated art is embedded and cached as tiny transparent render sprites", report);
            World impact = EmptyWorld(121); impact.Rate = 0; Pile ball = Ball("impact", 934, 180, 13, 1000, 0); double mass = ball.Mass(); impact.Falling.Add(ball);
            impact.Step(.03); double debris = 0; bool bounded = true, fanUp = false, fanDown = false;
            for (int i = 0; i < impact.Count; i++)
            {
                Flake f = impact.Flakes[i]; debris += f.Area;
                if (f.X < impact.Bounds.Left || f.X > impact.Bounds.Right || !f.Ballistic) bounded = false;
                fanUp |= f.VY < -10; fanDown |= f.VY > 10;
            }
            Assert(impact.Falling.Count == 0 && impact.Count >= 9 && Math.Abs(debris - mass) < .01 && bounded && fanUp && fanDown, "A snowball breaks against the screen edge into bounded ballistic debris with conserved mass", report);
            Pile rounded = Ball("rounded", 200, 100, 18, 0, 0); double low = 100, high = 0;
            foreach (System.Drawing.PointF point in rounded.NativeShapes[0])
            {
                double dx = (point.X - 200) / 1.025, dy = (point.Y - 100) / .97, radius = Math.Sqrt(dx * dx + dy * dy);
                low = Math.Min(low, radius); high = Math.Max(high, radius);
            }
            Assert(high / low < 1.13 && high / low > 1.015, "Snowballs have a gently rounded packed-snow silhouette with only slight asymmetry", report);
        }
        private static void RefinementChecks(StringBuilder report)
        {
            foreach (bool extreme in new bool[] { false, true })
            {
                Settings settings = new Settings { GroundHeight = 48, Extreme = extreme };
                World full = new World(new Rect(0, 0, 960, 640), new Rect(0, 0, 960, 610), settings, 180, 0, extreme ? 141 : 140);
                Pile field = full.Piles[0]; for (int i = 0; i < field.Height.Length; i++) field.Height[i] = field.MaxHeight * .94;
                field.BuildShape(); full.Cursor = new Point(480, field.Y - field.At(480)); full.Gesture = 2; full.Interact(.01); full.Gesture = 0;
                double launch = full.Falling[0].Y;
                for (int i = 0; i < 42; i++) full.Step(1.0 / 120);
                Assert(full.Falling.Count == 2 && full.Falling[0].Y < launch - 50, "Ctrl visibly lifts a maximum-height " + (extreme ? "extreme" : "normal") + " bank instead of reabsorbing it during takeoff", report);
                for (int i = 0; i < 330; i++) full.Step(1.0 / 120);
                Assert(full.Falling.Count == 0 && full.Piles[0].Mass() > 1000, "Launched snow can rejoin its original full surface when it lands", report);
            }
            World edge = EmptyWorld(143); edge.Rate = 0; for (int i = 0; i < edge.Piles[0].Height.Length; i++) edge.Piles[0].Height[i] = 29;
            edge.Cursor = new Point(8, edge.Piles[0].Y - 29); edge.Gesture = 2; edge.Interact(.01); edge.Gesture = 0;
            for (int i = 0; i < 24; i++) edge.Step(1.0 / 120);
            Assert(edge.Falling.Count > 0 && edge.Count > 0, "Ctrl at the screen edge produces an airborne portion and impact debris without flickering the bank", report);
            World machine = EmptyWorld(144); machine.Rate = 0; machine.Config.Excavator = true; Pile floor = machine.Piles[0];
            for (int i = 0; i < floor.Height.Length; i++) floor.Height[i] = 22;
            double left = machine.Bounds.Right, right = machine.Bounds.Left;
            for (int i = 0; i < 120 * 22; i++) { machine.Step(1.0 / 120); if (machine.DiggerAge > 1) { left = Math.Min(left, machine.DiggerX); right = Math.Max(right, machine.DiggerX); } }
            double lowest = 22; foreach (double h in floor.Height) lowest = Math.Min(lowest, h);
            Assert(machine.DiggerClearedMass > 550 && right - left > 120 && lowest < 13 && machine.DiggerBucket <= 601, "The excavator travels between banks, leaves a visible cleared lane and hauls away bounded loads", report);
            System.Drawing.Bitmap sprite = ExcavatorArt.Sprite(); int red = 0, opaque = 0;
            for (int y = 0; y < sprite.Height; y++) for (int x = 0; x < sprite.Width; x++)
            {
                System.Drawing.Color c = sprite.GetPixel(x, y); if (c.A > 100) { opaque++; if (c.R > c.G * 1.4 && c.R > c.B * 1.3) red++; }
            }
            Assert(sprite.Width == 82 && sprite.Height == 56 && red > opaque * .2 && sprite.GetPixel(0, 0).A == 0, "The new generated Santa excavator is embedded as a tiny transparent red sprite", report);
        }
        private static double BrushCut(double depth)
        {
            Settings c = new Settings { GroundHeight = 56 };
            World w = new World(new Rect(0,0,960,640), new Rect(0,0,960,610), c, 180, 0, 162);
            Pile p = w.Piles[0]; for (int i = 0; i < p.Height.Length; i++) p.Height[i] = depth;
            w.Cursor = new Point(350, p.Y - depth); w.CursorSpeed = new Vector(240,0);
            for (int i = 0; i < 12; i++) { w.Time += .005; w.Cursor += new Vector(1.2,0); w.Interact(.005); }
            double cut = 0; foreach (double h in p.Height) cut += Math.Max(0,depth - h) * p.Cell; return cut;
        }
        private static void After23Checks(StringBuilder report, string folder)
        {
            double light = BrushCut(10), deep = BrushCut(48);
            Assert(light > 15 && deep > 0 && deep < light * .65, "Large deep banks resist a cursor sweep while small piles remain easy to push", report);
            World blanket = EmptyWorld(164); blanket.Rate = 0;
            Pile a = new Pile("blanket-a",200,180,160,30,false) { Dynamic=true, FallVX=10 }, b = new Pile("blanket-b",320,180,160,30,false) { Dynamic=true, FallVX=-10 };
            for (int i=0;i<a.Height.Length;i++) a.Height[i]=5; for(int i=0;i<b.Height.Length;i++) b.Height[i]=5; a.BuildShape(); b.BuildShape();
            blanket.Falling.Add(a); blanket.Falling.Add(b); blanket.Step(.005);
            Assert(blanket.Falling.Count == 2 && !blanket.Falling[0].Snowball, "A broad overlapping falling sheet stays in separate pieces instead of collapsing into one ball", report);
            World compact = EmptyWorld(165); compact.Rate=0; double mass=0;
            for(int i=0;i<8;i++) { Pile clod=Ball("dense-"+i,400+(i%2)*2,180+(i%3)*2,18,0,0); compact.Falling.Add(clod);mass+=clod.Mass(); }
            compact.Step(.005); double sum=0,maxRadius=0;
            foreach(Pile clod in compact.Falling) {sum+=clod.Mass();maxRadius=Math.Max(maxRadius,clod.BallRadius);}
            Assert(compact.Falling.Count < 8 && maxRadius<=32.01 && Math.Abs(sum-mass)<.01, "Concentrated snow joins locally but splits its mass across bounded clods instead of a giant sphere", report);
            World density=new World(new Rect(0,0,1980,800),new Rect(0,0,1980,770),new Settings(),180,0,166);
            foreach(Pile p in density.Piles) for(int i=0;i<p.Height.Length;i++) p.Height[i]=p.MaxHeight*.95;
            for(int i=0;i<120;i++) density.Step(1.0/120);
            density.Decorations.Sort(delegate(SnowDecoration x,SnowDecoration y){return x.X.CompareTo(y.X);}); bool spaced=true;
            for(int i=0;i+2<density.Decorations.Count;i++) if(density.Decorations[i+2].X-density.Decorations[i].X<density.DecorationSpan-.5) spaced=false;
            HashSet<int> variants=new HashSet<int>();foreach(SnowDecoration d in density.Decorations) variants.Add(d.Variant);
            Assert(density.Decorations.Count>=6 && spaced && variants.Count==3, "A large snowy surface gets varied groups with at most two figures in any five-icon span", report);
            density.Sync(new List<Body> { new Body("near-a",new Rect(100,200,400,220),true,false),new Body("near-b",new Rect(300,230,400,220),true,false) });
            foreach(Pile p in density.Piles)for(int i=0;i<p.Height.Length;i++)p.Height[i]=p.MaxHeight*.95;
            for(int i=0;i<180;i++)density.Step(1.0/120);
            bool regional=true;
            for(int i=0;i<density.Decorations.Count;i++)for(int j=i+1;j<density.Decorations.Count;j++)for(int k=j+1;k<density.Decorations.Count;k++)
            {
                SnowDecoration d=density.Decorations[i],e=density.Decorations[j],f=density.Decorations[k];
                if(d.Retiring||e.Retiring||f.Retiring)continue;
                if(Math.Max(d.Y,Math.Max(e.Y,f.Y))-Math.Min(d.Y,Math.Min(e.Y,f.Y))>density.DecorationIconPitch)continue;
                if(Math.Max(d.X,Math.Max(e.X,f.X))-Math.Min(d.X,Math.Min(e.X,f.X))<density.DecorationSpan-.5)regional=false;
            }
            Assert(regional,"Nearby window surfaces share the same local decoration quota instead of spawning overlapping groups",report);
            World machine=EmptyWorld(167);machine.Rate=0;machine.Config.Excavator=true; foreach(Pile p in machine.Piles) for(int i=0;i<p.Height.Length;i++)p.Height[i]=25;
            double cleared=0;bool unloadOutside=true,sawOutside=false,sawReturn=false;double minCurl=1,maxCurl=-1;
            for(int i=0;i<120*35;i++)
            {
                machine.Step(1.0/120); bool outside=machine.DiggerX+45<=machine.Bounds.Left||machine.DiggerX-45>=machine.Bounds.Right;
                sawOutside|=outside;sawReturn|=machine.DiggerMode==3;minCurl=Math.Min(minCurl,machine.DiggerCurl);maxCurl=Math.Max(maxCurl,machine.DiggerCurl);
                if(machine.DiggerClearedMass>cleared+.01&&!outside)unloadOutside=false;cleared=machine.DiggerClearedMass;
            }
            Assert(cleared>550&&sawOutside&&sawReturn&&unloadOutside&&machine.Count==0, "The excavator unloads only fully offscreen, returns empty, and emits no snow at the screen edge", report);
            Assert(maxCurl-minCurl>1, "The scoop articulates through digging, carrying and unloading states", report);
            using(System.Drawing.Bitmap down=new System.Drawing.Bitmap(90,66)) using(System.Drawing.Bitmap up=new System.Drawing.Bitmap(90,66))
            {
                using(System.Drawing.Graphics g=System.Drawing.Graphics.FromImage(down))ExcavatorLayer.PaintRig(g,1,0,0,true);
                using(System.Drawing.Graphics g=System.Drawing.Graphics.FromImage(up))ExcavatorLayer.PaintRig(g,1,1,.8,true);
                int changed=0;for(int y=0;y<66;y++)for(int x=0;x<90;x++)if(down.GetPixel(x,y)!=up.GetPixel(x,y))changed++;
                down.Save(Path.Combine(folder,"bucket-down.png"));up.Save(Path.Combine(folder,"bucket-up.png"));
                Assert(changed>100, "The rendered boom and bucket actually move with the articulated joints", report);
            }
            using(System.Drawing.Bitmap gallery=new System.Drawing.Bitmap(520,190))using(System.Drawing.Graphics g=System.Drawing.Graphics.FromImage(gallery))using(System.Drawing.Font font=new System.Drawing.Font("Segoe UI",11))
            {
                g.Clear(System.Drawing.Color.FromArgb(30,35,41));g.DrawString("Матовые снеговички и елочки",font,System.Drawing.Brushes.WhiteSmoke,18,14);
                for(int v=0;v<3;v++)g.DrawImageUnscaled(DecorationArt.Sprite(false,v),30+v*65,58);
                for(int v=0;v<2;v++)g.DrawImageUnscaled(DecorationArt.Sprite(true,v),280+v*70,58);
                using(System.Drawing.Bitmap rig=new System.Drawing.Bitmap(90,66))using(System.Drawing.Graphics r=System.Drawing.Graphics.FromImage(rig))
                { ExcavatorLayer.PaintRig(r,1,1,.8,true);g.DrawImageUnscaled(rig,205,116); }
                gallery.Save(Path.Combine(folder,"new-gallery.png"));
            }
            for(int variant=0;variant<3;variant++) { DecorationArt.Sprite(false,variant).Save(Path.Combine(folder,"snowman-"+variant+".png"));DecorationArt.Sprite(true,variant).Save(Path.Combine(folder,"fir-"+variant+".png")); }
        }
        private static void ThemeChecks(StringBuilder report, string folder)
        {
            ThemeDecision choice=new ThemeDecision(); choice.Observe(.01,true);
            Assert(choice.Dark,"A dark background selects the white-on-dark interface",report);
            choice.Observe(.95,false);Assert(choice.Dark,"A single bright sample cannot flash the theme",report);
            choice.Observe(.95,false);Assert(!choice.Dark,"A sustained bright background selects the black-on-white interface",report);
            for(int i=0;i<20;i++)choice.Observe(i%2==0?.17:.21,false);
            Assert(!choice.Dark,"Small gray-background changes stay inside the theme hysteresis band",report);
            List<double> noisy=new List<double>{.03,.04,.02,.03,.04,.03,1,1,.02};
            Assert(UiTheme.Median(noisy)<.05,"The background median ignores a few bright passing snowflakes",report);
            SettingsPanel panel=new SettingsPanel(new Settings(),delegate{},delegate{},delegate{});
            UiTheme.Apply(panel,false);SaveControl(panel,308,Path.Combine(folder,"settings-light.png"));
            Color lightText=((SolidColorBrush)UiTheme.Get(panel,"Text")).Color,lightPanel=((SolidColorBrush)UiTheme.Get(panel,"Panel")).Color;
            UiTheme.Apply(panel,true);SaveControl(panel,308,Path.Combine(folder,"settings-dark.png"));
            Color darkText=((SolidColorBrush)UiTheme.Get(panel,"Text")).Color,darkPanel=((SolidColorBrush)UiTheme.Get(panel,"Panel")).Color;
            Assert(UiTheme.Contrast(lightText,lightPanel)>10&&UiTheme.Contrast(darkText,darkPanel)>10,"Both panel palettes keep high text contrast",report);
            Toolbar bar=new Toolbar();bar.Snow.IsChecked=true;bar.Snow.BeginAnimation(SnowSwitch.PositionProperty,null);bar.Snow.SetValue(SnowSwitch.PositionProperty,1.0);
            bar.ApplyTheme(false);SaveControl((FrameworkElement)bar.Content,152,Path.Combine(folder,"toolbar-light.png"));
            bar.ApplyTheme(true);SaveControl((FrameworkElement)bar.Content,152,Path.Combine(folder,"toolbar-dark.png"));
            Assert(bar.Snow.IsChecked==true,"Switch state survives a live theme change",report);bar.Close();
            using(System.Drawing.Bitmap sheet=new System.Drawing.Bitmap(660,530))using(System.Drawing.Graphics g=System.Drawing.Graphics.FromImage(sheet))
            using(System.Drawing.Font title=new System.Drawing.Font("Segoe UI",11))
            using(System.Drawing.Image light=System.Drawing.Image.FromFile(Path.Combine(folder,"settings-light.png")))
            using(System.Drawing.Image dark=System.Drawing.Image.FromFile(Path.Combine(folder,"settings-dark.png")))
            using(System.Drawing.Image lightBar=System.Drawing.Image.FromFile(Path.Combine(folder,"toolbar-light.png")))
            using(System.Drawing.Image darkBar=System.Drawing.Image.FromFile(Path.Combine(folder,"toolbar-dark.png")))
            {
                g.Clear(System.Drawing.Color.FromArgb(25,28,33));g.FillRectangle(System.Drawing.Brushes.WhiteSmoke,0,0,330,530);
                g.DrawString("Светлый фон",title,System.Drawing.Brushes.Black,18,12);g.DrawString("Темный фон",title,System.Drawing.Brushes.WhiteSmoke,348,12);
                g.DrawImageUnscaled(lightBar,18,40);g.DrawImageUnscaled(darkBar,348,40);
                g.DrawImageUnscaled(light,11,90);g.DrawImageUnscaled(dark,341,90);sheet.Save(Path.Combine(folder,"adaptive-preview.png"));
            }
        }
        private static double CenterOfMass(Pile pile)
        {
            double moment = 0, mass = 0;
            for (int i = 0; i < pile.Height.Length; i++) { double h = pile.Height[i]; moment += (pile.X + (i + .5) * pile.Cell) * h; mass += h; }
            return moment / Math.Max(.001, mass);
        }
        internal static void Preview(string path)
        {
            World w = new World(new Rect(0, 0, 1160, 720), new Rect(0, 0, 1160, 680), new Settings(), 300, 25, 1970);
            List<Body> bodies = new List<Body>();
            bodies.Add(new Body("browser", new Rect(330, 232, 660, 356), true, false));
            for (int i = 0; i < 7; i++) bodies.Add(new Body("icon" + i, new Rect(48, 65 + i * 83, 38, 38), true, true));
            w.Sync(bodies); w.Cursor = new Point(688, 654);
            for (int i = 0; i < 45 * 130; i++) w.Step(1.0 / 45);
            DrawingVisual visual = new DrawingVisual();
            Typeface font = new Typeface("Segoe UI");
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Art.Brush("#23313C"), null, new Rect(0, 0, 1160, 720));
                dc.DrawRectangle(Art.Brush("#293B46"), null, new Rect(0, 480, 1160, 240));
                // A schematic desktop: no screenshot or personal data in the preview.
                for (int i = 0; i < 7; i++)
                {
                    dc.DrawRoundedRectangle(Art.Brush(i % 2 == 0 ? "#698EAD" : "#8A9D78"), Art.BorderPen, new Rect(48, 65 + i * 83, 38, 38), 6, 6);
                    dc.DrawRoundedRectangle(Art.Ice, null, new Rect(56, 72 + i * 83, 21, 22), 2, 2);
                    Text(dc, "Ярлык " + (i + 1), 38, 107 + i * 83, 10, Art.Text);
                }
                dc.DrawRoundedRectangle(Art.Panel, Art.BorderPen, new Rect(330, 232, 660, 356), 9, 9);
                dc.DrawRoundedRectangle(Art.Brush("#343A43"), null, new Rect(344, 243, 186, 28), 7, 7);
                Text(dc, "Браузер", 360, 247, 12, Art.Text);
                dc.DrawRoundedRectangle(Art.Brush("#23272E"), null, new Rect(347, 280, 625, 29), 14, 14);
                Text(dc, "snow1970 / зимний рабочий стол", 365, 286, 11, Art.Muted);
                dc.DrawRectangle(Art.Brush("#F1F4F5"), null, new Rect(331, 322, 658, 265));
                Text(dc, "Тихая зима.", 378, 360, 37, Art.Brush("#27313D"));
                Text(dc, "Снег оседает на окнах и ярлыках,", 380, 425, 17, Art.Brush("#65717C"));
                Text(dc, "медленно сползая к их краям.", 380, 454, 17, Art.Brush("#65717C"));
                dc.DrawRectangle(Art.Panel, null, new Rect(0, 680, 1160, 40));
                Text(dc, "Snow1970", 24, 693, 12, Art.Muted); Text(dc, "22:10", 1090, 693, 12, Art.Muted);
                SnowCanvas.DrawWorld(dc, w);
            }
            // Render the actual control panel, using the same WPF controls as the app.
            Toolbar bar = new Toolbar(); bar.Snow.IsChecked = true; bar.Status(true, false, "");
            bar.Snow.BeginAnimation(SnowSwitch.PositionProperty, null); bar.Snow.SetValue(SnowSwitch.PositionProperty, 1.0);
            FrameworkElement panel = (FrameworkElement)bar.Content; bar.Content = null;
            panel.Measure(new Size(152, 42)); panel.Arrange(new Rect(0, 0, 152, 42)); panel.UpdateLayout();
            SettingsPanel settingsPanel = new SettingsPanel(new Settings(), delegate { }, delegate { }, delegate { });
            settingsPanel.Measure(new Size(308, double.PositiveInfinity)); settingsPanel.Arrange(new Rect(new Point(), settingsPanel.DesiredSize)); settingsPanel.UpdateLayout();
            DrawingVisual composed = new DrawingVisual();
            using (DrawingContext dc = composed.RenderOpen())
            {
                dc.DrawDrawing(visual.Drawing);
                dc.DrawRectangle(new VisualBrush(panel), null, new Rect(1022, 14, 120, 42));
                dc.DrawRectangle(new VisualBrush(settingsPanel), null, new Rect(831, 59, 308, settingsPanel.ActualHeight));
            }
            RenderTargetBitmap bitmap = new RenderTargetBitmap(1160, 720, 96, 96, PixelFormats.Pbgra32); bitmap.Render(composed);
            PngBitmapEncoder png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path)) png.Save(stream);
            bar.Close();
        }
        private static void Text(DrawingContext dc, string text, double x, double y, double size, Brush color)
        {
            dc.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("ru-RU"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, color, 1.0), new Point(x, y));
        }
    }
}
