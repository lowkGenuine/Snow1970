using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snow1970
{
    internal sealed class ThemeDecision
    {
        internal bool Dark = true, Known;
        private bool candidate;
        private int confirmations;
        internal bool Observe(double luminance, bool immediate)
        {
            bool next = !Known ? luminance < .18 : Dark ? luminance < .23 : luminance < .13;
            if (!Known || immediate) { bool changed = !Known || next != Dark; Dark = next; Known = true; confirmations = 0; return changed; }
            if (next == Dark) { confirmations = 0; return false; }
            if (candidate != next) { candidate = next; confirmations = 1; return false; }
            if (++confirmations < 2) return false;
            Dark = next; confirmations = 0; return true;
        }
    }

    internal static class UiTheme
    {
        private static double Linear(byte value) { double n = value / 255.0; return n <= .04045 ? n / 12.92 : Math.Pow((n + .055) / 1.055, 2.4); }
        internal static double Luminance(Color color) { return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B); }
        internal static double Contrast(Color a, Color b) { double x = Luminance(a), y = Luminance(b); return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05); }
        internal static double Median(List<double> values) { values.Sort(); return values[values.Count / 2]; }
        internal static Brush Get(FrameworkElement element, string role)
        {
            return element.TryFindResource("Ui." + role) as Brush ?? Art.Text;
        }
        internal static void Bind(FrameworkElement element, DependencyProperty property, string role) { element.SetResourceReference(property, "Ui." + role); }
        internal static void Bind(FrameworkElementFactory element, DependencyProperty property, string role) { element.SetResourceReference(property, "Ui." + role); }
        internal static void BindTree(DependencyObject element)
        {
            FrameworkElement view = element as FrameworkElement;
            if (view != null)
            {
                DependencyProperty[] properties = { Control.ForegroundProperty, Control.BackgroundProperty, Control.BorderBrushProperty, TextBlock.ForegroundProperty, Border.BackgroundProperty, Border.BorderBrushProperty };
                foreach (DependencyProperty property in properties)
                {
                    SolidColorBrush brush = view.ReadLocalValue(property) as SolidColorBrush;
                    if (brush == null) continue;
                    string hex = brush.Color.ToString(); string role = null;
                    if (hex == "#FFEAF0F6") role = "Text";
                    if (hex == "#FFAAB5C3") role = "Muted";
                    if (hex == "#FF191C21") role = "Panel";
                    if (hex == "#FF36404C") role = "Line";
                    if (hex == "#FF3C4754" || hex == "#FF465262") role = "ControlLine";
                    if (role != null) Bind(view, property, role);
                }
            }
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            { DependencyObject node = child as DependencyObject; if (node != null) BindTree(node); }
        }
        internal static void Apply(FrameworkElement root, bool dark)
        {
            string[] keys = { "Panel", "Text", "Muted", "Line", "ControlLine", "Hover", "OnTrack", "OnKnob", "OnGlyph", "OffTrack", "OffKnob", "Selection", "SelectionText" };
            string[] colors = dark
                ? new string[] { "#191C21", "#EAF0F6", "#AAB5C3", "#36404C", "#465262", "#303843", "#EAF0F6", "#171A1E", "#F2F5F8", "#465262", "#EAF0F6", "#D7EBF7", "#191C21" }
                : new string[] { "#FAFBFC", "#171A1F", "#526071", "#CFD5DD", "#AEB8C6", "#E7ECF2", "#171A1F", "#FAFBFC", "#171A1F", "#DDE3EB", "#252A32", "#253448", "#FAFBFC" };
            for (int i = 0; i < keys.Length; i++) root.Resources["Ui." + keys[i]] = Art.Brush(colors[i]);
            root.Resources["Ui.Dark"] = dark;
            Style tips = new Style(typeof(ToolTip));
            tips.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("Ui.Panel")));
            tips.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("Ui.Text")));
            tips.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("Ui.Line")));
            root.Resources[typeof(ToolTip)] = tips;
            Style menus = new Style(typeof(MenuItem));
            menus.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("Ui.Text")));
            menus.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            menus.Setters.Add(new Setter(Control.TemplateProperty, System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='MenuItem'><Border x:Name='row' Background='{TemplateBinding Background}' CornerRadius='4' Padding='10,7'><ContentPresenter ContentSource='Header' RecognizesAccessKey='True'/></Border><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='row' Property='Background' Value='{DynamicResource Ui.Hover}'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.5'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));
            root.Resources[typeof(MenuItem)] = menus;
        }
    }

    internal static class BackgroundTone
    {
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr dc, int x, int y);
        // Nine point samples only. Pixels are reduced to one luminance number,
        // never saved, logged or sent anywhere.
        internal static bool TryRead(Rect rect, bool surround, out double luminance)
        {
            luminance = 0; IntPtr dc = GetDC(IntPtr.Zero); if (dc == IntPtr.Zero) return false;
            List<double> values = new List<double>();
            System.Drawing.Rectangle screen = System.Windows.Forms.SystemInformation.VirtualScreen;
            try
            {
                for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
                {
                    if (surround && row == 1 && col == 1) continue;
                    double px = surround ? (col == 0 ? rect.Left - 5 : col == 2 ? rect.Right + 5 : rect.X + rect.Width * .5) : rect.X + rect.Width * (.15 + col * .35);
                    double py = surround ? (row == 0 ? rect.Top - 5 : row == 2 ? rect.Bottom + 5 : rect.Y + rect.Height * .5) : rect.Y + rect.Height * (.15 + row * .35);
                    int x = (int)Math.Round(px), y = (int)Math.Round(py);
                    if (x < screen.Left || x >= screen.Right || y < screen.Top || y >= screen.Bottom) continue;
                    uint pixel = GetPixel(dc, x, y); if (pixel == 0xFFFFFFFF) continue;
                    values.Add(UiTheme.Luminance(Color.FromRgb((byte)pixel, (byte)(pixel >> 8), (byte)(pixel >> 16))));
                }
            }
            finally { ReleaseDC(IntPtr.Zero, dc); }
            if (values.Count < 3) return false; luminance = UiTheme.Median(values); return true;
        }
        internal static Rect ScreenRect(FrameworkElement element)
        {
            Point a = element.PointToScreen(new Point()), b = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
            return new Rect(a, b);
        }
    }
}
