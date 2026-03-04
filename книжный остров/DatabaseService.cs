using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAppBookStore
{
    public class OrderSummary
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Ожидается";
        public DateTime CreatedAt { get; set; }
        public string AddressLine { get; set; } = string.Empty;
    }

    public class AddressInfo
    {
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string Apartment { get; set; } = string.Empty;
        public string Intercom { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;

        public string AsSingleLine()
        {
            return $"{City}, {District}, {Street}, д. {House}" +
                   (string.IsNullOrWhiteSpace(Apartment) ? string.Empty : $", кв. {Apartment}") +
                   (string.IsNullOrWhiteSpace(Intercom) ? string.Empty : $", домофон {Intercom}") +
                   (string.IsNullOrWhiteSpace(Floor) ? string.Empty : $", этаж {Floor}");
        }
    }

    public static class DatabaseService
    {
        private static bool isEnsured;

        public static void EnsureInfrastructure()
        {
            if (isEnsured)
            {
                return;
            }

            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();

            string script = @"
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
BEGIN
    ALTER TABLE dbo.Books ADD CoverImage VARBINARY(MAX) NULL;
END;

IF COL_LENGTH('dbo.Books','SalesCount') IS NULL
BEGIN
    ALTER TABLE dbo.Books ADD SalesCount INT NOT NULL CONSTRAINT DF_Books_SalesCount DEFAULT(0);
END;

IF COL_LENGTH('dbo.Books','CartAddsCount') IS NULL
BEGIN
    ALTER TABLE dbo.Books ADD CartAddsCount INT NOT NULL CONSTRAINT DF_Books_CartAddsCount DEFAULT(0);
END;

IF COL_LENGTH('dbo.читатели','AddressCity') IS NULL
BEGIN
    ALTER TABLE dbo.читатели ADD AddressCity NVARCHAR(120) NULL, AddressDistrict NVARCHAR(120) NULL,
        AddressStreet NVARCHAR(120) NULL, AddressHouse NVARCHAR(30) NULL, AddressApartment NVARCHAR(30) NULL,
        AddressIntercom NVARCHAR(30) NULL, AddressFloor NVARCHAR(30) NULL;
END;
";

            using SqlCommand cmd = new(script, conn);
            cmd.ExecuteNonQuery();
            isEnsured = true;
        }

        public static List<MainWindow.Book> LoadBooks(bool onlyInStock = true)
        {
            EnsureInfrastructure();
            List<MainWindow.Book> books = new();

            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();

            string query = @"SELECT BookID, Title, Author, Price, Description, Genre, PublishYear, InStock, CoverImage,
                                    ISNULL(SalesCount,0) AS SalesCount, ISNULL(CartAddsCount,0) AS CartAddsCount
                             FROM dbo.Books
                             WHERE (@onlyInStock = 0 OR InStock = 1)";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@onlyInStock", onlyInStock ? 1 : 0);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                books.Add(new MainWindow.Book
                {
                    BookID = reader.GetInt32(reader.GetOrdinal("BookID")),
                    Title = reader["Title"]?.ToString() ?? "Без названия",
                    Author = reader["Author"]?.ToString() ?? "Неизвестный автор",
                    Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Price")),
                    Description = reader["Description"]?.ToString() ?? string.Empty,
                    Genre = reader["Genre"]?.ToString() ?? "Без жанра",
                    PublishYear = reader.IsDBNull(reader.GetOrdinal("PublishYear")) ? 0 : reader.GetInt32(reader.GetOrdinal("PublishYear")),
                    InStock = !reader.IsDBNull(reader.GetOrdinal("InStock")) && reader.GetBoolean(reader.GetOrdinal("InStock")),
                    CoverImage = reader.IsDBNull(reader.GetOrdinal("CoverImage"))
                        ? MainWindow.BuildPlaceholderImage()
                        : BuildImage((byte[])reader["CoverImage"]),
                    SalesCount = reader.IsDBNull(reader.GetOrdinal("SalesCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("SalesCount")),
                    CartAddsCount = reader.IsDBNull(reader.GetOrdinal("CartAddsCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("CartAddsCount"))
                });
            }

            return books;
        }

        public static List<CartItem> LoadUserCart(int userId, List<MainWindow.Book> books)
        {
            EnsureInfrastructure();
            List<CartItem> items = new();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();

            const string query = @"SELECT BookID, Quantity, IsSelected FROM dbo.CartItems WHERE UserId=@userId";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int bookId = reader.GetInt32(0);
                MainWindow.Book? book = books.FirstOrDefault(b => b.BookID == bookId);
                if (book == null)
                {
                    continue;
                }

                items.Add(new CartItem
                {
                    BookID = bookId,
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    Price = book.Price,
                    CoverImage = book.CoverImage,
                    Quantity = reader.GetInt32(1),
                    IsSelected = reader.GetBoolean(2)
                });
            }

            return items;
        }

        public static void UpsertCartItem(int userId, CartItem item)
        {
            EnsureInfrastructure();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();
            const string query = @"
MERGE dbo.CartItems AS target
USING (SELECT @UserId AS UserId, @BookID AS BookID) AS src
ON target.UserId = src.UserId AND target.BookID = src.BookID
WHEN MATCHED THEN
    UPDATE SET Quantity=@Quantity, IsSelected=@IsSelected, UpdatedAtUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT(UserId, BookID, Quantity, IsSelected) VALUES (@UserId, @BookID, @Quantity, @IsSelected);";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@BookID", item.BookID);
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@IsSelected", item.IsSelected);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteCartItem(int userId, int bookId)
        {
            EnsureInfrastructure();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();
            using SqlCommand cmd = new("DELETE FROM dbo.CartItems WHERE UserId=@uid AND BookID=@bid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@bid", bookId);
            cmd.ExecuteNonQuery();
        }

        public static void IncrementCartAdds(int bookId)
        {
            EnsureInfrastructure();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();
            using SqlCommand cmd = new("UPDATE dbo.Books SET CartAddsCount = ISNULL(CartAddsCount,0) + 1 WHERE BookID=@id", conn);
            cmd.Parameters.AddWithValue("@id", bookId);
            cmd.ExecuteNonQuery();
        }

        public static int CreateOrder(int userId, AddressInfo address, List<CartItem> selectedItems)
        {
            EnsureInfrastructure();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();
            using SqlTransaction tx = conn.BeginTransaction();

            decimal total = selectedItems.Sum(i => i.TotalPrice);
            const string insertOrder = @"INSERT INTO dbo.Orders(UserId, City, District, Street, House, Apartment, Intercom, Floor, TotalAmount)
                                         VALUES (@uid, @city, @district, @street, @house, @apartment, @intercom, @floor, @total);
                                         SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using SqlCommand orderCmd = new(insertOrder, conn, tx);
            orderCmd.Parameters.AddWithValue("@uid", userId);
            orderCmd.Parameters.AddWithValue("@city", address.City);
            orderCmd.Parameters.AddWithValue("@district", address.District);
            orderCmd.Parameters.AddWithValue("@street", address.Street);
            orderCmd.Parameters.AddWithValue("@house", address.House);
            orderCmd.Parameters.AddWithValue("@apartment", string.IsNullOrWhiteSpace(address.Apartment) ? DBNull.Value : address.Apartment);
            orderCmd.Parameters.AddWithValue("@intercom", string.IsNullOrWhiteSpace(address.Intercom) ? DBNull.Value : address.Intercom);
            orderCmd.Parameters.AddWithValue("@floor", string.IsNullOrWhiteSpace(address.Floor) ? DBNull.Value : address.Floor);
            orderCmd.Parameters.AddWithValue("@total", total);

            int orderId = (int)(orderCmd.ExecuteScalar() ?? 0);

            const string insertItem = @"INSERT INTO dbo.OrderItems(OrderId, BookID, TitleSnapshot, UnitPrice, Quantity)
                                        VALUES (@oid, @bookId, @title, @price, @qty);
                                        UPDATE dbo.Books SET SalesCount = ISNULL(SalesCount,0) + @qty WHERE BookID=@bookId;
                                        DELETE FROM dbo.CartItems WHERE UserId=@uid AND BookID=@bookId;";

            foreach (CartItem item in selectedItems)
            {
                using SqlCommand itemCmd = new(insertItem, conn, tx);
                itemCmd.Parameters.AddWithValue("@oid", orderId);
                itemCmd.Parameters.AddWithValue("@uid", userId);
                itemCmd.Parameters.AddWithValue("@bookId", item.BookID);
                itemCmd.Parameters.AddWithValue("@title", item.Title);
                itemCmd.Parameters.AddWithValue("@price", item.DiscountedUnitPrice);
                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                itemCmd.ExecuteNonQuery();
            }

            const string updateAddress = @"UPDATE dbo.читатели
                                           SET AddressCity=@city, AddressDistrict=@district, AddressStreet=@street,
                                               AddressHouse=@house, AddressApartment=@apartment, AddressIntercom=@intercom, AddressFloor=@floor
                                           WHERE id=@uid";
            using SqlCommand addrCmd = new(updateAddress, conn, tx);
            addrCmd.Parameters.AddWithValue("@uid", userId);
            addrCmd.Parameters.AddWithValue("@city", address.City);
            addrCmd.Parameters.AddWithValue("@district", address.District);
            addrCmd.Parameters.AddWithValue("@street", address.Street);
            addrCmd.Parameters.AddWithValue("@house", address.House);
            addrCmd.Parameters.AddWithValue("@apartment", string.IsNullOrWhiteSpace(address.Apartment) ? DBNull.Value : address.Apartment);
            addrCmd.Parameters.AddWithValue("@intercom", string.IsNullOrWhiteSpace(address.Intercom) ? DBNull.Value : address.Intercom);
            addrCmd.Parameters.AddWithValue("@floor", string.IsNullOrWhiteSpace(address.Floor) ? DBNull.Value : address.Floor);
            addrCmd.ExecuteNonQuery();

            tx.Commit();
            return orderId;
        }

        public static List<OrderSummary> GetOrdersByUser(int userId)
        {
            EnsureInfrastructure();
            List<OrderSummary> result = new();
            using SqlConnection conn = new(DatabaseConfig.ConnectionString);
            conn.Open();
            const string query = @"SELECT OrderId, TotalAmount, Status, CreatedAt, City, District, Street, House, Apartment
                                   FROM dbo.Orders
                                   WHERE UserId=@uid
                                   ORDER BY CreatedAt DESC";
            using SqlCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string apt = reader["Apartment"]?.ToString() ?? string.Empty;
                result.Add(new OrderSummary
                {
                    OrderId = reader.GetInt32(0),
                    TotalAmount = reader.GetDecimal(1),
                    Status = reader["Status"]?.ToString() ?? "Ожидается",
                    CreatedAt = reader.GetDateTime(3),
                    AddressLine = $"{reader["City"]}, {reader["District"]}, {reader["Street"]}, д. {reader["House"]}" + (string.IsNullOrWhiteSpace(apt) ? string.Empty : $", кв. {apt}")
                });
            }
            return result;
        }

        public static BitmapImage BuildImage(byte[] bytes)
        {
            using MemoryStream ms = new(bytes);
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
