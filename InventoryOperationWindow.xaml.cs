using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePro.Wpf;

public partial class InventoryOperationWindow : Window
{
    private readonly string _operationType;

    public string OperationType => _operationType;

    public InventoryOperationWindow(string operationType)
    {
        InitializeComponent();
        _operationType = OperationTypes.Normalize(operationType);
        Loaded += InventoryOperationWindow_Loaded;
        Closed += InventoryOperationWindow_Closed;
        WarehouseEvents.DataChanged += OnWarehouseDataChanged;
        ApplyLanguageStrings();
    }

    public void ApplyLanguageStrings()
    {
        var isEnglish = AppSettings.Language == "en";
        var operationName = GetLocalizedOperationName(_operationType, isEnglish);

        Title = isEnglish ? "Operation" : "Операция";
        titleText.Text = operationName;
        productLabel.Text = isEnglish ? "Product" : "Товар";
        quantityLabel.Text = isEnglish ? "Quantity" : "Количество";
        BtnSave.Content = isEnglish ? "Save" : "Сохранить";
        MinimizeButton.ToolTip = isEnglish ? "Minimize" : "Свернуть";
        CloseButton.ToolTip = isEnglish ? "Close" : "Закрыть";
        WindowCaption.UpdateMaximizeButton(this, MaximizeButton);

        if (_operationType == OperationTypes.Return)
        {
            var previous = OperationTypes.Normalize(cmbDirection.SelectedItem?.ToString());
            directionPanel.Visibility = Visibility.Visible;
            directionLabel.Text = isEnglish ? "Return type" : "Тип возврата";
            cmbDirection.ItemsSource = isEnglish
                ? new[] { "Return from customer", "Return to supplier" }
                : new[] { OperationTypes.ReturnFromCustomer, OperationTypes.ReturnToSupplier };
            cmbDirection.SelectedIndex = previous == OperationTypes.ReturnToSupplier ? 1 : 0;
        }
        else if (_operationType == OperationTypes.Adjustment)
        {
            var previous = OperationTypes.Normalize(cmbDirection.SelectedItem?.ToString());
            directionPanel.Visibility = Visibility.Visible;
            directionLabel.Text = isEnglish ? "Adjustment type" : "Тип корректировки";
            cmbDirection.ItemsSource = isEnglish
                ? new[] { "Increase stock", "Decrease stock" }
                : new[] { OperationTypes.IncreaseStock, OperationTypes.DecreaseStock };
            cmbDirection.SelectedIndex = previous == OperationTypes.DecreaseStock ? 1 : 0;
        }
        else
        {
            directionPanel.Visibility = Visibility.Collapsed;
        }
    }

    private static string GetLocalizedOperationName(string operationType, bool isEnglish)
    {
        return operationType switch
        {
            OperationTypes.Incoming => isEnglish ? "Incoming" : OperationTypes.Incoming,
            OperationTypes.Expense => isEnglish ? "Expense" : OperationTypes.Expense,
            OperationTypes.Adjustment => isEnglish ? "Adjustment" : OperationTypes.Adjustment,
            OperationTypes.Return => isEnglish ? "Return" : OperationTypes.Return,
            _ => isEnglish ? "Operation" : "Операция"
        };
    }

    private void InventoryOperationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProductsCombo();
    }

    private void InventoryOperationWindow_Closed(object? sender, EventArgs e)
    {
        WarehouseEvents.DataChanged -= OnWarehouseDataChanged;
    }

    private void OnWarehouseDataChanged()
    {
        LoadProductsCombo();
    }

    private void LoadProductsCombo()
    {
        var selectedId = cmbProducts.SelectedValue;
        var products = DatabaseHelper.GetProductsForCombo();
        cmbProducts.ItemsSource = products.DefaultView;
        if (selectedId != null)
        {
            cmbProducts.SelectedValue = selectedId;
        }
        else if (products.Rows.Count > 0 && cmbProducts.SelectedIndex < 0)
        {
            cmbProducts.SelectedIndex = 0;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var isEnglish = AppSettings.Language == "en";
        try
        {
            if (cmbProducts.SelectedItem is not DataRowView row)
            {
                MessageBox.Show(
                    isEnglish ? "Select a product." : "Выберите товар.",
                    isEnglish ? "Error" : "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out var quantity) || quantity <= 0)
            {
                throw new Exception(isEnglish
                    ? "Quantity must be a number greater than zero."
                    : "Количество должно быть больше нуля.");
            }

            var productId = Convert.ToInt32(row["Id"]);

            if (_operationType == OperationTypes.Adjustment)
            {
                var direction = OperationTypes.Normalize(cmbDirection.SelectedItem?.ToString());
                var delta = direction == OperationTypes.DecreaseStock ? -quantity : quantity;
                DatabaseHelper.AdjustStock(productId, delta);
            }
            else if (_operationType == OperationTypes.Return)
            {
                var returnType = OperationTypes.Normalize(cmbDirection.SelectedItem?.ToString());
                if (returnType != OperationTypes.ReturnFromCustomer && returnType != OperationTypes.ReturnToSupplier)
                    returnType = OperationTypes.ReturnFromCustomer;

                DatabaseHelper.AddOperation(productId, returnType, quantity);
            }
            else
            {
                DatabaseHelper.AddOperation(productId, _operationType, quantity);
            }

            WarehouseEvents.RaiseDataChanged();
            txtQuantity.Clear();
            MessageBox.Show(
                isEnglish ? "Operation saved." : "Операция сохранена.",
                isEnglish ? "Success" : "Успешно",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                isEnglish ? "Operation error" : "Ошибка операции",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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
