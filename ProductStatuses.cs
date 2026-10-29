namespace WarehousePro.Wpf;

public static class ProductStatuses
{
    public const string InStock = "В наличии";
    public const string OnOrder = "Под заказ";
    public const string Discontinued = "Снят с продажи";
    public const string Defective = "Брак";

    public static readonly string[] Stored =
    {
        InStock,
        OnOrder,
        Discontinued,
        Defective
    };

    public static string Normalize(string? value)
    {
        return value?.Trim() switch
        {
            "In stock" or InStock => InStock,
            "Made to order" or "On order" or OnOrder => OnOrder,
            "Discontinued" or Discontinued => Discontinued,
            "Defective" or Defective => Defective,
            _ => InStock
        };
    }

    public static string Localize(string? value, bool english)
    {
        if (!english)
            return Normalize(value);

        return Normalize(value) switch
        {
            OnOrder => "Made to order",
            Discontinued => "Discontinued",
            Defective => "Defective",
            _ => "In stock"
        };
    }
}
