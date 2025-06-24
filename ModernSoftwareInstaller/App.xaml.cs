
using System.Windows;
using ModernWpf;

namespace ModernSoftwareInstaller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ThemeManager.Current.AccentColor = (System.Windows.Media.Color)FindResource("AppAccentColor");
            base.OnStartup(e);
        }
    }
}
