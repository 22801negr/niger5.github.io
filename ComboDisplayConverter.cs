using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WarehousePro.Wpf;

public sealed class ComboDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DataRowView row)
        {
            if (row.Row.Table.Columns.Contains("Name"))
                return row["Name"]?.ToString() ?? string.Empty;

            return row.Row.ItemArray.Length > 0
                ? row[0]?.ToString() ?? string.Empty
                : string.Empty;
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
