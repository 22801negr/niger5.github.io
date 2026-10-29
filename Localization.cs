using System.Windows;
using System.Windows.Controls;

namespace WarehousePro.Wpf;

public static class Localization
{
    public static void ApplyToAllOpenWindows()
    {
        if (Application.Current == null)
            return;

        foreach (var window in Application.Current.Windows)
        {
            if (window is MainWindow mainWindow)
            {
                mainWindow.ApplyLanguageStrings();
            }
            else if (window is SettingsWindow settingsWindow)
            {
                settingsWindow.ApplyLanguageStrings();
            }
            else if (window is ProductsWindow productsWindow)
            {
                productsWindow.ApplyLanguageStrings();
            }
            else if (window is InventoryOperationWindow operationWindow)
            {
                operationWindow.ApplyLanguageStrings();
            }
            else if (window is HistoryWindow historyWindow)
            {
                historyWindow.ApplyLanguageStrings();
            }
            else if (window is StatisticsWindow statisticsWindow)
            {
                statisticsWindow.ApplyLanguageStrings();
            }
        }
    }
}
