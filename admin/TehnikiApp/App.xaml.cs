using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace TehnikiApp
{
    public partial class App : Application
    {
        public static bool IsLightTheme { get; set; } = false;

        public static void SetTheme(bool lightTheme)
        {
            IsLightTheme = lightTheme;
            var resources = Current.Resources;

            if (lightTheme)
            {
                resources["ThemeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4F8FC"));
                resources["ThemeHeaderBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1ECF7"));
                resources["ThemeCardBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                resources["ThemePanelBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF3FC"));
                resources["ThemeDarkBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                resources["ThemeTextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F203D"));
                resources["ThemeTextSecondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B6584"));
                resources["ThemeTextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#778CA3"));
                resources["ThemeBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1E2F3"));
                resources["ThemeBorderLight"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCE6F1"));
                resources["ThemeAccent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                resources["ThemeTextBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF2FA"));
                resources["ThemeComboBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                
                resources["ThemeLoginGradient"] = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#F4F8FC"),
                    (Color)ColorConverter.ConvertFromString("#B0CBE9"),
                    new Point(0.5, 0),
                    new Point(0.5, 1)
                );
            }
            else
            {
                resources["ThemeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0E1A"));
                resources["ThemeHeaderBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#050A18"));
                resources["ThemeCardBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
                resources["ThemePanelBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                resources["ThemeDarkBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                resources["ThemeTextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                resources["ThemeTextSecondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                resources["ThemeTextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                resources["ThemeBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                resources["ThemeBorderLight"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                resources["ThemeAccent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                resources["ThemeTextBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                resources["ThemeComboBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));

                resources["ThemeLoginGradient"] = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#0A0E1A"),
                    (Color)ColorConverter.ConvertFromString("#2563EB"),
                    new Point(0.5, 0),
                    new Point(0.5, 1)
                );
            }
        }
    }
}
