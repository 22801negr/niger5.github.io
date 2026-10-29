using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class StatisticsWindow : Window
{
    public StatisticsWindow()
    {
        InitializeComponent();
        Loaded += StatisticsWindow_Loaded;
        Closed += StatisticsWindow_Closed;
        WarehouseEvents.DataChanged += OnWarehouseDataChanged;
        ApplyLanguageStrings();
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";
        Title = isEnglish ? "Statistics" : "Статистика";
        titleText.Text = Title;
        RefreshButton.Content = isEnglish ? "Refresh" : "Обновить";
        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";

        kpiProductsLabel.Text = isEnglish ? "Products" : "Товаров";
        kpiStockLabel.Text = isEnglish ? "In stock" : "На складе";
        kpiLowStockLabel.Text = isEnglish ? "Below minimum" : "Ниже минимума";
        kpiOperationsLabel.Text = isEnglish ? "Operations" : "Операций";
        kpiIncomingLabel.Text = isEnglish ? "Incoming" : "Приход";
        kpiExpenseLabel.Text = isEnglish ? "Outgoing" : "Расход";

        lowStockTitle.Text = isEnglish ? "Low stock products" : "Товары с минимальным остатком";
        topProductsTitle.Text = isEnglish ? "Top products by movement" : "Топ товаров по движению";
        monthlyTitle.Text = isEnglish ? "Monthly movement" : "Движение по месяцам";

        colLowName.Header = isEnglish ? "Product" : "Товар";
        colLowQuantity.Header = isEnglish ? "Stock" : "Остаток";
        colLowMinimum.Header = isEnglish ? "Minimum" : "Минимум";
        colLowNeed.Header = isEnglish ? "To order" : "Заказать";

        colTopName.Header = isEnglish ? "Product" : "Товар";
        colTopStock.Header = isEnglish ? "Stock" : "Остаток";
        colTopIncoming.Header = isEnglish ? "In" : "Приход";
        colTopExpense.Header = isEnglish ? "Out" : "Расход";
        colTopNet.Header = isEnglish ? "Net" : "Сальдо";
        colTopOps.Header = isEnglish ? "Ops" : "Операций";

        colMonthPeriod.Header = isEnglish ? "Period" : "Период";
        colMonthIncoming.Header = isEnglish ? "Incoming" : "Приход";
        colMonthExpense.Header = isEnglish ? "Outgoing" : "Расход";
        colMonthAdjustment.Header = isEnglish ? "Adj." : "Корр.";
        colMonthNet.Header = isEnglish ? "Net" : "Сальдо";

        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);
    }

    private void StatisticsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadStatistics();
    }

    private void StatisticsWindow_Closed(object? sender, EventArgs e)
    {
        WarehouseEvents.DataChanged -= OnWarehouseDataChanged;
    }

    private void OnWarehouseDataChanged()
    {
        LoadStatistics();
    }

    private void LoadStatistics()
    {
        var isEnglish = AppSettings.Language == "en";
        try
        {
            var summary = DatabaseHelper.GetStatisticsSummary();
            kpiProductsValue.Text = summary.ProductCount.ToString();
            kpiStockValue.Text = summary.TotalQuantity.ToString();
            kpiLowStockValue.Text = summary.LowStockCount.ToString();
            kpiOperationsValue.Text = summary.OperationCount.ToString();
            kpiIncomingValue.Text = summary.IncomingQuantity.ToString();
            kpiExpenseValue.Text = summary.ExpenseQuantity.ToString();
            subtitleText.Text = isEnglish
                ? $"{summary.ProductCount} products · {summary.OperationCount} operations"
                : $"{summary.ProductCount} товаров · {summary.OperationCount} операций";

            dgLowStock.ItemsSource = DatabaseHelper.GetLowStockProducts().DefaultView;
            dgTopProducts.ItemsSource = DatabaseHelper.GetTopProducts().DefaultView;
            dgMonthly.ItemsSource = BuildMonthlyTable().DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                isEnglish ? "Statistics error" : "Ошибка статистики",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private DataTable BuildMonthlyTable()
    {
        var source = DatabaseHelper.GetMonthlyMovementData();
        var culture = AppSettings.Language == "en" ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("ru-RU");
        source.Columns.Add("Period", typeof(string));

        foreach (DataRow row in source.Rows)
        {
            var year = Convert.ToInt32(row["YearNumber"]);
            var month = Convert.ToInt32(row["MonthNumber"]);
            row["Period"] = new DateTime(year, month, 1).ToString("MMMM yyyy", culture);
        }

        return source;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadStatistics();
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
