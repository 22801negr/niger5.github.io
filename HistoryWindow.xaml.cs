using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
        Loaded += HistoryWindow_Loaded;
        Closed += HistoryWindow_Closed;
        WarehouseEvents.DataChanged += OnWarehouseDataChanged;
        ApplyLanguageStrings();
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";
        Title = isEnglish ? "Operation history" : "История операций";
        titleText.Text = Title;

        if (FindName("txtSearch") is TextBox searchBox)
            searchBox.ToolTip = isEnglish ? "Search" : "Поиск";

        if (FindName("BtnSearch") is Button searchButton)
            searchButton.Content = isEnglish ? "Search" : "Поиск";

        colProduct.Header = isEnglish ? "Product" : "Товар";
        colType.Header = isEnglish ? "Type" : "Тип";
        colQuantity.Header = isEnglish ? "Quantity" : "Количество";
        colDate.Header = isEnglish ? "Date" : "Дата";
        colOrder.Header = isEnglish ? "Order number" : "Номер заказа";

        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";
        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);

        if (IsLoaded)
            LoadHistory();
    }

    private void HistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    private void HistoryWindow_Closed(object? sender, EventArgs e)
    {
        WarehouseEvents.DataChanged -= OnWarehouseDataChanged;
    }

    private void OnWarehouseDataChanged()
    {
        LoadHistory();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadHistory();
    }

    private void LoadHistory()
    {
        var table = DatabaseHelper.GetOperationsTable(txtSearch.Text);
        if (!table.Columns.Contains("TypeDisplay"))
            table.Columns.Add("TypeDisplay", typeof(string));

        var english = AppSettings.Language == "en";
        foreach (DataRow row in table.Rows)
            row["TypeDisplay"] = OperationTypes.Localize(row["Type"]?.ToString(), english);

        dgHistory.ItemsSource = table.DefaultView;
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
