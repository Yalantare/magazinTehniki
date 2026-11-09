using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace TehnikiApp
{
    public partial class App : Application
    {
        public static bool IsLightTheme { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            var culture = new System.Globalization.CultureInfo("ru-RU");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage("ru-RU")));

            base.OnStartup(e);
        }

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
                resources["ThemeBorderLight"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                resources["ThemeAccent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                resources["ThemeTextBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF2FA"));
                resources["ThemeComboBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                
                resources["ThemeSuccessBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
                resources["ThemeSuccessText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
                resources["ThemeDangerBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                resources["ThemeDangerText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
                resources["ThemeWarningBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                resources["ThemeWarningText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
                resources["ThemeInfoBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
                resources["ThemeInfoText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                resources["ThemePurpleBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E8FF"));
                resources["ThemePurpleText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9333EA"));
                resources["ThemeUserBadgeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                resources["ThemeUserBadgeText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

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
                resources["ThemeBorderLight"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                resources["ThemeAccent"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                resources["ThemeTextBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                resources["ThemeComboBoxBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));

                resources["ThemeSuccessBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B"));
                resources["ThemeSuccessText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
                resources["ThemeDangerBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                resources["ThemeDangerText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                resources["ThemeWarningBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F2D06"));
                resources["ThemeWarningText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                resources["ThemeInfoBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A"));
                resources["ThemeInfoText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                resources["ThemePurpleBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#311042"));
                resources["ThemePurpleText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A855F7"));
                resources["ThemeUserBadgeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                resources["ThemeUserBadgeText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

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
