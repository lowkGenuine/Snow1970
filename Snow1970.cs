// Snow1970. Portable Windows desktop snow. No packages, network, or installation.
// Build with the inbox .NET Framework compiler; see Start.ps1.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

[assembly: System.Reflection.AssemblyVersion("2.5.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.5.0.0")]

namespace Snow1970
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] internal struct RECT
        {
            public int Left, Top, Right, Bottom;
            public Rect Value { get { return new Rect(Left, Top, Math.Max(0, Right - Left), Math.Max(0, Bottom - Top)); } }
        }
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct SIZE { public int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct BLEND { public byte Operation, Flags, Alpha, Format; }
        [StructLayout(LayoutKind.Sequential)] internal struct BITMAPINFO
        {
            public uint Size; public int Width, Height; public ushort Planes, Bits;
            public uint Compression, ImageSize; public int XPels, YPels; public uint Used, Important, Color;
        }
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFO info, uint usage, out IntPtr bits, IntPtr section, uint offset);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destDC, ref POINT dest, ref SIZE size, IntPtr sourceDC, ref POINT source, uint key, ref BLEND blend, uint flags);
        internal delegate bool EnumProc(IntPtr window, IntPtr data);
        internal delegate void WinEventProc(IntPtr hook, uint ev, IntPtr window, int obj, int child, uint thread, uint time);
        [DllImport("user32.dll")] internal static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc callback, uint process, uint thread, uint flags);
        [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, IntPtr data);
        [DllImport("user32.dll")] internal static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr window, StringBuilder text, int max);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] internal static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] internal static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int w, int h, uint flags);
        [DllImport("user32.dll")] internal static extern bool SetLayeredWindowAttributes(IntPtr window, uint color, byte alpha, uint flags);
        [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] internal static extern int MapWindowPoints(IntPtr from, IntPtr to, [In, Out] RECT[] points, uint count);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern IntPtr SendMessageTimeout(IntPtr window, uint msg, IntPtr wp, IntPtr lp, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr SendMessage(IntPtr window, uint msg, IntPtr wp, IntPtr lp);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] internal static extern int DwmRect(IntPtr window, int attribute, out RECT rect, int size);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] internal static extern int DwmInt(IntPtr window, int attribute, out int value, int size);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr OpenProcess(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint type, uint protect);
        [DllImport("kernel32.dll")] internal static extern bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint type);
        [DllImport("kernel32.dll")] internal static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] bytes, UIntPtr size, out UIntPtr written);
        [DllImport("kernel32.dll")] internal static extern bool ReadProcessMemory(IntPtr process, IntPtr address, byte[] bytes, UIntPtr size, out UIntPtr read);
        [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint mods, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);

        internal static List<Body> Windows(int ownProcess)
        {
            List<Body> result = new List<Body>();
            EnumWindows(delegate(IntPtr window, IntPtr data)
            {
                if (!IsWindowVisible(window) || IsIconic(window)) return true;
                uint process; GetWindowThreadProcessId(window, out process);
                if (process == ownProcess) return true;
                StringBuilder cls = new StringBuilder(128); GetClassName(window, cls, cls.Capacity);
                string name = cls.ToString();
                if (name == "Progman" || name == "WorkerW" || name == "SHELLDLL_DefView" || name == "SysListView32" || name == "CEF-OSC-WIDGET" || name.IndexOf("HwndWrapper[Snow1970", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                int cloaked;
                if (DwmInt(window, 14, out cloaked, 4) == 0 && cloaked != 0) return true;
                // Exclude passive tooltip/shadow surfaces, retain real apps and shell bars.
                int ex = GetWindowLong(window, -20);
                if ((ex & 0x20) != 0 || name == "tooltips_class32" || name == "SysShadow") return true;
                bool shellbar = name == "Shell_TrayWnd" || name == "Shell_SecondaryTrayWnd";
                if (!shellbar && (ex & (0x80 | 0x08000000)) != 0) return true;
                RECT r;
                if (DwmRect(window, 9, out r, 16) != 0 && !GetWindowRect(window, out r)) return true;
                if (r.Right - r.Left < 24 || r.Bottom - r.Top < 20) return true;
                bool roof = name != "Shell_TrayWnd" && name != "Shell_SecondaryTrayWnd" && (ex & 0x80) == 0;
                Body body = new Body("w" + window.ToInt64().ToString(CultureInfo.InvariantCulture), r.Value, roof, false);
                // Rain remains visible in front of maximized and full-screen apps.
                // A window with no exposed top edge acts as the background, not a roof.
                Forms.Screen screen = Forms.Screen.FromHandle(window);
                if (IsZoomed(window) || (roof && r.Top <= screen.WorkingArea.Top + 4)) { body.Solid = false; body.Roof = false; }
                result.Add(body);
                return result.Count < 256;
            }, IntPtr.Zero);
            return result;
        }

        internal static List<Body> DesktopIcons(out string status)
        {
            IntPtr list = IntPtr.Zero;
            IntPtr desktop = FindWindow("Progman", null);
            if (desktop != IntPtr.Zero)
            {
                IntPtr view = FindWindowEx(desktop, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero) list = FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
            }
            EnumWindows(delegate(IntPtr window, IntPtr data)
            {
                if (list != IntPtr.Zero) return false;
                IntPtr view = FindWindowEx(window, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero) list = FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
                if (list == IntPtr.Zero)
                {
                    StringBuilder cls = new StringBuilder(64); GetClassName(window, cls, cls.Capacity);
                    if (cls.ToString() == "Progman" || cls.ToString() == "WorkerW")
                        EnumChildWindows(window, delegate(IntPtr child, IntPtr unused)
                        {
                            StringBuilder c = new StringBuilder(64); GetClassName(child, c, c.Capacity);
                            if (c.ToString() == "SHELLDLL_DefView") list = FindWindowEx(child, IntPtr.Zero, "SysListView32", null);
                            return list == IntPtr.Zero;
                        }, IntPtr.Zero);
                }
                return list == IntPtr.Zero;
            }, IntPtr.Zero);
            List<Body> icons = new List<Body>();
            status = "Значки рабочего стола скрыты или недоступны";
            if (list == IntPtr.Zero || (GetWindowLong(list, -16) & 0x10000000) == 0) return icons;
            IntPtr count;
            if (SendMessageTimeout(list, 0x1004, IntPtr.Zero, IntPtr.Zero, 0x22, 80, out count) == IntPtr.Zero) return icons;
            int length = Math.Min(1024, Math.Max(0, count.ToInt32()));
            uint id; GetWindowThreadProcessId(list, out id);
            // LVM_GETITEMRECT is an in/out pointer message. Only a 16-byte RECT
            // is exchanged with Explorer; no code, icon names, or files are read.
            IntPtr process = OpenProcess(0x38, false, id);
            if (process == IntPtr.Zero) { status = "Нет доступа к геометрии значков"; return icons; }
            IntPtr remote = IntPtr.Zero;
            try
            {
                remote = VirtualAllocEx(process, IntPtr.Zero, new UIntPtr(16), 0x3000, 4);
                if (remote == IntPtr.Zero) return icons;
                byte[] buffer = new byte[16]; UIntPtr done;
                Stopwatch budget = Stopwatch.StartNew();
                for (int i = 0; i < length; i++)
                {
                    if (budget.ElapsedMilliseconds > 250) { status = "Обновление значков отложено"; return null; }
                    Array.Clear(buffer, 0, 16); buffer[0] = 1; // LVIR_ICON, not the icon label.
                    if (!WriteProcessMemory(process, remote, buffer, new UIntPtr(16), out done) || done.ToUInt64() != 16) return null;
                    // Pointer messages must finish before their buffer is freed.
                    // This runs only on the single background reader, never the UI thread.
                    if (SendMessage(list, 0x100E, new IntPtr(i), remote) == IntPtr.Zero) continue;
                    if (!ReadProcessMemory(process, remote, buffer, new UIntPtr(16), out done) || done.ToUInt64() != 16) continue;
                    RECT[] mapped = { new RECT { Left = BitConverter.ToInt32(buffer, 0), Top = BitConverter.ToInt32(buffer, 4), Right = BitConverter.ToInt32(buffer, 8), Bottom = BitConverter.ToInt32(buffer, 12) } };
                    MapWindowPoints(list, IntPtr.Zero, mapped, 2);
                    Rect r = mapped[0].Value;
                    if (r.Width > 8 && r.Height > 8 && r.Width < 512 && r.Height < 512)
                        icons.Add(new Body("i" + list.ToInt64() + ":" + i, r, true, true));
                }
                status = "Значков: " + icons.Count;
                return icons;
            }
            finally
            {
                if (remote != IntPtr.Zero) VirtualFreeEx(process, remote, UIntPtr.Zero, 0x8000);
                CloseHandle(process);
            }
        }
    }

    internal sealed class Body
    {
        internal string Key;
        internal Rect Rect;
        internal bool Roof, Icon, Solid = true;
        internal Body(string key, Rect rect, bool roof, bool icon) { Key = key; Rect = rect; Roof = roof; Icon = icon; }
    }

    internal sealed class Settings
    {
        internal string Quality = "Auto";
        internal bool AllMonitors = true;
        internal bool StartEnabled = true;
        internal bool Interactive = true;
        internal bool Decorations = true;
        internal bool CursorInteraction = true, GestureInteraction = true, Excavator = false;
        internal bool SyncDisplay = true, SnowOnIcons = false, Extreme = false;
        internal int MaxFlakes = 180, Fps = 60;
        internal double Rate = 17, GroundHeight = 32, WindowHeight = 13, IconHeight = 10;
        internal static Settings Read(string folder)
        {
            Settings s = new Settings();
            string path = Path.Combine(folder, "Settings.ini");
            if (!File.Exists(path)) return s;
            foreach (string line in File.ReadAllLines(path))
            {
                string[] pair = line.Split(new char[] { '=' }, 2);
                if (pair.Length != 2) continue;
                string k = pair[0].Trim().ToLowerInvariant(), v = pair[1].Trim();
                double n; bool numeric = double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out n);
                if (k == "quality" && (v == "Auto" || v == "Eco" || v == "Smooth")) s.Quality = v;
                if (k == "monitors") s.AllMonitors = v.ToLowerInvariant() != "primary";
                if (k == "startenabled") s.StartEnabled = v == "1";
                if (k == "interactive") s.Interactive = v != "0";
                if (k == "cursorinteraction") s.CursorInteraction = v != "0";
                if (k == "gestureinteraction") s.GestureInteraction = v != "0";
                if (k == "decorations") s.Decorations = v != "0";
                if (k == "excavator") s.Excavator = v == "1";
                if (k == "syncdisplay") s.SyncDisplay = v != "0";
                if (k == "snowonicons") s.SnowOnIcons = v == "1";
                if (k == "extreme") s.Extreme = v == "1";
                if (!numeric || double.IsNaN(n) || double.IsInfinity(n)) continue;
                if (k == "maxflakes") s.MaxFlakes = (int)Clamp(n, 40, 900);
                if (k == "fps") s.Fps = (int)Clamp(n, 24, 500);
                if (k == "spawnpersecond") s.Rate = Clamp(n, 4, 65);
                if (k == "groundheight") s.GroundHeight = Clamp(n, 4, 56);
                if (k == "windowheight") s.WindowHeight = Clamp(n, 3, 22);
                if (k == "iconheight") s.IconHeight = Clamp(n, 3, 16);
            }
            return s;
        }
        internal void Save(string folder)
        {
            string data = "; Snow1970 settings\r\nQuality=" + Quality + "\r\nMonitors=" + (AllMonitors ? "All" : "Primary") + "\r\nStartEnabled=" + (StartEnabled ? "1" : "0") + "\r\nMaxFlakes=" + MaxFlakes + "\r\nFPS=" + Fps + "\r\nSpawnPerSecond=" + Rate.ToString(CultureInfo.InvariantCulture) + "\r\nGroundHeight=" + GroundHeight.ToString(CultureInfo.InvariantCulture) + "\r\nWindowHeight=" + WindowHeight.ToString(CultureInfo.InvariantCulture) + "\r\nIconHeight=" + IconHeight.ToString(CultureInfo.InvariantCulture) + "\r\n";
            data += "Decorations=" + (Decorations ? "1" : "0") + "\r\n";
            data += "Interactive=" + (Interactive ? "1" : "0") + "\r\n";
            data += "CursorInteraction=" + (CursorInteraction ? "1" : "0") + "\r\nGestureInteraction=" + (GestureInteraction ? "1" : "0") + "\r\nExcavator=" + (Excavator ? "1" : "0") + "\r\n";
            data += "SyncDisplay=" + (SyncDisplay ? "1" : "0") + "\r\nSnowOnIcons=" + (SnowOnIcons ? "1" : "0") + "\r\n";
            data += "Extreme=" + (Extreme ? "1" : "0") + "\r\n";
            File.WriteAllText(Path.Combine(folder, "Settings.ini"), data, Encoding.UTF8);
        }
        internal static double Clamp(double v, double low, double high) { return Math.Max(low, Math.Min(high, v)); }
        internal double HeightLimit(double normal) { return normal * (Extreme ? 2 : 1); }
    }

    internal struct Flake
    {
        internal double X, Y, VX, VY, Radius, Phase, Age, Area;
        internal bool Ballistic;
        internal bool Sublimating;
    }

    internal sealed class Pile
    {
        internal string Key;
        internal double X, Y, Width, Cell, MaxHeight;
        internal double[] Height;
        internal double[] Flow, NextHeight;
        internal double[] NextMomentum;
        internal double ContactUntil;
        private double[] drawnHeight;
        private bool[] drawnExposed;
        internal int RenderVersion;
        internal double ShapeX, ShapeY;
        internal bool[] Exposed;
        internal bool Ground;
        internal Geometry Shape;
        internal double LeftSpill, RightSpill;
        internal List<Drawing.PointF[]> NativeShapes = new List<Drawing.PointF[]>();
        internal Drawing.Drawing2D.GraphicsPath SmoothPath;
        internal double FallAge, FallVY, FallVX, BreakClock;
        internal bool Snowball, CohesiveLaunch;
        internal double BallRadius, BallMass;
        internal bool Dynamic;
        internal string DepartedSupport;
        internal Drawing.RectangleF ShapeBounds;
        internal double ShapePhase;
        private double drawnRadius = -1;
        internal Pile(string key, double x, double y, double width, double max, bool ground)
        {
            Key = key; X = x; Y = y; Width = width; MaxHeight = max; Ground = ground;
            uint seed = 2166136261; foreach (char ch in key) seed = (seed ^ ch) * 16777619;
            ShapePhase = (seed % 6283) / 1000.0;
            int n = Math.Max(2, (int)Math.Ceiling(width / 5));
            Cell = width / n; Height = new double[n]; Flow = new double[n]; NextHeight = new double[n]; Exposed = new bool[n];
            NextMomentum = new double[n];
            drawnHeight = new double[n]; drawnExposed = new bool[n];
            for (int i = 0; i < n; i++) Exposed[i] = true;
        }
        internal double At(double x)
        {
            int i = Math.Max(0, Math.Min(Height.Length - 1, (int)((x - X) / Cell)));
            return Height[i];
        }
        internal void Deposit(double x, double area)
        {
            int center = Math.Max(0, Math.Min(Height.Length - 1, (int)((x - X) / Cell)));
            int start = Math.Max(0, center - 2), end = Math.Min(Height.Length - 1, center + 2);
            double weight = 0;
            for (int i = start; i <= end; i++) weight += 3 - Math.Abs(i - center);
            for (int i = start; i <= end; i++) Height[i] += area * (3 - Math.Abs(i - center)) / (weight * Cell);
        }
        internal double Mass()
        {
            if (Snowball) return BallMass;
            double m = 0; for (int i = 0; i < Height.Length; i++) m += Height[i] * Cell; return m;
        }
        internal Rect VisualBounds()
        {
            return new Rect(ShapeBounds.X + X - ShapeX, ShapeBounds.Y + Y - ShapeY, Math.Max(.1, ShapeBounds.Width), Math.Max(.1, ShapeBounds.Height));
        }
        internal void BuildShape()
        {
            bool changed = SmoothPath == null || (Snowball && Math.Abs(BallRadius - drawnRadius) > .015);
            for (int i = 0; i < Height.Length; i++)
                if (Math.Abs(Height[i] - drawnHeight[i]) > .035 || Exposed[i] != drawnExposed[i]) { changed = true; break; }
            if (!changed) return;
            Array.Copy(Height, drawnHeight, Height.Length); Array.Copy(Exposed, drawnExposed, Exposed.Length); RenderVersion++;
            drawnRadius = BallRadius;
            ShapeX = X; ShapeY = Y;
            if (SmoothPath != null) SmoothPath.Dispose(); SmoothPath = new Drawing.Drawing2D.GraphicsPath();
            NativeShapes.Clear();
            if (Mass() < .12) { Shape = null; return; }
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext g = geometry.Open())
            {
                int i = 0;
                while (i < Height.Length)
                {
                    while (i < Height.Length && (!Exposed[i] || Height[i] < .07)) i++;
                    if (i >= Height.Length) break;
                    int start = i;
                    g.BeginFigure(new Point(X + start * Cell, Y + .5), true, true);
                    g.LineTo(new Point(X + start * Cell, Y - Height[start] * .75), true, false);
                    while (i < Height.Length && Exposed[i] && Height[i] >= .07)
                    {
                        g.LineTo(new Point(X + (i + .5) * Cell, Y - Height[i]), true, false); i++;
                    }
                    g.LineTo(new Point(X + i * Cell, Y - Height[i - 1] * .75), true, false);
                    g.LineTo(new Point(X + i * Cell, Y + .5), true, false);
                }
            }
            geometry.Freeze(); Shape = geometry;
            if (Snowball)
            {
                Drawing.PointF[] outline = new Drawing.PointF[28];
                for (int j = 0; j < outline.Length; j++)
                {
                    double a = j * Math.PI * 2 / outline.Length;
                    double rough = 1 + .034 * Math.Sin(3 * a + ShapePhase) + .016 * Math.Sin(5 * a + ShapePhase * 1.7) + .007 * Math.Sin(8 * a + .8);
                    outline[j] = new Drawing.PointF((float)(X + Width * .5 + BallRadius * 1.025 * rough * Math.Cos(a)), (float)(Y - BallRadius + BallRadius * .97 * rough * Math.Sin(a)));
                }
                SmoothPath.AddClosedCurve(outline, .28f); NativeShapes.Add(outline); ShapeBounds = SmoothPath.GetBounds(); return;
            }
            int index = 0;
            while (index < Height.Length)
            {
                while (index < Height.Length && (!Exposed[index] || Height[index] < .07)) index++;
                if (index >= Height.Length) break;
                int start = index;
                List<Drawing.PointF> points = new List<Drawing.PointF>();
                points.Add(new Drawing.PointF((float)(X + start * Cell), (float)(Y + 1)));
                points.Add(new Drawing.PointF((float)(X + start * Cell), (float)(Y - Height[start] * .75)));
                while (index < Height.Length && Exposed[index] && Height[index] >= .07)
                {
                    points.Add(new Drawing.PointF((float)(X + (index + .5) * Cell), (float)(Y - Height[index]))); index++;
                }
                points.Add(new Drawing.PointF((float)(X + index * Cell), (float)(Y - Height[index - 1] * .75)));
                points.Add(new Drawing.PointF((float)(X + index * Cell), (float)(Y + 1))); NativeShapes.Add(points.ToArray());
                // A continuous cubic surface hides the simulation grid completely.
                List<Drawing.PointF> ridge = new List<Drawing.PointF>();
                for (int j = start; j < index; j++)
                {
                    double h = Height[j];
                    if (j > start && j + 1 < index) h = (Height[j - 1] + 2 * Height[j] + Height[j + 1]) * .25;
                    double powder = (.23 * Math.Sin(j * 2.31) + .16 * Math.Sin(j * .73 + 1.2)) * Math.Min(1, h * .2);
                    ridge.Add(new Drawing.PointF((float)(X + (j + .5) * Cell), (float)(Y - h - powder)));
                }
                SmoothPath.StartFigure();
                ridge.Insert(0, new Drawing.PointF((float)(X + start * Cell), (float)(Y + .25)));
                ridge.Add(new Drawing.PointF((float)(X + index * Cell), (float)(Y + .25)));
                SmoothPath.AddCurve(ridge.ToArray(), .32f); SmoothPath.CloseFigure();
            }
            ShapeBounds = SmoothPath.PointCount > 0 ? SmoothPath.GetBounds() : new Drawing.RectangleF((float)X, (float)Y, 0, 0);
        }
    }

    internal sealed partial class World
    {
        internal Rect Bounds, Working;
        internal List<Pile> Piles = new List<Pile>();
        internal List<Pile> Falling = new List<Pile>();
        internal Pile HeldBall;
        internal Pile Sliding;
        private Pile slideSupport;
        private double slideSupportX, slideContact;
        private int previousGesture;
        private double nextCtrlBurst;
        private int detachedSerial;
        internal List<Body> Bodies = new List<Body>();
        internal Flake[] Flakes;
        internal int Count, Limit;
        internal double Rate, Time, SkyArea;
        internal Geometry Sky;
        internal Drawing.Region NativeSky;
        internal int TerrainVersion, Gesture;
        internal int SceneVersion;
        internal bool MovingSnow, SupportDrag;
        internal Point Cursor;
        internal Vector CursorSpeed;
        internal Settings Config;
        private Random random;
        private double credit, terrainClock;
        private List<Body>[] bins;
        private List<Pile>[] roofBins;
        private const double BinSize = 96;
        internal World(Rect bounds, Rect working, Settings config, int capacity, double rate, int seed)
        {
            Bounds = bounds; Working = working; Config = config;
            Limit = capacity; Flakes = new Flake[capacity]; Rate = rate; random = new Random(seed);
            Piles.Add(new Pile("ground", bounds.Left, working.Bottom - 1, bounds.Width, config.HeightLimit(config.GroundHeight), true));
            Sync(new List<Body>());
        }
        internal void Clear()
        {
            Count = 0; credit = 0;
            OverflowMass = 0;
            Decorations.Clear();
            DiggerBucket = DiggerAge = DiggerClearedMass = 0; DiggerVisible = false;
            HeldBall = null; Sliding = null; slideSupport = null; previousGesture = 0;
            foreach (Pile p in Falling) if (p.SmoothPath != null) p.SmoothPath.Dispose(); Falling.Clear();
            foreach (Pile p in Piles) { Array.Clear(p.Height, 0, p.Height.Length); Array.Clear(p.Flow, 0, p.Flow.Length); p.Shape = null; p.NativeShapes.Clear(); if (p.SmoothPath != null) { p.SmoothPath.Dispose(); p.SmoothPath = null; } p.LeftSpill = p.RightSpill = 0; }
            TerrainVersion++;
        }
        internal void SeedAir()
        {
            Pile ground = Piles[0];
            if (ground.Mass() < 1)
            {
                for (int i = 0; i < ground.Height.Length; i++) ground.Height[i] = 7 * Math.Pow(Math.Max(0, Math.Sin(i * .065)), 4);
                ground.BuildShape(); TerrainVersion++;
            }
            for (int i = 0; i < Math.Min(36, Limit / 5); i++)
            {
                double x = Bounds.Left + random.NextDouble() * Bounds.Width;
                double y = Bounds.Top + random.NextDouble() * Bounds.Height;
                if (!InsideBody(x, y)) Add(x, y, random.NextDouble() * 12 - 6, 28 + random.NextDouble() * 25, 1 + random.NextDouble() * 1.5);
            }
        }
        internal void Add(double x, double y, double vx, double vy, double radius, double area = 0)
        {
            if (Count >= Limit || Count >= Flakes.Length) return;
            if (area <= 0 && Config.Extreme) vy *= 2;
            Flakes[Count++] = new Flake { X = x, Y = y, VX = vx, VY = vy, Radius = radius, Phase = random.NextDouble() * 6.283, Age = 0, Area = area > 0 ? area : radius * radius * 15 };
        }
        private int Bin(double x) { return Math.Max(0, Math.Min(bins.Length - 1, (int)((x - Bounds.Left) / BinSize))); }
        private bool InsideBody(double x, double y)
        {
            if (bins == null) return false;
            foreach (Body b in bins[Bin(x)]) if (b.Rect.Contains(x, y)) return b.Solid;
            return false;
        }
        internal bool CanDrawFlake(double x, double y)
        {
            if (x < Bounds.Left || x >= Bounds.Right || y > Working.Bottom) return false;
            return !InsideBody(x, y);
        }
        internal void Sync(List<Body> bodies)
        {
            SceneVersion++;
            Bodies = bodies;
            Dictionary<string, Pile> old = new Dictionary<string, Pile>();
            foreach (Pile p in Piles) old[p.Key] = p;
            List<Pile> next = new List<Pile>(); next.Add(old["ground"]); old.Remove("ground");
            foreach (Body body in bodies)
            {
                Rect r = body.Rect;
                if (!body.Roof || r.Top < Bounds.Top + 2 || r.Top >= Working.Bottom || r.Right <= Bounds.Left || r.Left >= Bounds.Right) continue;
                double inset = body.Icon ? 0 : 7;
                double x = Math.Max(r.Left + inset, Bounds.Left), width = Math.Min(r.Right - inset, Bounds.Right) - x;
                if (width < 4) continue;
                Pile p;
                if (old.TryGetValue(body.Key, out p))
                {
                    old.Remove(body.Key);
                    if (body.Icon && Math.Abs(p.X - x) + Math.Abs(p.Y - r.Top) > 2)
                    {
                        Detach(p); p = new Pile(body.Key, x, r.Top, width, Config.HeightLimit(Math.Min(Config.IconHeight, width * .28)), false);
                    }
                    else if (Math.Abs(p.Width - width) > 4)
                    {
                        Pile resized = new Pile(body.Key, x, r.Top, width, p.MaxHeight, false);
                        // Re-sample on resize; a narrower support sheds the excess.
                        double ratio = Math.Min(1, p.Width / width);
                        for (int i = 0; i < resized.Height.Length; i++)
                            resized.Height[i] = p.Height[Math.Min(p.Height.Length - 1, i * p.Height.Length / resized.Height.Length)] * ratio;
                        double lost = Math.Max(0, p.Mass() - resized.Mass());
                        Release(p, lost); p = resized;
                    }
                    p.X = x; p.Y = r.Top;
                }
                else p = new Pile(body.Key, x, r.Top, width, Config.HeightLimit(body.Icon ? Math.Min(Config.IconHeight, width * .28) : Config.WindowHeight), false);
                next.Add(p);
            }
            foreach (Pile p in old.Values) Detach(p);
            Piles = next;
            RefreshCollisionGeometry();
            int binCount = Math.Max(1, (int)Math.Ceiling(Bounds.Width / BinSize));
            bins = new List<Body>[binCount];
            roofBins = new List<Pile>[binCount];
            for (int i = 0; i < binCount; i++) { bins[i] = new List<Body>(); roofBins[i] = new List<Pile>(); }
            foreach (Body b in bodies)
                if (b.Rect.Right > Bounds.Left && b.Rect.Left < Bounds.Right && b.Rect.Bottom > Bounds.Top && b.Rect.Top < Bounds.Bottom)
                    for (int i = Bin(b.Rect.Left - 5); i <= Bin(b.Rect.Right + 5); i++) bins[i].Add(b);
            foreach (Pile p in Piles)
                for (int i = Bin(p.X); i <= Bin(p.X + p.Width); i++) roofBins[i].Add(p);
            // A rectangle union handles overlapping windows without XOR holes.
            using (Drawing.Region region = new Drawing.Region(new Drawing.RectangleF((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height)))
            using (Drawing.Drawing2D.Matrix matrix = new Drawing.Drawing2D.Matrix())
            {
                for (int bodyIndex = 0; bodyIndex < bodies.Count; bodyIndex++)
                {
                    Body body = bodies[bodyIndex];
                    if (body.Icon || !body.Solid) continue;
                    using (Drawing.Region visible = new Drawing.Region(new Drawing.RectangleF((float)body.Rect.X, (float)body.Rect.Y, (float)body.Rect.Width, (float)body.Rect.Height)))
                    {
                        // Only the visible part of a solid window blocks snow. A maximized
                        // foreground app hides all of the small windows behind it.
                        for (int j = 0; j < bodyIndex; j++)
                        {
                            Rect cover = bodies[j].Rect;
                            if (!bodies[j].Icon) visible.Exclude(new Drawing.RectangleF((float)cover.X, (float)cover.Y, (float)cover.Width, (float)cover.Height));
                        }
                        region.Exclude(visible);
                    }
                }
                Drawing.RectangleF[] scans = region.GetRegionScans(matrix);
                StreamGeometry geometry = new StreamGeometry(); geometry.FillRule = FillRule.Nonzero;
                SkyArea = 0;
                using (StreamGeometryContext g = geometry.Open())
                    foreach (Drawing.RectangleF r in scans)
                    {
                        SkyArea += r.Width * r.Height;
                        g.BeginFigure(new Point(r.Left, r.Top), true, true);
                        g.LineTo(new Point(r.Right, r.Top), false, false);
                        g.LineTo(new Point(r.Right, r.Bottom), false, false);
                        g.LineTo(new Point(r.Left, r.Bottom), false, false);
                    }
                geometry.Freeze(); Sky = geometry;
                if (NativeSky != null) NativeSky.Dispose(); NativeSky = region.Clone();
            }
            foreach (Pile p in Piles)
            {
                for (int i = 0; i < p.Height.Length; i++)
                {
                    double x = p.X + (i + .5) * p.Cell;
                    if (p.Ground) { p.Exposed[i] = true; continue; }
                    bool exposed = true;
                    foreach (Body b in bins[Bin(x)])
                    {
                        if (b.Key == p.Key) break; // Native EnumWindows lists the frontmost first.
                        if (b.Rect.Contains(x, p.Y - .8)) { exposed = false; break; }
                    }
                    p.Exposed[i] = exposed;
                }
                p.BuildShape();
            }
            TerrainVersion++;
            // Newly moved windows swallow particles that would otherwise be inside a solid.
            for (int i = Count - 1; i >= 0; i--)
                if (InsideBody(Flakes[i].X, Flakes[i].Y))
                {
                    Flake f = Flakes[i]; ResolveFlakeMotion(ref f, f.X, f.Y); Flakes[i] = f;
                }
        }
        internal IEnumerable<Pile> DisplayPiles()
        {
            foreach (Pile p in Piles) yield return p;
            foreach (Pile p in Falling) yield return p;
            if (HeldBall != null) yield return HeldBall;
            if (Sliding != null) yield return Sliding;
        }
        private void Release(Pile p, double mass)
        {
            if (mass < 3) return;
            int n = Math.Min(32, (int)(mass / 22) + 1);
            for (int i = 0; i < n; i++)
            {
                double x = p.X + random.NextDouble() * p.Width;
                Add(x, p.Y - p.At(x) - 2, (random.NextDouble() - .5) * 45, 10 + random.NextDouble() * 24, 1.2 + random.NextDouble(), Math.Min(80, mass / n));
            }
        }
        private void PriorityAdd(double x, double y, double vx, double vy, double radius, double area)
        {
            if (Count >= Limit && Count > 0) Count--;
            int index = Count;
            Add(x, y, vx, vy, radius, area);
            if (Count > index) { Flake f = Flakes[index]; f.Ballistic = true; Flakes[index] = f; }
        }
        private void Arc(double x, double y, double direction, double area)
        {
            if (Count >= Limit && Count > 0) Count--;
            int index = Count;
            Add(x, y, direction * (45 + random.NextDouble() * 35), -65 - random.NextDouble() * 40, 1.25 + random.NextDouble(), area);
            if (Count > index) { Flake f = Flakes[index]; f.Ballistic = true; Flakes[index] = f; }
        }
        private Pile LiftSection(Pile source, int start, int end, double fraction, double vx, double vy, bool airborne = true)
        {
            start = Math.Max(0, start); end = Math.Min(source.Height.Length - 1, end);
            if (end < start) return null;
            double available = 0;
            for (int i = start; i <= end; i++) available += source.Height[i] * fraction * source.Cell;
            if (available < 3) return null;
            Pile piece = new Pile("lift:" + (++detachedSerial), source.X + start * source.Cell, source.Y, (end - start + 1) * source.Cell, source.MaxHeight, false);
            for (int i = 0; i < piece.Height.Length; i++) piece.Height[i] = source.At(piece.X + (i + .5) * piece.Cell) * fraction;
            double removed = 0;
            for (int i = start; i <= end; i++) { removed += source.Height[i] * fraction * source.Cell; source.Height[i] *= 1 - fraction; source.Flow[i] = 0; }
            double scale = removed / Math.Max(.001, piece.Mass());
            for (int i = 0; i < piece.Height.Length; i++) piece.Height[i] *= scale;
            if (removed < 3) return null;
            double remainingTop = 0;
            for (int i = start; i <= end; i++) remainingTop = Math.Max(remainingTop, source.Height[i]);
            piece.Y = source.Y - remainingTop - .6;
            piece.Dynamic = true; piece.DepartedSupport = source.Key; piece.FallVX = vx; piece.FallVY = vy; piece.CohesiveLaunch = true; piece.BuildShape(); source.BuildShape();
            if (airborne)
            {
                if (Falling.Count >= 12) { Release(Falling[0], Falling[0].Mass()); Falling.RemoveAt(0); }
                Falling.Add(piece);
            }
            TerrainVersion++; return piece;
        }
        private void StampSliding()
        {
            Pile piece = Sliding, support = slideSupport;
            if (piece == null || support == null) return;
            DepositActor(piece, support); Sliding = null; slideSupport = null;
        }
        private void LaunchSliding(double vx, double vy)
        {
            if (Sliding == null) return;
            Sliding.DepartedSupport = slideSupport == null ? null : slideSupport.Key;
            Sliding.FallVX = vx; Sliding.FallVY = vy; Sliding.FallAge = 0; Sliding.CohesiveLaunch = true;
            Falling.Add(Sliding); Sliding = null; slideSupport = null; TerrainVersion++;
        }
        private void MoveSliding(double dt)
        {
            if (Sliding == null) return;
            if (slideSupport == null || !Piles.Contains(slideSupport)) { LaunchSliding(Sliding.FallVX, 12); return; }
            Sliding.X += slideSupport.X - slideSupportX; slideSupportX = slideSupport.X; Sliding.Y = slideSupport.Y - .6;
            bool touching = Gesture == 0 && Config.Interactive && !SupportDrag && Math.Abs(CursorSpeed.X) > 28 && Cursor.X >= Sliding.X - 60 && Cursor.X <= Sliding.X + Sliding.Width + 60 && Cursor.Y >= Sliding.Y - Sliding.MaxHeight - 24 && Cursor.Y <= Sliding.Y + 28;
            if (touching) { Sliding.FallVX = Settings.Clamp(CursorSpeed.X * .92, -850, 850); slideContact = Time; }
            else Sliding.FallVX = Math.Sign(Sliding.FallVX) * Math.Max(0, Math.Abs(Sliding.FallVX) - 3800 * dt);
            Contact contact = MoveActor(Sliding, Sliding.FallVX * dt, 0, false);
            if (contact.Support != null)
            {
                DepositActor(Sliding, contact.Support); Sliding = null; slideSupport = null; return;
            }
            if (!slideSupport.Ground && (Sliding.X < slideSupport.X - Sliding.Width * .15 || Sliding.X + Sliding.Width > slideSupport.X + slideSupport.Width + Sliding.Width * .15))
            {
                LaunchSliding(Sliding.FallVX * .6, -80); return;
            }
            if ((!touching && Math.Abs(Sliding.FallVX) < 5) || (contact.Valid && contact.NX != 0)) StampSliding();
        }
        internal void ShakeSupport(string key, double speed, double strength)
        {
            foreach (Pile p in Piles)
            {
                if (p.Key != key || p.Mass() < 5) continue;
                int span = Math.Max(2, Math.Min(p.Height.Length, (int)(85 / p.Cell)));
                int start = speed > 0 ? p.Height.Length - span : 0;
                LiftSection(p, start, start + span - 1, Settings.Clamp(strength, .08, .55), Settings.Clamp(speed * .3, -300, 300), -32);
                break;
            }
        }
        private void BallShape()
        {
            Pile ball = HeldBall; if (ball == null) return;
            double oldX = ball.X, oldY = ball.Y;
            ball.BallRadius = Math.Sqrt(ball.BallMass / Math.PI);
            ball.X = Cursor.X - ball.Width * .5; ball.Y = Cursor.Y - 14 + ball.BallRadius;
            ProfileBall(ball);
            double dx = ball.X - oldX, dy = ball.Y - oldY; ball.X = oldX; ball.Y = oldY;
            MoveActor(ball, dx, dy, true);
        }
        private void ReleaseBall(bool upward)
        {
            Pile ball = HeldBall; if (ball == null) return;
            ball.FallVX = Settings.Clamp(ball.FallVX, -900, 900);
            ball.FallVY = upward ? -300 : Settings.Clamp(ball.FallVY, -850, 650);
            if (upward && Math.Abs(ball.FallVX) < 40) ball.FallVX = CursorSpeed.X < 0 ? -100 : 100;
            if (Math.Abs(ball.FallVX) + Math.Abs(ball.FallVY) < 45) { ball.FallVX = 60; ball.FallVY = -45; }
            ball.FallAge = 0; ball.CohesiveLaunch = true; Falling.Add(ball); HeldBall = null; TerrainVersion++;
        }
        private void GatherBall(Pile source, double dt)
        {
            double maxMass = Math.PI * Math.Pow(Config.Extreme ? 30 : 22, 2);
            double available = maxMass - (HeldBall == null ? 0 : HeldBall.BallMass);
            if (available < .1) return;
            double gathered = 0;
            for (int i = 0; i < source.Height.Length; i++)
            {
                double distance = Math.Abs(source.X + (i + .5) * source.Cell - Cursor.X);
                if (distance > 96 || !source.Exposed[i]) continue;
                double take = Math.Min(source.Height[i] * Math.Min(.3, dt * 5 * (1 - distance / 96)), available / source.Cell);
                source.Height[i] -= take; source.Flow[i] = 0; gathered += take * source.Cell; available -= take * source.Cell;
                if (available < .01) break;
            }
            if (gathered <= .01) return;
            if (HeldBall == null) HeldBall = new Pile("ball:" + (++detachedSerial), Cursor.X - 36, Cursor.Y, 72, 66, false) { Snowball = true, Dynamic = true };
            HeldBall.BallMass += gathered; BallShape();
        }
        private void Detach(Pile p)
        {
            if (p.Mass() < 3) return;
            int parts = Math.Min(6, Math.Max(1, (int)Math.Ceiling(p.Width / 220)));
            for (int part = 0; part < parts; part++)
            {
                double width = p.Width / parts;
                Pile piece = new Pile("fall:" + (++detachedSerial), p.X + part * width, p.Y, width, p.MaxHeight, false);
                for (int i = 0; i < piece.Height.Length; i++) piece.Height[i] = p.At(piece.X + (i + .5) * piece.Cell);
                if (piece.Mass() < 3) continue;
                piece.Dynamic = true; piece.FallVY = 12 + random.NextDouble() * 14; piece.FallVX = (random.NextDouble() - .5) * 15; piece.BuildShape();
                if (Falling.Count >= 12) { Pile oldest = Falling[0]; Release(oldest, oldest.Mass()); if (oldest.SmoothPath != null) oldest.SmoothPath.Dispose(); Falling.RemoveAt(0); }
                Falling.Add(piece);
            }
            TerrainVersion++;
        }
        private void FallDetached(double dt)
        {
            for (int k = Falling.Count - 1; k >= 0; k--)
            {
                Pile p = Falling[k]; p.FallAge += dt; p.FallVY += 380 * dt;
                Contact contact = MoveActor(p, p.FallVX * dt, p.FallVY * dt, false);
                if (contact.Valid && contact.Boundary && contact.Impact > (contact.NY == -1 ? 170 : 42))
                {
                    Shatter(p, contact); Falling.RemoveAt(k); continue;
                }
                if (contact.Support != null)
                {
                    DepositActor(p, contact.Support); Falling.RemoveAt(k); continue;
                }
                if (!p.Snowball && (p.CohesiveLaunch ? p.FallAge > .8 && p.FallVY > 70 : p.FallAge > .3))
                {
                    p.BreakClock += dt;
                    if (p.BreakClock > .075)
                    {
                        p.BreakClock = 0;
                        double mass = p.Mass() * .16;
                        for (int i = 0; i < 4; i++)
                        {
                            double x = p.X + random.NextDouble() * p.Width;
                            PriorityAdd(x, p.Y - p.At(x), p.FallVX + (random.NextDouble() - .5) * 50, p.FallVY * .65, 1.2 + random.NextDouble(), Math.Max(1, mass / 4));
                        }
                        for (int i = 0; i < p.Height.Length; i++) p.Height[i] *= .84;
                        p.BuildShape(); TerrainVersion++;
                    }
                }
                if (p.FallAge > (p.Snowball ? 4 : p.CohesiveLaunch ? 2.2 : 1.2))
                {
                    for (int i = 0; i < 12; i++)
                        {
                            double x = p.X + random.NextDouble() * p.Width;
                            PriorityAdd(x, p.Y - p.At(x), (random.NextDouble() - .5) * 65, p.FallVY * .5, 1.2 + random.NextDouble(), Math.Max(1, p.Mass() / 12));
                        }
                    if (p.SmoothPath != null) p.SmoothPath.Dispose(); Falling.RemoveAt(k); TerrainVersion++;
                }
            }
            MergeAirborne();
        }
        internal void Step(double dt)
        {
            Time += dt; credit = Math.Min(credit + Rate * dt, 4);
            MoveSliding(dt);
            FallDetached(dt);
            while (credit >= 1)
            {
                credit--;
                double x = Bounds.Left + random.NextDouble() * Bounds.Width;
                Add(x, Bounds.Top - 5, random.NextDouble() * 12 - 6, 20 + random.NextDouble() * 24, .9 + random.NextDouble() * 1.65);
            }
            double wind = Math.Sin(Time * .19) * 14 + Math.Sin(Time * .047) * 10;
            for (int i = Count - 1; i >= 0; i--)
            {
                Flake f = Flakes[i];
                double ox = f.X, oy = f.Y;
                f.Age += dt;
                double target = wind + Math.Sin(Time * 1.25 + f.Phase) * 13;
                if (f.Ballistic) { f.VX *= Math.Max(0, 1 - dt * .12); f.VY = Math.Min(360, f.VY + 340 * dt); }
                else
                {
                    double factor = Config.Extreme ? 2 : 1;
                    f.VX += (target - f.VX) * dt * .8;
                    f.VY = Math.Min((39 + f.Radius * 23) * factor, f.VY + (19 + f.Radius * 9) * factor * dt);
                }
                double dx = f.X - Cursor.X, dy = f.Y - Cursor.Y;
                double distance = dx * dx + dy * dy;
                if (Config.Interactive && Config.CursorInteraction && distance < 1600 && distance > 16)
                {
                    double push = (1 - distance / 1600) * dt;
                    f.VX += Settings.Clamp(CursorSpeed.X, -400, 400) * push * .4;
                    f.VY += Settings.Clamp(CursorSpeed.Y, -250, 250) * push * .15;
                }
                f.X += f.VX * dt; f.Y += f.VY * dt;
                bool remove = f.Y > Bounds.Bottom + 12 || f.Age > 75;
                if (f.Sublimating) { f.Radius -= dt * .6; if (f.Radius < .4 || f.Age > 2.4) remove = true; }
                Pile hit = null; double hitT = 2;
                if (!remove)
                {
                    // Swept collision against height fields: even at 12 fps no tunnelling.
                    foreach (Pile p in roofBins[Bin(f.X)])
                    {
                        if (f.X < p.X || f.X >= p.X + p.Width) continue;
                        int cell = Math.Max(0, Math.Min(p.Height.Length - 1, (int)((f.X - p.X) / p.Cell)));
                        if (!p.Exposed[cell]) continue;
                        double top = p.Y - p.Height[cell] - f.Radius;
                        if (oy <= top + 1.0 && f.Y >= top && f.Y > oy)
                        {
                            double t = (top - oy) / (f.Y - oy);
                            if (t < hitT) { hitT = t; hit = p; }
                        }
                    }
                    if (Sliding != null && f.X >= Sliding.X && f.X < Sliding.X + Sliding.Width)
                    {
                        double top = Sliding.Y - Sliding.At(f.X) - f.Radius;
                        if (oy <= top + 1 && f.Y >= top && f.Y > oy)
                        {
                            double t = (top - oy) / (f.Y - oy);
                            if (t < hitT) { hitT = t; hit = Sliding; }
                        }
                    }
                    if (hit != null)
                    {
                        if (!f.Sublimating) { hit.Deposit(f.X, f.Area); ResolveCapacity(hit, f.X); hit.BuildShape(); }
                        remove = true;
                    }
                    else
                    {
                        Pile direct = ResolveFlakeMotion(ref f, ox, oy);
                        if (direct != null) { if (!f.Sublimating) { direct.Deposit(f.X, f.Area); ResolveCapacity(direct, f.X); direct.BuildShape(); } remove = true; }
                    }
                }
                if (remove) Flakes[i] = Flakes[--Count]; else Flakes[i] = f;
            }
            Interact(dt); Advect(dt); StepExcavator(dt); StepDecorations(dt);
            MovingSnow = MovingSnow || HeldBall != null || Sliding != null || Gesture != 0;
            terrainClock += dt;
            double terrainInterval = MovingSnow ? 1.0 / 60 : .125;
            if (terrainClock + 1e-8 >= terrainInterval) { Settle(terrainInterval); terrainClock = Math.Max(0, terrainClock - terrainInterval); }
        }
        internal void Interact(double dt)
        {
            int gesture = Config.Interactive && Config.GestureInteraction ? Gesture : 0;
            bool ctrlPressed = gesture == 2 && previousGesture != 2;
            bool releasedBall = HeldBall != null && gesture != 1;
            if (releasedBall) ReleaseBall(gesture == 2);
            previousGesture = gesture;
            if (!Config.Interactive || Time < pickupAfter) return;
            if (HeldBall != null)
            {
                HeldBall.FallVX = HeldBall.FallVX * .65 + CursorSpeed.X * .35;
                HeldBall.FallVY = HeldBall.FallVY * .65 + CursorSpeed.Y * .35;
                BallShape();
            }
            if (releasedBall && gesture == 2) return;
            if (Sliding != null) { StampSliding(); }
            if (gesture == 0 && (!Config.CursorInteraction || CursorSpeed.Length < 55 || Math.Abs(CursorSpeed.X) < 35)) return;
            foreach (Pile p in Piles)
            {
                if (SupportDrag && !p.Ground) continue;
                double radius = gesture == 1 ? 100 : gesture == 2 ? 62 : 27;
                if (Cursor.X < p.X - radius || Cursor.X > p.X + p.Width + radius || Cursor.Y > p.Y + 12 || Cursor.Y < p.Y - p.MaxHeight - 20) continue;
                if (gesture == 1) { GatherBall(p, dt); continue; }
                if (gesture == 2)
                {
                    if (ctrlPressed || Time >= nextCtrlBurst)
                    {
                        int center = (int)((Cursor.X - p.X) / p.Cell), span = Math.Max(2, (int)(38 / p.Cell));
                        double sideways = 105 + Math.Min(130, Math.Abs(CursorSpeed.X) * .3);
                        double up = Config.Extreme ? -330 : -260;
                        LiftSection(p, center - span, center - 1, .82, -sideways, up - random.NextDouble() * 25);
                        LiftSection(p, center, center + span, .82, sideways, up + random.NextDouble() * 25);
                        nextCtrlBurst = Time + .65;
                    }
                    continue;
                }
                BrushSurface(p, dt);
            }
        }
        private void BrushSurface(Pile p, double dt)
        {
            // A small packed section follows the sweep with a visible local depression.
            // Mere proximity and a stationary pointer cannot lift a whole drift.
            double direction = Math.Sign(CursorSpeed.X), distance = Math.Min(24, Math.Abs(CursorSpeed.X) * dt * .65);
            double reach = 23, lifted = 0, moment = 0;
            for (int i = Math.Max(0, (int)((Cursor.X - reach - p.X) / p.Cell)); i < p.Height.Length; i++)
            {
                double x = p.X + (i + .5) * p.Cell;
                if (x > Cursor.X + reach) break;
                if (!p.Exposed[i] || p.Height[i] < .1 || Math.Abs(Cursor.Y - (p.Y - p.Height[i])) > 14) continue;
                double weight = 1 - Math.Abs(x - Cursor.X) / reach;
                // Deeper snow is heavier: a sweep shears its surface, not its full body.
                double inertia = 1 / (1 + Math.Pow(Math.Max(0, p.Height[i] - 14) / 20, 1.35));
                double take = Math.Min(p.Height[i] * .18, Math.Min(4.5, Math.Abs(CursorSpeed.X) * dt * .65 * inertia) * weight);
                p.Height[i] -= take; lifted += take * p.Cell; moment += x * take * p.Cell;
            }
            if (lifted < .001) return;
            double target = moment / lifted + direction * (25 + distance);
            if (!p.Ground && (target < p.X + 2 || target > p.X + p.Width - 2))
                Arc(direction < 0 ? p.X - 3 : p.X + p.Width + 3, p.Y - 3, direction, lifted);
            else p.Deposit(Settings.Clamp(target, p.X + p.Cell, p.X + p.Width - p.Cell), lifted);
            ResolveCapacity(p, target); p.BuildShape(); TerrainVersion++;
        }
        internal void Advect(double dt)
        {
            MovingSnow = false;
            foreach (Pile p in Piles)
            {
                bool hasMotion = false;
                for (int i = 0; i < p.Flow.Length; i++) if (Math.Abs(p.Flow[i]) > 2 && p.Height[i] > .02) { hasMotion = true; break; }
                if (!hasMotion) { Array.Clear(p.Flow, 0, p.Flow.Length); continue; }
                Array.Copy(p.Height, p.NextHeight, p.Height.Length); Array.Clear(p.NextMomentum, 0, p.NextMomentum.Length);
                int cursor = 0;
                while (cursor < p.Height.Length)
                {
                    while (cursor < p.Height.Length && (Math.Abs(p.Flow[cursor]) <= 2 || p.Height[cursor] <= .02)) cursor++;
                    if (cursor >= p.Height.Length) break;
                    int start = cursor; double mass = 0, momentum = 0;
                    while (cursor < p.Height.Length && Math.Abs(p.Flow[cursor]) > 2 && p.Height[cursor] > .02)
                    {
                        mass += p.Height[cursor]; momentum += p.Height[cursor] * p.Flow[cursor]; cursor++;
                    }
                    int end = cursor - 1; double velocity = momentum / Math.Max(.001, mass);
                    if (Time > p.ContactUntil)
                        velocity = Math.Sign(velocity) * Math.Max(0, Math.Abs(velocity) - 3000 * dt);
                    if (Math.Abs(velocity) < 9) velocity = 0;
                    double shift = velocity * dt / p.Cell;
                    for (int i = start; i <= end; i++)
                    {
                        double h = p.Height[i]; p.NextHeight[i] -= h;
                        double position = p.Ground ? Settings.Clamp(i + shift, 0, p.Height.Length - 1) : i + shift;
                        int left = (int)Math.Floor(position), right = left + 1; double fraction = position - left;
                        MovePackedPart(p, left, h * (1 - fraction), velocity); MovePackedPart(p, right, h * fraction, velocity);
                    }
                    if (velocity != 0) MovingSnow = true;
                }
                double[] swap = p.Height; p.Height = p.NextHeight; p.NextHeight = swap;
                for (int i = 0; i < p.Flow.Length; i++) p.Flow[i] = p.Height[i] > .02 ? p.NextMomentum[i] / p.Height[i] : 0;
            }
        }
        private static void MovePackedPart(Pile p, int index, double height, double velocity)
        {
            if (height <= 0) return;
            if (p.Ground) index = Math.Max(0, Math.Min(p.Height.Length - 1, index));
            if (index < 0) p.LeftSpill += height * p.Cell;
            else if (index >= p.Height.Length) p.RightSpill += height * p.Cell;
            else { p.NextHeight[index] += height; p.NextMomentum[index] += height * velocity; }
        }
        internal void Settle(double dt)
        {
            foreach (Pile p in Piles)
            {
                int n = p.Height.Length;
                double repose = p.Cell * 1.1 + 2.5;
                for (int pass = 0; pass < 2; pass++)
                    for (int k = 0; k < n - 1; k++)
                    {
                        int i = pass == 0 ? k : n - 2 - k;
                        double difference = p.Height[i] - p.Height[i + 1];
                        if (Math.Abs(difference) > repose)
                        {
                            double move = (Math.Abs(difference) - repose) * Math.Min(.12, dt * 1.8);
                            if (difference > 0) { p.Height[i] -= move; p.Height[i + 1] += move; }
                            else { p.Height[i] += move; p.Height[i + 1] -= move; }
                        }
                    }
                for (int i = 0; i < n; i++)
                {
                    double x = p.X + (i + .5) * p.Cell;
                    double cap = p.MaxHeight;
                    if (!p.Ground)
                    {
                        double distance = Math.Min((i + .5) * p.Cell, p.Width - (i + .5) * p.Cell);
                        cap *= Math.Sin(Math.Min(1, distance / Math.Min(9, p.Width * .4)) * Math.PI * .5);
                    }
                    double h = Math.Max(0, p.Height[i]);
                    // Sublimation rises with pile depth, establishing a finite equilibrium.
                    h -= dt * (.005 * h + Math.Max(0, h - cap * .94) * .35);
                    p.Height[i] = Math.Max(0, h);
                }
                if (!p.Ground)
                {
                    for (int edge = 0; edge < 2; edge++)
                    {
                        int i = edge == 0 ? 0 : n - 1;
                        double spill = Math.Max(0, p.Height[i] - repose) * Math.Min(.8, dt * 4);
                        p.Height[i] -= spill;
                        if (edge == 0) p.LeftSpill = Math.Min(100, p.LeftSpill + spill * p.Cell);
                        else p.RightSpill = Math.Min(100, p.RightSpill + spill * p.Cell);
                        double volume = edge == 0 ? p.LeftSpill : p.RightSpill;
                        if (volume >= 18)
                        {
                            double x = edge == 0 ? p.X - 3 : p.X + p.Width + 3;
                            Arc(x, p.Y - p.Height[i], edge == 0 ? -1 : 1, volume);
                            if (edge == 0) p.LeftSpill = 0; else p.RightSpill = 0;
                        }
                    }
                }
                ResolveCapacity(p, p.X + p.Width * .5); p.BuildShape();
            }
            TerrainVersion++;
        }
    }
}
