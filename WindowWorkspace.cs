using System.Windows;

namespace WarehousePro.Wpf;

public static class WarehouseEvents
{
    public static event Action? DataChanged;

    public static void RaiseDataChanged()
    {
        var app = Application.Current;
        if (app?.Dispatcher == null || app.Dispatcher.CheckAccess())
        {
            DataChanged?.Invoke();
            return;
        }

        app.Dispatcher.Invoke(() => DataChanged?.Invoke());
    }
}

public static class WindowWorkspace
{
    public static void ShowSingleton<T>(Func<T> create) where T : Window
    {
        if (TryActivate<T>())
            return;

        var window = create();
        PlaceNearMain(window);
        window.Show();
        Activate(window);
    }

    public static void ShowOperation(string operationType)
    {
        var type = OperationTypes.Normalize(operationType);
        if (Application.Current != null)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is InventoryOperationWindow existing && existing.OperationType == type)
                {
                    Activate(existing);
                    return;
                }
            }
        }

        var created = new InventoryOperationWindow(type);
        PlaceNearMain(created);
        created.Show();
        Activate(created);
    }

    private static bool TryActivate<T>() where T : Window
    {
        if (Application.Current == null)
            return false;

        foreach (Window window in Application.Current.Windows)
        {
            if (window is T existing)
            {
                Activate(existing);
                return true;
            }
        }

        return false;
    }

    private static void Activate(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.Show();
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    private static void PlaceNearMain(Window window)
    {
        if (Application.Current?.MainWindow is not { IsLoaded: true } main)
            return;

        var count = Math.Min(Application.Current.Windows.Count, 8);
        var offset = 32 * count;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = main.Left + offset;
        window.Top = main.Top + offset;
    }
}
