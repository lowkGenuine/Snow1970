using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Snow1970
{
    internal sealed class L10n : INotifyPropertyChanged
    {
        internal static readonly L10n Current = new L10n();
        public event PropertyChangedEventHandler PropertyChanged;
        private string language = "ru";
        public string Language { get { return language; } }
        internal static bool English { get { return Current.language == "en"; } }
        internal static void Use(string code)
        {
            string value = code == "en" ? "en" : "ru"; if (Current.language == value) return;
            Current.language = value;
            if (Current.PropertyChanged != null) Current.PropertyChanged(Current, new PropertyChangedEventArgs("Language"));
        }
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>
        {
            { "Снег", "Snow" },
            { "Снег · включить / выключить (Ctrl+Alt+S)", "Snow · on / off (Ctrl+Alt+S)" },
            { "Снег выключен", "Snow is off" }, { "Снег идёт", "Snow is falling" },
            { "Снег включён · рабочая область закрыта окнами", "Snow is on · desktop is covered by windows" },
            { "Настройки снега", "Snow settings" }, { "Настройки", "Settings" },
            { "Экскаватор", "Excavator" },
            { "Экскаватор · разбирать большие сугробы", "Excavator · clear deep snowbanks" },
            { "Вернуть в правый верхний угол", "Move to the top-right corner" },
            { "Вернуть панель", "Restore toolbar" }, { "Выход", "Quit" },
            { "Выход · Ctrl+Alt+Q", "Quit · Ctrl+Alt+Q" },
            { "Взаимодействие", "Interaction" }, { "Сдвигать снег курсором", "Push snow with the pointer" },
            { "Проведи вдоль кучки, чтобы сдвинуть снег.", "Sweep along a snowbank to move the snow." },
            { "Жесты Shift и Ctrl", "Shift and Ctrl gestures" },
            { "Ctrl — подбросить в стороны\nShift — слепить снежок; отпустить — бросить", "Ctrl — toss snow to the sides\nShift — pack a snowball; release to throw" },
            { "Снег и накопление", "Snow and accumulation" },
            { "Интенсивность", "Snowfall intensity" }, { "Высота сугробов", "Snowbank height" },
            { " / с", " / s" }, { "Снег на значках", "Snow on desktop icons" },
            { "Экстремальный снегопад · ×2", "Extreme snowfall · ×2" },
            { "Двойная скорость падения и двойной предел высоты сугробов", "Double falling speed and maximum snowbank height" },
            { "Снеговички и елочки", "Snowmen and fir trees" },
            { "При заполнении 80% участка · до двух фигурок на пять шагов сетки значков", "Appear at 80% coverage · up to two figures per five icon cells" },
            { "Нагрузка и экран", "Performance and display" },
            { "Авто", "Auto" }, { "Экономно", "Eco" }, { "Плавно", "Smooth" },
            { "Плавность по частоте экрана", "Match the display refresh rate" },
            { "Снег на всех мониторах", "Snow on every monitor" },
            { "Включать сразу при запуске", "Turn snow on at startup" },
            { "Убрать снег", "Clear snow" },
            { "Ctrl+Alt+S — снег     Ctrl+Alt+Q — выход", "Ctrl+Alt+S — snow     Ctrl+Alt+Q — quit" },
            { "Снег: включить / выключить", "Snow: on / off" }, { "Snow1970 — снег", "Snow1970 — snow" },
            { "Снег на значках выключен", "Snow on desktop icons is off" },
            { "Снег на значках включён", "Snow on desktop icons is on" },
            { "Чтение значков…", "Reading desktop icons…" },
            { "Геометрия значков временно недоступна", "Desktop icon geometry is temporarily unavailable" },
            { "Значки рабочего стола скрыты или недоступны", "Desktop icons are hidden or unavailable" },
            { "Нет доступа к геометрии значков", "Desktop icon geometry cannot be read" },
            { "Обновление значков отложено", "Desktop icon refresh postponed" },
            { "Частота экрана: {0} Гц · лимит {1} снежинок", "Display: {0} Hz · limit {1} snowflakes" },
            { "Русский", "Russian" },
            { "Не удалось сохранить настройки: ", "Could not save settings: " },
            { "Не удалось запустить Snow1970: ", "Could not start Snow1970: " },
            { "Snow1970 уже работает. Второй снежный слой не запущен.", "Snow1970 is already running. A second snow layer was not started." },
            { "Snow1970 уже запущен. Закройте прежнюю версию через её меню или Ctrl+Alt+Q, затем откройте новую.", "Snow1970 is already running. Quit the previous version from its menu or with Ctrl+Alt+Q, then start the new one." },
            { "Snow1970 остановлен. Подробности: Snow1970.log рядом с программой.", "Snow1970 stopped. See Snow1970.log next to the application for details." }
        };
        internal static string T(string text)
        {
            if (!English) return text;
            string value; if (words.TryGetValue(text, out value)) return value;
            if (text.StartsWith("Значков: ", StringComparison.Ordinal)) return "Desktop icons: " + text.Substring(9);
            return text;
        }
        internal static string Format(string text, params object[] args) { return string.Format(CultureInfo.CurrentCulture, T(text), args); }
        private sealed class TextConverter : IValueConverter
        {
            public object Convert(object value, Type target, object parameter, CultureInfo culture) { return T((string)parameter); }
            public object ConvertBack(object value, Type target, object parameter, CultureInfo culture) { return Binding.DoNothing; }
        }
        private static readonly TextConverter converter = new TextConverter();
        private sealed class LanguageConverter : IValueConverter
        {
            public object Convert(object value, Type target, object parameter, CultureInfo culture) { return (string)value == (string)parameter; }
            public object ConvertBack(object value, Type target, object parameter, CultureInfo culture) { return Binding.DoNothing; }
        }
        internal static void BindChoice(RadioButton button, string code)
        {
            BindingOperations.SetBinding(button, ToggleButton.IsCheckedProperty, new Binding("Language") { Source = Current, Converter = new LanguageConverter(), ConverterParameter = code, Mode = BindingMode.OneWay });
        }
        internal static void Bind(DependencyObject target, DependencyProperty property, string text)
        {
            BindingOperations.SetBinding(target, property, new Binding("Language") { Source = Current, Converter = converter, ConverterParameter = text });
        }
        internal static void BindTree(DependencyObject root)
        {
            DependencyProperty[] properties = { TextBlock.TextProperty, ContentControl.ContentProperty, HeaderedContentControl.HeaderProperty, HeaderedItemsControl.HeaderProperty, FrameworkElement.ToolTipProperty, AutomationProperties.NameProperty };
            foreach (DependencyProperty property in properties)
            {
                string text = root.ReadLocalValue(property) as string;
                if (text != null && words.ContainsKey(text)) Bind(root, property, text);
            }
            foreach (object child in LogicalTreeHelper.GetChildren(root)) { DependencyObject element = child as DependencyObject; if (element != null) BindTree(element); }
        }
        private sealed class UnitConverter : IMultiValueConverter
        {
            private string suffix;
            internal UnitConverter(string text) { suffix = text; }
            public object Convert(object[] values, Type target, object parameter, CultureInfo culture)
            { return values.Length > 0 && values[0] is double ? Math.Round((double)values[0]).ToString(culture) + T(suffix) : ""; }
            public object[] ConvertBack(object value, Type[] target, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        internal static void BindNumber(TextBlock number, Slider slider, string suffix)
        {
            MultiBinding b = new MultiBinding { Converter = new UnitConverter(suffix) };
            b.Bindings.Add(new Binding("Value") { Source = slider }); b.Bindings.Add(new Binding("Language") { Source = Current });
            BindingOperations.SetBinding(number, TextBlock.TextProperty, b);
        }
    }
}
