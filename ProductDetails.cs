namespace WarehousePro.Wpf;

public sealed class ProductDetails
{
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт";
    public int Quantity { get; set; }
    public int MinimumStock { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string StorageCell { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public bool RequiresAssembly { get; set; }
    public string Status { get; set; } = ProductStatuses.InStock;

    public void RecalculateVolume()
    {
        VolumeM3 = LengthCm * WidthCm * HeightCm / 1_000_000m;
    }
}
