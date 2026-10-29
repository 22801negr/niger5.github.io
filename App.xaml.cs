using System.Windows;

namespace WarehousePro.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppSettings.ApplyThemeResources();
        base.OnStartup(e);
    }
}
