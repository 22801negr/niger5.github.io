using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        WarehouseEvents.DataChanged += OnWarehouseDataChanged;
        ApplySettings();
    }

    public void ApplySettings()
    {
        AppSettings.ApplyThemeToWindow(this);
        ApplyLanguageStrings();
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";

        Title = isEnglish ? "Warehouse" : "Склад";
        pageTitle.Text = isEnglish ? "Warehouse Analytics" : "СкладАналитика";
        pageSubtitle.Text = isEnglish ? "Management dashboard" : "Панель управления";
        overviewTitle.Text = isEnglish ? "Warehouse overview" : "Обзор склада";
        overviewSubtitle.Text = isEnglish ? "Summary of goods, stock and movements" : "Сводка по товарам, остаткам и движениям";
        btnProducts.Content = isEnglish ? "Products and stock" : "Товары и остатки";
        btnIncoming.Content = isEnglish ? "Incoming goods" : "Приход товара";
        btnAdjustment.Content = isEnglish ? "Stock adjustment" : "Корректировка остатков";
        btnExpense.Content = isEnglish ? "Outgoing goods" : "Расход товара";
        btnReturns.Content = isEnglish ? "Returns" : "Возвраты";
        btnSettings.Content = isEnglish ? "Settings" : "Настройки";
        btnStatistics.Content = isEnglish ? "Statistics" : "Статистика";
        btnHistory.Content = isEnglish ? "Operation history" : "История операций";
        productsLabel.Text = isEnglish ? "Products" : "Товаров";
        colName.Header = isEnglish ? "Name" : "Название";
        colArticle.Header = isEnglish ? "Article" : "Артикул";
        colCategory.Header = isEnglish ? "Category" : "Категория";
        colKind.Header = isEnglish ? "Kind" : "Вид";
        colColor.Header = isEnglish ? "Color" : "Цвет";
        colManufacturer.Header = isEnglish ? "Manufacturer" : "Производитель";
        colMaterial.Header = isEnglish ? "Material" : "Материал";
        colUnit.Header = isEnglish ? "Unit" : "Ед.";
        colPurchasePrice.Header = isEnglish ? "Purchase price" : "Цена закупки";
        colSalePrice.Header = isEnglish ? "Sale price" : "Цена продажи";
        colQuantity.Header = isEnglish ? "Stock" : "Остаток";
        colMinimum.Header = isEnglish ? "Minimum" : "Минимум";
        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";
        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);
        Refresh_ClickText();
    }

    private void Refresh_ClickText()
    {
        var refreshText = AppSettings.Language == "en" ? "Refresh" : "Обновить";
        if (this.FindName("RefreshButton") is Button refreshButton)
        {
            refreshButton.Content = refreshText;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProducts();
    }

    private void LoadProducts()
    {
        try
        {
            var connected = DatabaseHelper.TestConnection();
            statusText.Text = connected
                ? (AppSettings.Language == "en"
                    ? $"Database: connected ({DatabaseHelper.ConnectionLabel})"
                    : $"База данных: подключена ({DatabaseHelper.ConnectionLabel})")
                : (AppSettings.Language == "en"
                    ? $"Database: not connected. {DatabaseHelper.LastError}"
                    : $"База данных: не подключена. {DatabaseHelper.LastError}");
            statusText.Foreground = connected ? System.Windows.Media.Brushes.ForestGreen : System.Windows.Media.Brushes.IndianRed;

            var table = DatabaseHelper.GetProductsTable(txtSearch.Text);
            dgProducts.ItemsSource = table.DefaultView;
            countLabel.Text = table.Rows.Count.ToString();
        }
        catch (Exception ex)
        {
            statusText.Text = AppSettings.Language == "en" ? $"Error: {ex.Message}" : $"Ошибка: {ex.Message}";
            statusText.Foreground = System.Windows.Media.Brushes.IndianRed;
            MessageBox.Show(ex.Message, AppSettings.Language == "en" ? "Connection error" : "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadProducts();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadProducts();
    }

    private void OnWarehouseDataChanged()
    {
        LoadProducts();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        WarehouseEvents.DataChanged -= OnWarehouseDataChanged;
    }

    private void BtnProducts_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowSingleton(() => new ProductsWindow());
    }

    private void BtnIncoming_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowOperation(OperationTypes.Incoming);
    }

    private void BtnAdjustment_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowOperation(OperationTypes.Adjustment);
    }

    private void BtnExpense_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowOperation(OperationTypes.Expense);
    }

    private void BtnReturns_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowOperation(OperationTypes.Return);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowSingleton(() => new SettingsWindow());
    }

    private void BtnStatistics_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowSingleton(() => new StatisticsWindow());
    }

    private void BtnHistory_Click(object sender, RoutedEventArgs e)
    {
        WindowWorkspace.ShowSingleton(() => new HistoryWindow());
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        WindowCaption.DragOrMaximize(this, e, MaximizeButton);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowCaption.Minimize(this);
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowCaption.ToggleMaximize(this, MaximizeButton);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
