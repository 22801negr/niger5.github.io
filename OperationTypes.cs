namespace WarehousePro.Wpf;

public static class OperationTypes
{
    public const string Incoming = "Приход";
    public const string Expense = "Расход";
    public const string Adjustment = "Корректировка";
    public const string Return = "Возврат";
    public const string ReturnFromCustomer = "Возврат от клиента";
    public const string ReturnToSupplier = "Возврат поставщику";
    public const string IncreaseStock = "Увеличить остаток";
    public const string DecreaseStock = "Уменьшить остаток";

    public static string Normalize(string? type)
    {
        return type?.Trim() switch
        {
            "Incoming" or Incoming => Incoming,
            "Expense" or "Outgoing" or Expense => Expense,
            "Adjustment" or Adjustment => Adjustment,
            "Return" or Return => Return,
            "Return from customer" or ReturnFromCustomer => ReturnFromCustomer,
            "Return to supplier" or ReturnToSupplier => ReturnToSupplier,
            "Increase stock" or IncreaseStock => IncreaseStock,
            "Decrease stock" or DecreaseStock => DecreaseStock,
            _ => type?.Trim() ?? string.Empty
        };
    }

    public static bool IsStoredType(string type) =>
        type is Incoming or Expense or Adjustment or ReturnFromCustomer or ReturnToSupplier;

    public static bool DecreasesStock(string storedType) =>
        storedType is Expense or ReturnToSupplier;

    public static int StockDelta(string storedType, int quantity) =>
        DecreasesStock(storedType) ? -quantity : quantity;

    public static string Localize(string? type, bool english)
    {
        if (!english)
            return type?.Trim() ?? string.Empty;

        return Normalize(type) switch
        {
            Incoming => "Incoming",
            Expense => "Expense",
            Adjustment => "Adjustment",
            Return => "Return",
            ReturnFromCustomer => "Return from customer",
            ReturnToSupplier => "Return to supplier",
            IncreaseStock => "Increase stock",
            DecreaseStock => "Decrease stock",
            _ => type?.Trim() ?? string.Empty
        };
    }
}
