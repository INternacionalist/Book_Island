# SQL-гайд для Book Island

## 1) Создание инфраструктуры
```sql
IF OBJECT_ID('dbo.CartItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        BookID INT NOT NULL,
        Quantity INT NOT NULL,
        IsSelected BIT NOT NULL DEFAULT(1),
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Cart_User_Book UNIQUE(UserId, BookID)
    );
END;

IF OBJECT_ID('dbo.Orders','U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders(
        OrderId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        City NVARCHAR(120) NOT NULL,
        District NVARCHAR(120) NOT NULL,
        Street NVARCHAR(120) NOT NULL,
        House NVARCHAR(30) NOT NULL,
        Apartment NVARCHAR(30) NULL,
        Intercom NVARCHAR(30) NULL,
        Floor NVARCHAR(30) NULL,
        Status NVARCHAR(40) NOT NULL DEFAULT N'Ожидается',
        TotalAmount DECIMAL(12,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ExpectedDeliveryAt DATETIME2 NOT NULL DEFAULT DATEADD(DAY,2,SYSUTCDATETIME())
    );
END;

IF OBJECT_ID('dbo.OrderItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        BookID INT NOT NULL,
        TitleSnapshot NVARCHAR(255) NOT NULL,
        UnitPrice DECIMAL(12,2) NOT NULL,
        Quantity INT NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.ErrorLogs','U') IS NULL
BEGIN
    CREATE TABLE dbo.ErrorLogs(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Source NVARCHAR(100) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        StackTrace NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        UserId INT NULL
    );
END;

IF COL_LENGTH('dbo.Books','CoverImage') IS NULL
    ALTER TABLE dbo.Books ADD CoverImage VARBINARY(MAX) NULL;
IF COL_LENGTH('dbo.Books','SalesCount') IS NULL
    ALTER TABLE dbo.Books ADD SalesCount INT NOT NULL CONSTRAINT DF_Books_SalesCount DEFAULT(0);
IF COL_LENGTH('dbo.Books','CartAddsCount') IS NULL
    ALTER TABLE dbo.Books ADD CartAddsCount INT NOT NULL CONSTRAINT DF_Books_CartAddsCount DEFAULT(0);

IF COL_LENGTH('dbo.читатели','AddressCity') IS NULL
BEGIN
    ALTER TABLE dbo.читатели ADD AddressCity NVARCHAR(120) NULL, AddressDistrict NVARCHAR(120) NULL,
        AddressStreet NVARCHAR(120) NULL, AddressHouse NVARCHAR(30) NULL, AddressApartment NVARCHAR(30) NULL,
        AddressIntercom NVARCHAR(30) NULL, AddressFloor NVARCHAR(30) NULL;
END;
```

## 2) Пример добавления книги с обложкой (VARBINARY)
```sql
INSERT INTO dbo.Books(Title, Author, Price, Description, Genre, PublishYear, InStock, CoverImage)
VALUES (N'Название', N'Автор', 999.00, N'Описание', N'Жанр', 2024, 1, (SELECT * FROM OPENROWSET(BULK N'C:\\covers\\book1.jpg', SINGLE_BLOB) as img));
```

## 3) Проверка топ-10 популярных
```sql
SELECT TOP 10 BookID, Title,
       ISNULL(SalesCount,0) AS SalesCount,
       ISNULL(CartAddsCount,0) AS CartAddsCount,
       (ISNULL(SalesCount,0)*0.5 + ISNULL(CartAddsCount,0)*0.5) AS Score
FROM dbo.Books
ORDER BY Score DESC;
```

## 4) Проверка ошибок
```sql
SELECT TOP 100 * FROM dbo.ErrorLogs ORDER BY CreatedAtUtc DESC;
```

## 5) Проверка заказов пользователя
```sql
SELECT o.OrderId, o.Status, o.TotalAmount, o.CreatedAt,
       o.City, o.District, o.Street, o.House, o.Apartment
FROM dbo.Orders o
WHERE o.UserId = 1
ORDER BY o.CreatedAt DESC;
```
