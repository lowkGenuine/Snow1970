using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Snow1970
{
    // Snowflake textures are uploaded once. Per-frame work changes GPU transforms,
    // rather than copying a desktop-sized transparent bitmap across the CPU/GPU bus.
    internal sealed class GpuSnow : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] private struct ColorF { internal float R, G, B, A; }
        [StructLayout(LayoutKind.Sequential)] private struct Ellipse { internal float X, Y, RX, RY; }
        [StructLayout(LayoutKind.Sequential)] private struct RectF { internal float L, T, R, B; }
        [DllImport("d3d11.dll")] private static extern int D3D11CreateDevice(IntPtr adapter, int driver, IntPtr software, uint flags, IntPtr levels, uint count, uint sdk, out IntPtr device, out uint level, out IntPtr context);
        [DllImport("d2d1.dll")] private static extern int D2D1CreateDevice(IntPtr dxgi, IntPtr properties, out IntPtr device);
        [DllImport("dcomp.dll")] private static extern int DCompositionCreateDevice2(IntPtr renderingDevice, ref Guid iid, out IntPtr device);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Simple(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Create(IntPtr self, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Target(IntPtr self, IntPtr hwnd, int above, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Surface(IntPtr self, uint w, uint h, int format, int alpha, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int ObjectValue(IntPtr self, IntPtr value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int FloatValue(IntPtr self, float value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int RectangleValue(IntPtr self, ref RectF rect);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int AddVisual(IntPtr self, IntPtr child, int above, IntPtr reference);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int BeginSurface(IntPtr self, IntPtr rect, ref Guid iid, out IntPtr context, out Native.POINT offset);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void Clear(IntPtr self, ref ColorF color);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Brush(IntPtr self, ref ColorF color, IntPtr properties, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void FillEllipse(IntPtr self, ref Ellipse shape, IntPtr brush);
        private IntPtr d3d, dxgi, d2d, device, target, root;
        private IntPtr[] textures = new IntPtr[3];
        private IntPtr[] sprites;
        private int[] types;
        private int used;
        private Simple commit;
        private Create createVisual;
        private ObjectValue setContent;
        private FloatValue setX, setY;
        private AddVisual add;
        internal int PresentedFrames;
        internal static string LastError;
        private string stage;
        private static T Method<T>(IntPtr obj, int slot) where T : class
        {
            IntPtr address = Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), slot * IntPtr.Size);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(address, typeof(T));
        }
        private static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
        private static void Release(ref IntPtr value) { if (value != IntPtr.Zero) { Marshal.Release(value); value = IntPtr.Zero; } }
        internal static GpuSnow TryCreate(IntPtr window, int width, int height, int capacity)
        {
            GpuSnow result = new GpuSnow();
            try { result.Initialize(window, width, height, capacity); LastError = null; return result; }
            catch (Exception e) { LastError = result.stage + ": " + e.ToString(); result.Dispose(); return null; }
        }
        private void Initialize(IntPtr window, int width, int height, int capacity)
        {
            IntPtr context; uint level;
            // Hardware only: do not silently replace a weak GPU with a busy software renderer.
            stage = "D3D device";
            Check(D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0x20, IntPtr.Zero, 0, 7, out d3d, out level, out context));
            Release(ref context);
            Guid dxgiId = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
            Check(Marshal.QueryInterface(d3d, ref dxgiId, out dxgi));
            stage = "D2D device"; Check(D2D1CreateDevice(dxgi, IntPtr.Zero, out d2d));
            Guid compId = new Guid("c37ea93a-e7aa-450d-b16f-9746cb0407f3");
            stage = "Composition device"; Check(DCompositionCreateDevice2(d2d, ref compId, out device));
            commit = Method<Simple>(device, 3); createVisual = Method<Create>(device, 7);
            stage = "Root visual"; Check(createVisual(device, out root));
            // MSVC lays out these overloaded COM groups with animation first.
            setX = Method<FloatValue>(root, 4); setY = Method<FloatValue>(root, 6);
            setContent = Method<ObjectValue>(root, 15); add = Method<AddVisual>(root, 16);
            RectF clip = new RectF { R = width, B = height };
            stage = "Root clip"; Check(Method<RectangleValue>(root, 14)(root, ref clip));
            stage = "Dot textures"; for (int i = 0; i < textures.Length; i++) CreateDot(i);
            stage = "Composition target";
            if (window != IntPtr.Zero)
            {
                Check(Method<Target>(device, 6)(device, window, 1, out target));
                Check(Method<ObjectValue>(target, 3)(target, root));
            }
            sprites = new IntPtr[capacity]; types = new int[capacity];
            for (int i = 0; i < types.Length; i++) types[i] = -1;
            Check(commit(device));
        }
        private void CreateDot(int index)
        {
            Check(Method<Surface>(device, 8)(device, 8, 8, 87, 1, out textures[index]));
            Guid contextId = new Guid("e8f7fe7a-191c-466d-ad95-975678bda998");
            IntPtr context = IntPtr.Zero, brush = IntPtr.Zero; Native.POINT offset;
            Check(Method<BeginSurface>(textures[index], 3)(textures[index], IntPtr.Zero, ref contextId, out context, out offset));
            try
            {
                ColorF transparent = new ColorF(); Method<Clear>(context, 47)(context, ref transparent);
                ColorF color = new ColorF { R = index == 0 ? .76f : .96f, G = index == 0 ? .84f : .98f, B = 1, A = 1 };
                Check(Method<Brush>(context, 8)(context, ref color, IntPtr.Zero, out brush));
                float radius = index == 0 ? 1.05f : index == 1 ? 1.7f : 2.35f;
                Ellipse dot = new Ellipse { X = offset.X + 4, Y = offset.Y + 4, RX = radius, RY = radius };
                Method<FillEllipse>(context, 21)(context, ref dot, brush);
            }
            finally
            {
                Release(ref brush); Release(ref context);
                Check(Method<Simple>(textures[index], 4)(textures[index]));
            }
        }
        internal void Present(World world, double extrapolation)
        {
            int count = Math.Min(world.Count, sprites.Length);
            for (int i = 0; i < count; i++)
            {
                Flake f = world.Flakes[i];
                if (sprites[i] == IntPtr.Zero)
                {
                    Check(createVisual(device, out sprites[i])); Check(add(root, sprites[i], 1, IntPtr.Zero));
                }
                int type = f.Radius < 1.4 ? 0 : f.Radius < 2.1 ? 1 : 2;
                if (types[i] != type) { Check(setContent(sprites[i], textures[type])); types[i] = type; }
                double x = f.X + f.VX * extrapolation, y = f.Y + f.VY * extrapolation;
                if (!world.CanDrawFlake(x, y)) { x = world.Bounds.Left - 32; y = world.Bounds.Top - 32; }
                Check(setX(sprites[i], (float)(x - world.Bounds.X - 4)));
                Check(setY(sprites[i], (float)(y - world.Bounds.Y - 4)));
            }
            for (int i = count; i < used; i++) { Check(setContent(sprites[i], IntPtr.Zero)); types[i] = -1; }
            used = count; Check(commit(device)); PresentedFrames++;
        }
        public void Dispose()
        {
            if (target != IntPtr.Zero) Method<ObjectValue>(target, 3)(target, IntPtr.Zero);
            if (sprites != null) for (int i = 0; i < sprites.Length; i++) Release(ref sprites[i]);
            for (int i = 0; i < textures.Length; i++) Release(ref textures[i]);
            Release(ref root); Release(ref target); Release(ref device); Release(ref d2d); Release(ref dxgi); Release(ref d3d);
        }
    }

    internal sealed class DisplayClock : IDisposable
    {
        internal const int FrameMessage = 0x8000 + 74;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct Mode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] internal string Name;
            internal ushort Spec, Driver, Size, Extra; internal uint Fields;
            internal int X, Y; internal uint Orientation, Fixed;
            internal short Color, Duplex, YResolution, TT, Collate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] internal string Form;
            internal ushort LogPixels; internal uint Bits, Width, Height, Flags, Frequency, ICMMethod, ICMIntent, Media, Dither, Reserved1, Reserved2, PanWidth, PanHeight;
        }
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool EnumDisplaySettings(string device, int mode, ref Mode data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWaitableTimerEx(IntPtr attributes, string name, uint flags, uint access);
        [DllImport("kernel32.dll")] private static extern bool SetWaitableTimer(IntPtr timer, ref long due, int period, IntPtr routine, IntPtr argument, bool resume);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint timeout);
        [DllImport("kernel32.dll")] private static extern bool CancelWaitableTimer(IntPtr timer);
        private IntPtr timer, window;
        private Thread thread;
        private AutoResetEvent wake = new AutoResetEvent(false);
        private volatile bool running = true, enabled;
        private volatile int hertz = 60;
        private int pending;
        internal DisplayClock(IntPtr hwnd)
        {
            window = hwnd; timer = CreateWaitableTimerEx(IntPtr.Zero, null, 2, 0x1F0003);
            if (timer == IntPtr.Zero) timer = CreateWaitableTimerEx(IntPtr.Zero, null, 0, 0x1F0003);
            if (timer == IntPtr.Zero) throw new InvalidOperationException("Windows frame timer is unavailable");
            thread = new Thread(Run) { IsBackground = true, Name = "Snow display clock" }; thread.Start();
        }
        internal static int RefreshRate(Forms.Screen screen)
        {
            Mode mode = new Mode { Size = (ushort)Marshal.SizeOf(typeof(Mode)) };
            return EnumDisplaySettings(screen.DeviceName, -1, ref mode) && mode.Frequency >= 24 && mode.Frequency <= 500 ? (int)mode.Frequency : 60;
        }
        internal void Configure(int rate, bool on) { hertz = Math.Max(24, Math.Min(500, rate)); enabled = on; wake.Set(); }
        internal void Acknowledge() { Interlocked.Exchange(ref pending, 0); }
        private void Run()
        {
            Stopwatch clock = Stopwatch.StartNew(); double next = clock.Elapsed.TotalSeconds;
            try
            {
                while (running)
                {
                    if (!enabled) { wake.WaitOne(); next = clock.Elapsed.TotalSeconds; continue; }
                    double period = 1.0 / hertz, now = clock.Elapsed.TotalSeconds;
                    next += period; if (next < now - period) next = now + period;
                    long due = -(long)Math.Max(1000, (next - now) * 10000000);
                    if (!SetWaitableTimer(timer, ref due, 0, IntPtr.Zero, IntPtr.Zero, false)) break;
                    WaitForSingleObject(timer, 500);
                    if (!running || !enabled) continue;
                    if (Interlocked.Exchange(ref pending, 1) == 0) Native.PostMessage(window, FrameMessage, IntPtr.Zero, IntPtr.Zero);
                }
            }
            finally { Native.CloseHandle(timer); }
        }
        public void Dispose() { running = false; enabled = false; wake.Set(); CancelWaitableTimer(timer); }
    }
}
