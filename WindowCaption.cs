using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public static class WindowCaption
{
    public static void Minimize(Window window)
    {
        window.WindowState = WindowState.Minimized;
    }

    public static void ToggleMaximize(Window window, Button? maximizeButton = null)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        UpdateMaximizeButton(window, maximizeButton);
    }

    public static void UpdateMaximizeButton(Window window, Button? maximizeButton)
    {
        if (maximizeButton == null)
            return;

        var isEnglish = AppSettings.Language == "en";
        var maximized = window.WindowState == WindowState.Maximized;
        maximizeButton.Content = maximized ? "❐" : "☐";
        maximizeButton.ToolTip = maximized
            ? (isEnglish ? "Restore" : "Свернуть в окно")
            : (isEnglish ? "Maximize" : "На весь экран");
    }

    public static void DragOrMaximize(Window window, MouseButtonEventArgs e, Button? maximizeButton = null)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize(window, maximizeButton);
            return;
        }

        if (window.WindowState == WindowState.Maximized)
            return;

        window.DragMove();
    }
}
