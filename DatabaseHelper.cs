using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WarehousePro.Wpf;

public static class DatabaseHelper
{
    private static readonly object Sync = new();
    private static bool _ready;
    private static bool _isSqlite;
    private static string _connectionString = string.Empty;

    public static string ConnectionLabel { get; private set; } = "";
    public static string? LastError { get; private set; }

    public static bool TestConnection()
    {
        try
        {
            EnsureInitialized();
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private static void EnsureInitialized()
    {
        if (_ready)
            return;

        lock (Sync)
        {
            if (_ready)
                return;

            if (TryConnectSqlServer())
            {
                _ready = true;
                return;
            }

            ConnectSqlite();
            _ready = true;
        }
    }

    private static bool TryConnectSqlServer()
    {
        foreach (var server in GetSqlServers())
        {
            try
            {
                var master = BuildSqlConnectionString(server, "master");
                using (var connection = new SqlConnection(master))
                {
                    connection.Open();
                    using var createDb = new SqlCommand(
                        "IF DB_ID(N'WarehouseDB') IS NULL CREATE DATABASE WarehouseDB;", connection);
                    createDb.ExecuteNonQuery();
                }

                var warehouse = BuildSqlConnectionString(server, "WarehouseDB");
                using (var connection = new SqlConnection(warehouse))
                {
                    connection.Open();
                    EnsureSqlServerSchema(connection);
                    EnsureDemoData(connection);
                }

                _isSqlite = false;
                _connectionString = warehouse;
                ConnectionLabel = server;
                return true;
            }
            catch
            {
                // try the next SQL instance
            }
        }

        return false;
    }

    private static List<string> GetSqlServers()
    {
        var servers = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configured = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(configured);
                if (!string.IsNullOrWhiteSpace(builder.DataSource) && seen.Add(builder.DataSource))
                    servers.Add(builder.DataSource);
            }
            catch
            {
                // ignore malformed config
            }
        }

        foreach (var server in new[] { ".\\SQLEXPRESS", "(localdb)\\MSSQLLocalDB", "localhost\\SQLEXPRESS", "localhost", "." })
        {
            if (seen.Add(server))
                servers.Add(server);
        }

        return servers;
    }

    private static string BuildSqlConnectionString(string server, string database) =>
        new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 8
        }.ConnectionString;

    private static void ConnectSqlite()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WarehousePro.Wpf");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "warehouse.db");
        _isSqlite = true;
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ConnectionString;
        ConnectionLabel = "SQLite";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        EnsureSqliteSchema(connection);
        EnsureDemoData(connection);
    }

    private static IDbConnection Open()
    {
        EnsureInitialized();
        IDbConnection connection = _isSqlite
            ? new SqliteConnection(_connectionString)
            : new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureSqlServerSchema(SqlConnection connection)
    {
        ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Article NVARCHAR(50) NULL,
    Category NVARCHAR(100) NULL,
    Quantity INT NOT NULL DEFAULT 0,
    MinimumStock INT NOT NULL DEFAULT 0
);");
        ExecuteNonQuery(connection, "IF COL_LENGTH(N'dbo.Products', N'Article') IS NULL ALTER TABLE dbo.Products ADD Article NVARCHAR(50) NULL;");
        ExecuteNonQuery(connection, "IF COL_LENGTH(N'dbo.Products', N'Category') IS NULL ALTER TABLE dbo.Products ADD Category NVARCHAR(100) NULL;");
        ExecuteNonQuery(connection, "IF COL_LENGTH(N'dbo.Products', N'MinimumStock') IS NULL ALTER TABLE dbo.Products ADD MinimumStock INT NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT (0) WITH VALUES;");
        ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL
);");
        EnsureProductExtraColumns(connection);
        ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.Operations', N'U') IS NULL
CREATE TABLE dbo.Operations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    Type NVARCHAR(30) NOT NULL,
    Quantity INT NOT NULL,
    Date DATETIME NOT NULL DEFAULT GETDATE(),
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    CONSTRAINT FK_Operations_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
);");
        ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.Operations', N'U') IS NOT NULL
BEGIN
    DECLARE @constraintName SYSNAME;
    DECLARE @sql NVARCHAR(400);

    SELECT TOP 1 @constraintName = cc.name
    FROM sys.check_constraints cc
    INNER JOIN sys.columns c ON c.object_id = cc.parent_object_id AND cc.parent_column_id = c.column_id
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Operations') AND c.name = N'Type';

    WHILE @constraintName IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE dbo.Operations DROP CONSTRAINT ' + QUOTENAME(@constraintName);
        EXEC (@sql);
        SET @constraintName = NULL;
        SELECT TOP 1 @constraintName = cc.name
        FROM sys.check_constraints cc
        INNER JOIN sys.columns c ON c.object_id = cc.parent_object_id AND cc.parent_column_id = c.column_id
        WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Operations') AND c.name = N'Type';
    END

    IF COL_LENGTH(N'dbo.Operations', N'Type') < 60
    BEGIN
        SET @sql = N'ALTER TABLE dbo.Operations ALTER COLUMN Type NVARCHAR(30) NOT NULL';
        EXEC (@sql);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = N'CK_Operations_Type' AND parent_object_id = OBJECT_ID(N'dbo.Operations'))
    BEGIN
        SET @sql = N'ALTER TABLE dbo.Operations ADD CONSTRAINT CK_Operations_Type CHECK ([Type] IN (N''Приход'', N''Расход'', N''Корректировка'', N''Возврат от клиента'', N''Возврат поставщику''))';
        EXEC (@sql);
    END
END");
    }

    private static void EnsureSqliteSchema(SqliteConnection connection)
    {
        ExecuteNonQuery(connection, @"
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Article TEXT NULL,
    Category TEXT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0,
    MinimumStock INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS Operations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Type TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    Date TEXT NOT NULL,
    OrderNumber TEXT NOT NULL UNIQUE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
);");
        EnsureProductExtraColumns(connection);
    }

    private static void EnsureProductExtraColumns(IDbConnection connection)
    {
        EnsureProductColumn(connection, "Kind", "NVARCHAR(100) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "Color", "NVARCHAR(80) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "Manufacturer", "NVARCHAR(150) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "Material", "NVARCHAR(100) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "Unit", "NVARCHAR(20) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "LengthCm", "DECIMAL(10,2) NOT NULL CONSTRAINT DF_Products_LengthCm DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "WidthCm", "DECIMAL(10,2) NOT NULL CONSTRAINT DF_Products_WidthCm DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "HeightCm", "DECIMAL(10,2) NOT NULL CONSTRAINT DF_Products_HeightCm DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "WeightKg", "DECIMAL(10,3) NOT NULL CONSTRAINT DF_Products_WeightKg DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "VolumeM3", "DECIMAL(18,6) NOT NULL CONSTRAINT DF_Products_VolumeM3 DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "PurchasePrice", "DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_PurchasePrice DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "SalePrice", "DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_SalePrice DEFAULT (0) WITH VALUES", "REAL NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "StorageCell", "NVARCHAR(50) NULL", "TEXT NULL");
        EnsureProductColumn(connection, "SupplierId", "INT NULL", "INTEGER NULL");
        EnsureProductColumn(connection, "RequiresAssembly", "BIT NOT NULL CONSTRAINT DF_Products_RequiresAssembly DEFAULT (0) WITH VALUES", "INTEGER NOT NULL DEFAULT 0");
        EnsureProductColumn(connection, "Status", "NVARCHAR(40) NULL", "TEXT NULL");

        if (_isSqlite)
            return;

        ExecuteNonQuery(connection, @"
IF COL_LENGTH(N'dbo.Products', N'SupplierId') IS NOT NULL
   AND OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_Products_Suppliers' AND parent_object_id = OBJECT_ID(N'dbo.Products'))
ALTER TABLE dbo.Products ADD CONSTRAINT FK_Products_Suppliers
    FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id);");
    }

    private static void EnsureProductColumn(IDbConnection connection, string column, string sqlServerType, string sqliteType)
    {
        if (_isSqlite)
        {
            if (!SqliteHasColumn(connection, "Products", column))
                ExecuteNonQuery(connection, $"ALTER TABLE Products ADD COLUMN {column} {sqliteType}");
            return;
        }

        ExecuteNonQuery(connection,
            $"IF COL_LENGTH(N'dbo.Products', N'{column}') IS NULL ALTER TABLE dbo.Products ADD {column} {sqlServerType};");
    }

    private static bool SqliteHasColumn(IDbConnection connection, string table, string column)
    {
        using var command = CreateCommand(connection, $"PRAGMA table_info({table})");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void EnsureDemoData(IDbConnection connection)
    {
        EnsureDemoSuppliers(connection);

        if (ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Products") == 0)
        {
            var demoProducts = new[]
            {
                Catalog("Компьютер", "PC-100", "Техника", "Системный блок", "Чёрный", "DNS", "Пластик", 3, 45, 20, 40, 7.5m, 25000, 32990, "A-01-04", "DNS-опт", false, ProductStatuses.InStock),
                Catalog("Принтер", "PR-220", "Техника", "Лазерный", "Белый", "HP", "Пластик", 2, 40, 35, 25, 8.2m, 12000, 18990, "B-02-08", "HP Direct", false, ProductStatuses.InStock),
                Catalog("Стол рабочий", "TB-350", "Мебель", "Письменный", "Дуб", "IKEA", "ЛДСП", 4, 120, 60, 75, 22m, 4500, 7990, "A-05-12", "IKEA", true, ProductStatuses.InStock),
                Catalog("Стул", "CH-450", "Мебель", "Офисный", "Серый", "IKEA", "Ткань", 12, 50, 50, 90, 6.5m, 1800, 3490, "A-05-14", "IKEA", true, ProductStatuses.OnOrder),
                Catalog("Шкаф офисный", "CAB-120", "Мебель", "Шкаф", "Венге", "Hoff", "ЛДСП", 5, 80, 40, 180, 45m, 8900, 14990, "C-03-01", "Hoff", true, ProductStatuses.InStock)
            };

            foreach (var item in demoProducts)
            {
                item.SupplierId = FindSupplierId(connection, item.Manufacturer == "DNS" ? "DNS-опт" : item.Manufacturer == "HP" ? "HP Direct" : item.Manufacturer);
                item.Quantity = 0;
                InsertProduct(connection, item);
            }
        }

        if (ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Operations") > 0)
            return;

        var now = DateTime.Today;
        var demoOperations = new[]
        {
            new { Type = "Приход", ProductName = "Компьютер", Quantity = 8, Date = now.AddMonths(-3).AddDays(-4) },
            new { Type = "Расход", ProductName = "Компьютер", Quantity = 2, Date = now.AddMonths(-2).AddDays(5) },
            new { Type = "Приход", ProductName = "Принтер", Quantity = 5, Date = now.AddMonths(-2).AddDays(12) },
            new { Type = "Расход", ProductName = "Принтер", Quantity = 3, Date = now.AddMonths(-1).AddDays(2) },
            new { Type = "Приход", ProductName = "Стол рабочий", Quantity = 7, Date = now.AddMonths(-1).AddDays(-6) },
            new { Type = "Расход", ProductName = "Стол рабочий", Quantity = 4, Date = now.AddDays(-10) },
            new { Type = "Приход", ProductName = "Стул", Quantity = 12, Date = now.AddMonths(-3).AddDays(7) },
            new { Type = "Расход", ProductName = "Стул", Quantity = 5, Date = now.AddMonths(-2).AddDays(18) },
            new { Type = "Приход", ProductName = "Шкаф офисный", Quantity = 6, Date = now.AddMonths(-2).AddDays(-8) },
            new { Type = "Расход", ProductName = "Шкаф офисный", Quantity = 2, Date = now.AddMonths(-1).AddDays(9) },
            new { Type = "Корректировка", ProductName = "Компьютер", Quantity = 1, Date = now.AddDays(-4) },
            new { Type = "Возврат от клиента", ProductName = "Принтер", Quantity = 2, Date = now.AddDays(-1) }
        };

        foreach (var item in demoOperations)
        {
            using var findProduct = CreateCommand(connection, "SELECT Id FROM Products WHERE Name = @name");
            AddParam(findProduct, "@name", item.ProductName);
            var productIdValue = findProduct.ExecuteScalar();
            if (productIdValue == null || productIdValue is DBNull)
                continue;
            var productId = Convert.ToInt32(productIdValue);
            if (productId <= 0)
                continue;

            var storedType = OperationTypes.Normalize(item.Type);
            var orderNumber = $"DEMO-{DateTime.Now:yyyyMMddHHmmssfff}-{productId}-{Guid.NewGuid():N}";

            using var insertOperation = CreateCommand(connection,
                "INSERT INTO Operations (ProductId, Type, Quantity, Date, OrderNumber) VALUES (@productId, @type, @quantity, @date, @orderNumber)");
            AddParam(insertOperation, "@productId", productId);
            AddParam(insertOperation, "@type", storedType);
            AddParam(insertOperation, "@quantity", item.Quantity);
            AddParam(insertOperation, "@date", item.Date);
            AddParam(insertOperation, "@orderNumber", orderNumber);
            insertOperation.ExecuteNonQuery();

            using var updateProduct = CreateCommand(connection,
                "UPDATE Products SET Quantity = Quantity + @delta WHERE Id = @productId");
            AddParam(updateProduct, "@delta", OperationTypes.StockDelta(storedType, item.Quantity));
            AddParam(updateProduct, "@productId", productId);
            updateProduct.ExecuteNonQuery();
        }
    }

    private static void EnsureDemoSuppliers(IDbConnection connection)
    {
        if (ExecuteScalarInt(connection, "SELECT COUNT(*) FROM Suppliers") > 0)
            return;

        foreach (var name in new[] { "DNS-опт", "HP Direct", "IKEA", "Hoff", "ООО «МебельТорг»" })
        {
            using var command = CreateCommand(connection, "INSERT INTO Suppliers (Name) VALUES (@name)");
            AddParam(command, "@name", name);
            command.ExecuteNonQuery();
        }
    }

    private static ProductDetails Catalog(
        string name, string article, string category, string kind, string color, string manufacturer, string material,
        int minimumStock, decimal length, decimal width, decimal height, decimal weight,
        decimal purchase, decimal sale, string cell, string supplierName, bool assembly, string status)
    {
        var product = new ProductDetails
        {
            Name = name,
            Article = article,
            Category = category,
            Kind = kind,
            Color = color,
            Manufacturer = manufacturer,
            Material = material,
            Unit = "шт",
            MinimumStock = minimumStock,
            LengthCm = length,
            WidthCm = width,
            HeightCm = height,
            WeightKg = weight,
            PurchasePrice = purchase,
            SalePrice = sale,
            StorageCell = cell,
            RequiresAssembly = assembly,
            Status = status
        };
        product.RecalculateVolume();
        return product;
    }

    private static int? FindSupplierId(IDbConnection connection, string name)
    {
        using var command = CreateCommand(connection, "SELECT Id FROM Suppliers WHERE Name = @name");
        AddParam(command, "@name", name);
        var value = command.ExecuteScalar();
        if (value == null || value is DBNull)
            return null;
        return Convert.ToInt32(value);
    }

    public static DataTable GetSuppliersForCombo()
    {
        using var connection = Open();
        using var command = CreateCommand(connection, "SELECT Id, Name FROM Suppliers ORDER BY Name");
        return LoadTable(command);
    }

    public static DataTable GetProductsTable(string search = "")
    {
        using var connection = Open();
        using var command = CreateCommand(connection, @"
            SELECT p.Id, p.Name, p.Article, p.Category, p.Kind, p.Color, p.Manufacturer, p.Material, p.Unit,
                   p.Quantity, p.MinimumStock,
                   p.LengthCm, p.WidthCm, p.HeightCm, p.WeightKg,
                   (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000.0 AS VolumeM3,
                   p.PurchasePrice, p.SalePrice, p.StorageCell, p.SupplierId, s.Name AS SupplierName,
                   p.RequiresAssembly, p.Status,
                   (SELECT MAX(o.Date) FROM Operations o WHERE o.ProductId = p.Id AND o.Type = 'Приход') AS LastReceiptDate
            FROM Products p
            LEFT JOIN Suppliers s ON s.Id = p.SupplierId
            WHERE @search = ''
               OR p.Name LIKE @pattern
               OR p.Article LIKE @pattern
               OR p.Category LIKE @pattern
               OR p.Kind LIKE @pattern
               OR p.Color LIKE @pattern
               OR p.Manufacturer LIKE @pattern
               OR p.Material LIKE @pattern
               OR p.StorageCell LIKE @pattern
               OR p.Status LIKE @pattern
               OR s.Name LIKE @pattern
            ORDER BY p.Name");
        AddParam(command, "@search", string.IsNullOrWhiteSpace(search) ? string.Empty : search);
        AddParam(command, "@pattern", string.IsNullOrWhiteSpace(search) ? string.Empty : "%" + search + "%");
        var table = LoadTable(command);
        ApplyProductDisplayColumns(table);
        return table;
    }

    private static void ApplyProductDisplayColumns(DataTable table)
    {
        var english = AppSettings.Language == "en";
        if (!table.Columns.Contains("StatusDisplay"))
            table.Columns.Add("StatusDisplay", typeof(string));
        if (!table.Columns.Contains("AssemblyDisplay"))
            table.Columns.Add("AssemblyDisplay", typeof(string));
        if (!table.Columns.Contains("LastReceiptDisplay"))
            table.Columns.Add("LastReceiptDisplay", typeof(string));

        foreach (DataRow row in table.Rows)
        {
            row["StatusDisplay"] = ProductStatuses.Localize(row["Status"]?.ToString(), english);
            row["AssemblyDisplay"] = ToBool(row["RequiresAssembly"])
                ? (english ? "Yes" : "Да")
                : (english ? "No" : "Нет");
            row["LastReceiptDisplay"] = FormatDate(row["LastReceiptDate"], english);
        }
    }

    private static bool ToBool(object? value)
    {
        if (value == null || value is DBNull)
            return false;
        if (value is bool flag)
            return flag;
        if (value is byte or short or int or long)
            return Convert.ToInt64(value) != 0;
        return string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase)
            || value.ToString() == "1";
    }

    private static string FormatDate(object? value, bool english)
    {
        if (value == null || value is DBNull)
            return "—";
        if (value is DateTime date)
            return date.ToString(english ? "MM/dd/yyyy" : "dd.MM.yyyy");
        if (DateTime.TryParse(value.ToString(), out var parsed))
            return parsed.ToString(english ? "MM/dd/yyyy" : "dd.MM.yyyy");
        return value.ToString() ?? "—";
    }

    public static DataTable GetProductsForCombo()
    {
        using var connection = Open();
        using var command = CreateCommand(connection, "SELECT Id, Name FROM Products ORDER BY Name");
        return LoadTable(command);
    }

    public static DataTable GetOperationsTable(string search = "")
    {
        using var connection = Open();
        using var command = CreateCommand(connection, @"
            SELECT p.Name AS ProductName,
                   o.Type,
                   o.Quantity,
                   o.Date,
                   o.OrderNumber
            FROM Operations o
            INNER JOIN Products p ON p.Id = o.ProductId
            WHERE (@search = '' OR p.Name LIKE @pattern OR o.OrderNumber LIKE @pattern)
            ORDER BY o.Date DESC, o.Id DESC");
        AddParam(command, "@search", string.IsNullOrWhiteSpace(search) ? string.Empty : search);
        AddParam(command, "@pattern", string.IsNullOrWhiteSpace(search) ? string.Empty : "%" + search + "%");
        return LoadTable(command);
    }

    public static WarehouseStatistics GetStatisticsSummary()
    {
        var coalesce = _isSqlite ? "IFNULL" : "ISNULL";
        using var connection = Open();
        using var command = CreateCommand(connection, $@"
            SELECT
                (SELECT COUNT(*) FROM Products) AS ProductCount,
                (SELECT {coalesce}(SUM(Quantity), 0) FROM Products) AS TotalQuantity,
                (SELECT COUNT(*) FROM Products WHERE Quantity <= MinimumStock) AS LowStockCount,
                (SELECT COUNT(*) FROM Operations) AS OperationCount,
                (SELECT {coalesce}(SUM(Quantity), 0) FROM Operations WHERE Type IN ('Приход', 'Возврат от клиента')) AS IncomingQuantity,
                (SELECT {coalesce}(SUM(Quantity), 0) FROM Operations WHERE Type IN ('Расход', 'Возврат поставщику')) AS ExpenseQuantity");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return new WarehouseStatistics(0, 0, 0, 0, 0, 0);

        return new WarehouseStatistics(
            Convert.ToInt32(reader["ProductCount"]),
            Convert.ToInt32(reader["TotalQuantity"]),
            Convert.ToInt32(reader["LowStockCount"]),
            Convert.ToInt32(reader["OperationCount"]),
            Convert.ToInt32(reader["IncomingQuantity"]),
            Convert.ToInt32(reader["ExpenseQuantity"]));
    }

    public static DataTable GetTopProducts()
    {
        var coalesce = _isSqlite ? "IFNULL" : "ISNULL";
        var sql = _isSqlite
            ? $@"
            SELECT
                p.Name,
                p.Quantity AS Stock,
                {coalesce}(SUM(CASE WHEN o.Type IN ('Приход', 'Возврат от клиента') THEN o.Quantity ELSE 0 END), 0) AS Incoming,
                {coalesce}(SUM(CASE WHEN o.Type IN ('Расход', 'Возврат поставщику') THEN o.Quantity ELSE 0 END), 0) AS Expense,
                {coalesce}(SUM(CASE WHEN o.Type = 'Корректировка' THEN o.Quantity ELSE 0 END), 0) AS Adjustment,
                {coalesce}(SUM(CASE
                        WHEN o.Type IN ('Приход', 'Возврат от клиента') THEN o.Quantity
                        WHEN o.Type IN ('Расход', 'Возврат поставщику') THEN -o.Quantity
                        WHEN o.Type = 'Корректировка' THEN o.Quantity
                        ELSE 0 END), 0) AS NetMovement,
                COUNT(o.Id) AS OperationsCount
            FROM Products p
            LEFT JOIN Operations o ON o.ProductId = p.Id
            GROUP BY p.Id, p.Name, p.Quantity
            ORDER BY
                {coalesce}(SUM(CASE
                    WHEN o.Type IN ('Приход', 'Возврат от клиента', 'Расход', 'Возврат поставщику') THEN o.Quantity
                    WHEN o.Type = 'Корректировка' THEN ABS(o.Quantity)
                    ELSE 0 END), 0) DESC,
                p.Name ASC
            LIMIT 10"
            : $@"
            SELECT TOP 10
                p.Name,
                p.Quantity AS Stock,
                {coalesce}(SUM(CASE WHEN o.Type IN ('Приход', 'Возврат от клиента') THEN o.Quantity ELSE 0 END), 0) AS Incoming,
                {coalesce}(SUM(CASE WHEN o.Type IN ('Расход', 'Возврат поставщику') THEN o.Quantity ELSE 0 END), 0) AS Expense,
                {coalesce}(SUM(CASE WHEN o.Type = 'Корректировка' THEN o.Quantity ELSE 0 END), 0) AS Adjustment,
                {coalesce}(SUM(CASE
                        WHEN o.Type IN ('Приход', 'Возврат от клиента') THEN o.Quantity
                        WHEN o.Type IN ('Расход', 'Возврат поставщику') THEN -o.Quantity
                        WHEN o.Type = 'Корректировка' THEN o.Quantity
                        ELSE 0 END), 0) AS NetMovement,
                COUNT(o.Id) AS OperationsCount
            FROM Products p
            LEFT JOIN Operations o ON o.ProductId = p.Id
            GROUP BY p.Id, p.Name, p.Quantity
            ORDER BY
                {coalesce}(SUM(CASE
                    WHEN o.Type IN ('Приход', 'Возврат от клиента', 'Расход', 'Возврат поставщику') THEN o.Quantity
                    WHEN o.Type = 'Корректировка' THEN ABS(o.Quantity)
                    ELSE 0 END), 0) DESC,
                p.Name ASC";

        using var connection = Open();
        using var command = CreateCommand(connection, sql);
        return LoadTable(command);
    }

    public static DataTable GetMonthlyMovementData()
    {
        var sql = _isSqlite
            ? @"
            SELECT YearNumber, MonthNumber, Incoming, Expense, Adjustment, NetMovement
            FROM (
                SELECT
                    CAST(strftime('%Y', Date) AS INT) AS YearNumber,
                    CAST(strftime('%m', Date) AS INT) AS MonthNumber,
                    SUM(CASE WHEN Type IN ('Приход', 'Возврат от клиента') THEN Quantity ELSE 0 END) AS Incoming,
                    SUM(CASE WHEN Type IN ('Расход', 'Возврат поставщику') THEN Quantity ELSE 0 END) AS Expense,
                    SUM(CASE WHEN Type = 'Корректировка' THEN Quantity ELSE 0 END) AS Adjustment,
                    SUM(CASE
                            WHEN Type IN ('Приход', 'Возврат от клиента') THEN Quantity
                            WHEN Type IN ('Расход', 'Возврат поставщику') THEN -Quantity
                            WHEN Type = 'Корректировка' THEN Quantity
                            ELSE 0 END) AS NetMovement
                FROM Operations
                GROUP BY strftime('%Y', Date), strftime('%m', Date)
                ORDER BY YearNumber DESC, MonthNumber DESC
                LIMIT 12
            ) t
            ORDER BY YearNumber, MonthNumber"
            : @"
            SELECT YearNumber, MonthNumber, Incoming, Expense, Adjustment, NetMovement
            FROM (
                SELECT TOP 12
                    YEAR(Date) AS YearNumber,
                    MONTH(Date) AS MonthNumber,
                    SUM(CASE WHEN Type IN ('Приход', 'Возврат от клиента') THEN Quantity ELSE 0 END) AS Incoming,
                    SUM(CASE WHEN Type IN ('Расход', 'Возврат поставщику') THEN Quantity ELSE 0 END) AS Expense,
                    SUM(CASE WHEN Type = 'Корректировка' THEN Quantity ELSE 0 END) AS Adjustment,
                    SUM(CASE
                            WHEN Type IN ('Приход', 'Возврат от клиента') THEN Quantity
                            WHEN Type IN ('Расход', 'Возврат поставщику') THEN -Quantity
                            WHEN Type = 'Корректировка' THEN Quantity
                            ELSE 0 END) AS NetMovement
                FROM Operations
                GROUP BY YEAR(Date), MONTH(Date)
                ORDER BY YEAR(Date) DESC, MONTH(Date) DESC
            ) t
            ORDER BY YearNumber, MonthNumber";

        using var connection = Open();
        using var command = CreateCommand(connection, sql);
        return LoadTable(command);
    }

    public static DataTable GetLowStockProducts()
    {
        using var connection = Open();
        using var command = CreateCommand(connection, @"
            SELECT Name, Quantity, MinimumStock, (MinimumStock - Quantity) AS NeedToOrder
            FROM Products
            WHERE Quantity <= MinimumStock
            ORDER BY NeedToOrder DESC, Name ASC");
        return LoadTable(command);
    }

    public static int AddProduct(ProductDetails product)
    {
        ValidateProduct(product);
        var openingQuantity = product.Quantity;
        product.Quantity = 0;

        int productId;
        using (var connection = Open())
            productId = InsertProduct(connection, product);

        if (openingQuantity > 0)
            AddOperation(productId, OperationTypes.Incoming, openingQuantity);

        return productId;
    }

    public static int UpdateProduct(int productId, ProductDetails product)
    {
        if (productId <= 0) throw new Exception("Некорректный идентификатор товара.");
        ValidateProduct(product);

        using var connection = Open();
        using var command = CreateCommand(connection, @"
            UPDATE Products SET
                Name = @name,
                Article = @article,
                Category = @category,
                Kind = @kind,
                Color = @color,
                Manufacturer = @manufacturer,
                Material = @material,
                Unit = @unit,
                MinimumStock = @minimumStock,
                PurchasePrice = @purchasePrice,
                SalePrice = @salePrice
            WHERE Id = @id");
        AddParam(command, "@id", productId);
        BindEditableProduct(command, product);
        return command.ExecuteNonQuery();
    }

    private static void ValidateProduct(ProductDetails product)
    {
        if (string.IsNullOrWhiteSpace(product.Name)) throw new Exception("Название товара не может быть пустым.");
        if (product.Quantity < 0) throw new Exception("Количество товара не может быть отрицательным.");
        if (product.MinimumStock < 0) throw new Exception("Минимальный остаток не может быть отрицательным.");
        if (product.LengthCm < 0 || product.WidthCm < 0 || product.HeightCm < 0)
            throw new Exception("Габариты не могут быть отрицательными.");
        if (product.WeightKg < 0) throw new Exception("Вес не может быть отрицательным.");
        if (product.PurchasePrice < 0 || product.SalePrice < 0)
            throw new Exception("Цена не может быть отрицательной.");
        product.Status = ProductStatuses.Normalize(product.Status);
        product.RecalculateVolume();
    }

    private static int InsertProduct(IDbConnection connection, ProductDetails product)
    {
        var sql = _isSqlite
            ? @"
            INSERT INTO Products (Name, Article, Category, Kind, Color, Manufacturer, Material, Unit, Quantity, MinimumStock,
                LengthCm, WidthCm, HeightCm, WeightKg, VolumeM3, PurchasePrice, SalePrice, StorageCell, SupplierId, RequiresAssembly, Status)
            VALUES (@name, @article, @category, @kind, @color, @manufacturer, @material, @unit, @quantity, @minimumStock,
                @lengthCm, @widthCm, @heightCm, @weightKg, @volumeM3, @purchasePrice, @salePrice, @storageCell, @supplierId, @requiresAssembly, @status);
            SELECT last_insert_rowid();"
            : @"
            INSERT INTO Products (Name, Article, Category, Kind, Color, Manufacturer, Material, Unit, Quantity, MinimumStock,
                LengthCm, WidthCm, HeightCm, WeightKg, VolumeM3, PurchasePrice, SalePrice, StorageCell, SupplierId, RequiresAssembly, Status)
            VALUES (@name, @article, @category, @kind, @color, @manufacturer, @material, @unit, @quantity, @minimumStock,
                @lengthCm, @widthCm, @heightCm, @weightKg, @volumeM3, @purchasePrice, @salePrice, @storageCell, @supplierId, @requiresAssembly, @status);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var command = CreateCommand(connection, sql);
        BindProduct(command, product);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void BindProduct(IDbCommand command, ProductDetails product)
    {
        BindEditableProduct(command, product);
        AddParam(command, "@quantity", product.Quantity);
        AddParam(command, "@lengthCm", product.LengthCm);
        AddParam(command, "@widthCm", product.WidthCm);
        AddParam(command, "@heightCm", product.HeightCm);
        AddParam(command, "@weightKg", product.WeightKg);
        AddParam(command, "@volumeM3", product.VolumeM3);
        AddParam(command, "@storageCell", ToDb(product.StorageCell));
        AddParam(command, "@supplierId", product.SupplierId.HasValue ? product.SupplierId.Value : DBNull.Value);
        AddParam(command, "@requiresAssembly", product.RequiresAssembly);
        AddParam(command, "@status", ProductStatuses.Normalize(product.Status));
    }

    private static void BindEditableProduct(IDbCommand command, ProductDetails product)
    {
        AddParam(command, "@name", product.Name.Trim());
        AddParam(command, "@article", ToDb(product.Article));
        AddParam(command, "@category", ToDb(product.Category));
        AddParam(command, "@kind", ToDb(product.Kind));
        AddParam(command, "@color", ToDb(product.Color));
        AddParam(command, "@manufacturer", ToDb(product.Manufacturer));
        AddParam(command, "@material", ToDb(product.Material));
        AddParam(command, "@unit", ToDb(string.IsNullOrWhiteSpace(product.Unit) ? "шт" : product.Unit));
        AddParam(command, "@minimumStock", product.MinimumStock);
        AddParam(command, "@purchasePrice", product.PurchasePrice);
        AddParam(command, "@salePrice", product.SalePrice);
    }

    private static object ToDb(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    public static int DeleteProduct(int productId)
    {
        if (productId <= 0) throw new Exception("Некорректный идентификатор товара.");

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var deleteOperations = CreateCommand(connection, "DELETE FROM Operations WHERE ProductId = @id", transaction))
            {
                AddParam(deleteOperations, "@id", productId);
                deleteOperations.ExecuteNonQuery();
            }

            using var deleteProduct = CreateCommand(connection, "DELETE FROM Products WHERE Id = @id", transaction);
            AddParam(deleteProduct, "@id", productId);
            var deleted = deleteProduct.ExecuteNonQuery();
            transaction.Commit();
            return deleted;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public static void AddOperation(int productId, string type, int quantity)
    {
        if (quantity <= 0) throw new Exception("Количество должно быть больше нуля.");

        var storedType = OperationTypes.Normalize(type);
        if (storedType is OperationTypes.Adjustment or OperationTypes.Return || !OperationTypes.IsStoredType(storedType))
            throw new Exception("Некорректный тип операции.");

        ApplyStockChange(productId, storedType, quantity, $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}-{productId}");
    }

    public static void AdjustStock(int productId, int delta, string reason = "")
    {
        if (productId <= 0) throw new Exception("Некорректный идентификатор товара.");
        if (delta == 0) throw new Exception("Корректировка не может быть нулевой.");

        ApplyStockChange(productId, OperationTypes.Adjustment, delta, $"ADJ-{DateTime.Now:yyyyMMddHHmmssfff}-{productId}", signedQuantity: true);
    }

    private static void ApplyStockChange(int productId, string storedType, int quantityOrDelta, string orderNumber, bool signedQuantity = false)
    {
        var delta = signedQuantity ? quantityOrDelta : OperationTypes.StockDelta(storedType, quantityOrDelta);
        var storedQuantity = signedQuantity ? quantityOrDelta : quantityOrDelta;

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var selectCommand = CreateCommand(connection, "SELECT Quantity FROM Products WHERE Id = @productId", transaction))
            {
                AddParam(selectCommand, "@productId", productId);
                var currentQuantity = selectCommand.ExecuteScalar();
                if (currentQuantity == null || currentQuantity is DBNull)
                    throw new Exception("Товар не найден.");

                var stock = Convert.ToInt32(currentQuantity);
                if (stock + delta < 0)
                    throw new Exception(signedQuantity
                        ? "После корректировки остаток не может быть отрицательным."
                        : "Недостаточно товара на складе.");
            }

            using (var updateCommand = CreateCommand(connection, "UPDATE Products SET Quantity = Quantity + @delta WHERE Id = @productId", transaction))
            {
                AddParam(updateCommand, "@productId", productId);
                AddParam(updateCommand, "@delta", delta);
                updateCommand.ExecuteNonQuery();
            }

            using (var insertCommand = CreateCommand(connection,
                "INSERT INTO Operations (ProductId, Type, Quantity, Date, OrderNumber) VALUES (@productId, @type, @quantity, @date, @orderNumber)", transaction))
            {
                AddParam(insertCommand, "@productId", productId);
                AddParam(insertCommand, "@type", storedType);
                AddParam(insertCommand, "@quantity", storedQuantity);
                AddParam(insertCommand, "@date", DateTime.Now);
                AddParam(insertCommand, "@orderNumber", orderNumber);
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static IDbCommand CreateCommand(IDbConnection connection, string sql, IDbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (transaction != null)
            command.Transaction = transaction;
        return command;
    }

    private static void AddParam(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static DataTable LoadTable(IDbCommand command)
    {
        var table = new DataTable();
        using var reader = command.ExecuteReader();
        table.Load(reader);
        return table;
    }

    private static int ExecuteScalarInt(IDbConnection connection, string sql)
    {
        using var command = CreateCommand(connection, sql);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void ExecuteNonQuery(IDbConnection connection, string sql)
    {
        using var command = CreateCommand(connection, sql);
        command.ExecuteNonQuery();
    }
}

public sealed record WarehouseStatistics(
    int ProductCount,
    int TotalQuantity,
    int LowStockCount,
    int OperationCount,
    int IncomingQuantity,
    int ExpenseQuantity);
