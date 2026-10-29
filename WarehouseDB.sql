    USE master;
    GO

    IF DB_ID('WarehouseDB') IS NOT NULL
    BEGIN
        ALTER DATABASE WarehouseDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE WarehouseDB;
    END
    GO

    CREATE DATABASE WarehouseDB;
    GO

    USE WarehouseDB;
    GO

    CREATE TABLE Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Article NVARCHAR(50) NULL,
        Category NVARCHAR(100) NULL,
        Kind NVARCHAR(100) NULL,
        Color NVARCHAR(80) NULL,
        Manufacturer NVARCHAR(150) NULL,
        Material NVARCHAR(100) NULL,
        Unit NVARCHAR(20) NULL,
        Quantity INT NOT NULL DEFAULT 0,
        MinimumStock INT NOT NULL DEFAULT 0,
        PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0
    );

    CREATE TABLE Operations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        Type NVARCHAR(30) NOT NULL CHECK (Type IN (N'Приход', N'Расход', N'Корректировка', N'Возврат от клиента', N'Возврат поставщику')),
        Quantity INT NOT NULL,
        Date DATETIME NOT NULL DEFAULT GETDATE(),
        OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
        CONSTRAINT FK_Operations_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
    );
    GO

    INSERT INTO Products (Name, Article, Category, Kind, Color, Manufacturer, Material, Unit, Quantity, MinimumStock, PurchasePrice, SalePrice) VALUES
    (N'Шкаф офисный', N'SH-01', N'Мебель', N'Шкаф', N'Венге', N'Hoff', N'ЛДСП', N'шт', 7, 5, 8900, 14990),
    (N'Стол рабочий', N'TBL-12', N'Мебель', N'Письменный', N'Дуб', N'IKEA', N'ЛДСП', N'шт', 6, 4, 4500, 7990),
    (N'Стул', N'CHR-07', N'Мебель', N'Офисный', N'Серый', N'IKEA', N'Ткань', N'шт', 20, 10, 1800, 3490),
    (N'Компьютер', N'PC-220', N'Техника', N'Системный блок', N'Чёрный', N'DNS', N'Пластик', N'шт', 3, 3, 25000, 32990),
    (N'Принтер', N'PRN-90', N'Техника', N'Лазерный', N'Белый', N'HP', N'Пластик', N'шт', 4, 2, 12000, 18990);

    INSERT INTO Operations (ProductId, Type, Quantity, Date, OrderNumber) VALUES
    (1, N'Приход', 10, '2026-09-01 09:00:00', N'ORD-20260901-0001'),
    (1, N'Расход', 3, '2026-09-02 11:00:00', N'ORD-20260902-0002'),
    (2, N'Приход', 8, '2026-09-03 10:00:00', N'ORD-20260903-0003'),
    (2, N'Расход', 2, '2026-09-04 15:00:00', N'ORD-20260904-0004'),
    (3, N'Приход', 20, '2026-09-05 12:00:00', N'ORD-20260905-0005'),
    (4, N'Приход', 5, '2026-09-06 09:30:00', N'ORD-20260906-0006'),
    (4, N'Расход', 2, '2026-09-07 10:00:00', N'ORD-20260907-0007'),
    (5, N'Приход', 4, '2026-09-08 13:00:00', N'ORD-20260908-0008');
    GO

SELECT * FROM Products;
SELECT * FROM Operations;
GO
