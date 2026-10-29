using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class ProductsWindow : Window
{
    private int? _selectedProductId;
    private bool _suppressSelection;

    public ProductsWindow()
    {
        InitializeComponent();
        Loaded += ProductsWindow_Loaded;
        Closed += ProductsWindow_Closed;
        WarehouseEvents.DataChanged += OnWarehouseDataChanged;
        ClearForm();
        ApplyLanguageStrings();
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";
        Title = isEnglish ? "Products and stock" : "Товары и остатки";
        titleText.Text = Title;

        if (FindName("RefreshButton") is Button refreshButton)
            refreshButton.Content = isEnglish ? "Refresh" : "Обновить";

        newButton.Content = isEnglish ? "New" : "Новый";
        labelName.Text = isEnglish ? "Name" : "Название";
        labelArticle.Text = isEnglish ? "Article" : "Артикул";
        labelCategory.Text = isEnglish ? "Category" : "Категория";
        labelKind.Text = isEnglish ? "Kind" : "Вид";
        labelColor.Text = isEnglish ? "Color" : "Цвет";
        labelManufacturer.Text = isEnglish ? "Manufacturer" : "Производитель";
        labelMaterial.Text = isEnglish ? "Material" : "Материал";
        labelUnit.Text = isEnglish ? "Unit" : "Ед. изм.";
        labelQuantity.Text = isEnglish ? "Stock" : "Остаток";
        labelMinimum.Text = isEnglish ? "Minimum" : "Минимум";
        labelPurchasePrice.Text = isEnglish ? "Purchase price" : "Цена закупки";
        labelSalePrice.Text = isEnglish ? "Sale price" : "Цена продажи";
        saveButton.Content = isEnglish ? "Save" : "Сохранить";
        deleteButton.Content = isEnglish ? "Delete" : "Удалить";
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
        txtQuantity.ToolTip = isEnglish
            ? "For a new product this is the opening stock. After that, change stock through operations."
            : "Для нового товара это начальный остаток. Дальше остаток меняется операциями.";

        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";
        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);
    }

    private void ProductsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProducts();
    }

    private void ProductsWindow_Closed(object? sender, EventArgs e)
    {
        WarehouseEvents.DataChanged -= OnWarehouseDataChanged;
    }

    private void OnWarehouseDataChanged()
    {
        LoadProducts();
    }

    private void LoadProducts()
    {
        var keepId = _selectedProductId;
        var table = DatabaseHelper.GetProductsTable(txtSearch.Text);
        _suppressSelection = true;
        dgProducts.ItemsSource = table.DefaultView;
        _suppressSelection = false;

        if (keepId.HasValue)
            SelectProduct(keepId.Value);
    }

    private void SelectProduct(int productId)
    {
        foreach (var item in dgProducts.Items)
        {
            if (item is DataRowView row && Convert.ToInt32(row["Id"]) == productId)
            {
                dgProducts.SelectedItem = row;
                dgProducts.ScrollIntoView(row);
                return;
            }
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadProducts();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadProducts();
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var isEnglish = AppSettings.Language == "en";
        try
        {
            var product = ReadForm();
            var savedId = _selectedProductId.HasValue
                ? _selectedProductId.Value
                : DatabaseHelper.AddProduct(product);

            if (_selectedProductId.HasValue)
                DatabaseHelper.UpdateProduct(savedId, product);

            _selectedProductId = savedId;
            WarehouseEvents.RaiseDataChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                isEnglish ? "Save error" : "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        var isEnglish = AppSettings.Language == "en";
        if (!_selectedProductId.HasValue)
        {
            MessageBox.Show(
                isEnglish ? "Select a product in the table first." : "Сначала выберите товар из таблицы.",
                isEnglish ? "Delete" : "Удаление",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            isEnglish
                ? "Delete this product and all of its operations?"
                : "Удалить этот товар и все его операции?",
            isEnglish ? "Delete" : "Удаление",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            DatabaseHelper.DeleteProduct(_selectedProductId.Value);
            ClearForm();
            WarehouseEvents.RaiseDataChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                isEnglish ? "Delete error" : "Ошибка удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void DgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection || dgProducts.SelectedItem is not DataRowView row)
            return;

        _selectedProductId = Convert.ToInt32(row["Id"]);
        txtName.Text = Cell(row, "Name");
        txtArticle.Text = Cell(row, "Article");
        txtCategory.Text = Cell(row, "Category");
        txtKind.Text = Cell(row, "Kind");
        txtColor.Text = Cell(row, "Color");
        txtManufacturer.Text = Cell(row, "Manufacturer");
        txtMaterial.Text = Cell(row, "Material");
        txtUnit.Text = string.IsNullOrWhiteSpace(Cell(row, "Unit")) ? "шт" : Cell(row, "Unit");
        txtQuantity.Text = Cell(row, "Quantity", "0");
        txtMinimumStock.Text = Cell(row, "MinimumStock", "0");
        txtPurchasePrice.Text = Cell(row, "PurchasePrice", "0");
        txtSalePrice.Text = Cell(row, "SalePrice", "0");
        txtQuantity.IsReadOnly = true;
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

    private void ClearForm()
    {
        _selectedProductId = null;
        _suppressSelection = true;
        dgProducts.SelectedItem = null;
        _suppressSelection = false;
        txtName.Clear();
        txtArticle.Clear();
        txtCategory.Clear();
        txtKind.Clear();
        txtColor.Clear();
        txtManufacturer.Clear();
        txtMaterial.Clear();
        txtUnit.Text = "шт";
        txtQuantity.Text = "0";
        txtMinimumStock.Text = "0";
        txtPurchasePrice.Text = "0";
        txtSalePrice.Text = "0";
        txtQuantity.IsReadOnly = false;
        txtName.Focus();
    }

    private ProductDetails ReadForm()
    {
        var isEnglish = AppSettings.Language == "en";
        if (!int.TryParse(txtQuantity.Text.Trim(), out var quantity))
            throw new Exception(isEnglish ? "Stock must be a number." : "Остаток должен быть числом.");
        if (!int.TryParse(txtMinimumStock.Text.Trim(), out var minimumStock))
            throw new Exception(isEnglish ? "Minimum must be a number." : "Минимум должен быть числом.");
        if (!decimal.TryParse(txtPurchasePrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var purchasePrice))
            throw new Exception(isEnglish ? "Purchase price must be a number." : "Цена закупки должна быть числом.");
        if (!decimal.TryParse(txtSalePrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var salePrice))
            throw new Exception(isEnglish ? "Sale price must be a number." : "Цена продажи должна быть числом.");

        return new ProductDetails
        {
            Name = txtName.Text.Trim(),
            Article = txtArticle.Text.Trim(),
            Category = txtCategory.Text.Trim(),
            Kind = txtKind.Text.Trim(),
            Color = txtColor.Text.Trim(),
            Manufacturer = txtManufacturer.Text.Trim(),
            Material = txtMaterial.Text.Trim(),
            Unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? "шт" : txtUnit.Text.Trim(),
            Quantity = quantity,
            MinimumStock = minimumStock,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice
        };
    }

    private static string Cell(DataRowView row, string column, string fallback = "")
    {
        if (!row.Row.Table.Columns.Contains(column) || row[column] is DBNull)
            return fallback;
        return row[column]?.ToString() ?? fallback;
    }
}
