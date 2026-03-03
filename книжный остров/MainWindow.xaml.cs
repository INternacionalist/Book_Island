using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = @"Server=144.31.48.85,1433;Database=книжный остров;User Id=sa;Password=Database33;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;";
        private readonly List<Book> allBooks = new();

        public MainWindow()
        {
            InitializeComponent();
            UpdateProfileButton();
            LoadBooks();
        }

        public class Book
        {
            public int BookID { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Author { get; set; } = "Неизвестный автор";
            public decimal Price { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Genre { get; set; } = "Без жанра";
            public int PublishYear { get; set; }
            public bool InStock { get; set; }
            public ImageSource? CoverImage { get; set; }
        }

        public class GenreGroup
        {
            public string GenreName { get; set; } = string.Empty;
            public List<Book> Books { get; set; } = new();
        }

        private void LoadBooks()
        {
            allBooks.Clear();

            try
            {
                using SqlConnection conn = new(connectionString);
                conn.Open();

                bool hasCoverImageColumn = HasColumn(conn, "dbo", "Books", "CoverImage");
                string coverImageSelect = hasCoverImageColumn
                    ? "b.CoverImage"
                    : "CAST(NULL AS varbinary(max)) AS CoverImage";

                string query = $@"SELECT b.BookID, b.Title, b.Author, b.Price, b.Description, b.Genre, b.PublishYear, b.InStock,
                                         {coverImageSelect}
                                  FROM dbo.Books b
                                  WHERE b.InStock = 1";

                using SqlCommand cmd = new(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                int bookIdOrdinal = reader.GetOrdinal("BookID");
                int titleOrdinal = reader.GetOrdinal("Title");
                int authorOrdinal = reader.GetOrdinal("Author");
                int priceOrdinal = reader.GetOrdinal("Price");
                int descriptionOrdinal = reader.GetOrdinal("Description");
                int genreOrdinal = reader.GetOrdinal("Genre");
                int publishYearOrdinal = reader.GetOrdinal("PublishYear");
                int inStockOrdinal = reader.GetOrdinal("InStock");
                int coverImageOrdinal = reader.GetOrdinal("CoverImage");

                while (reader.Read())
                {
                    allBooks.Add(new Book
                    {
                        BookID = reader.GetInt32(bookIdOrdinal),
                        Title = reader.IsDBNull(titleOrdinal) ? "Без названия" : reader.GetString(titleOrdinal),
                        Author = reader.IsDBNull(authorOrdinal) ? "Неизвестный автор" : reader.GetString(authorOrdinal),
                        Price = reader.IsDBNull(priceOrdinal) ? 0 : reader.GetDecimal(priceOrdinal),
                        Description = reader.IsDBNull(descriptionOrdinal) ? string.Empty : reader.GetString(descriptionOrdinal),
                        CoverImage = reader.IsDBNull(coverImageOrdinal) ? BuildPlaceholderImage() : BuildImage((byte[])reader[coverImageOrdinal]),
                        Genre = reader.IsDBNull(genreOrdinal) ? "Без жанра" : reader.GetString(genreOrdinal),
                        PublishYear = reader.IsDBNull(publishYearOrdinal) ? 0 : reader.GetInt32(publishYearOrdinal),
                        InStock = reader.GetBoolean(inStockOrdinal)
                    });
                }

                BindSections();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool HasColumn(SqlConnection connection, string schemaName, string tableName, string columnName)
        {
            const string query = @"SELECT COUNT(1)
                                   FROM INFORMATION_SCHEMA.COLUMNS
                                   WHERE TABLE_SCHEMA = @schemaName
                                     AND TABLE_NAME = @tableName
                                     AND COLUMN_NAME = @columnName";

            using SqlCommand cmd = new(query, connection);
            cmd.Parameters.AddWithValue("@schemaName", schemaName);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            cmd.Parameters.AddWithValue("@columnName", columnName);

            object? result = cmd.ExecuteScalar();
            return result is int count && count > 0;
        }

        private void BindSections()
        {
            var popularStub = allBooks.Take(8).ToList();
            PopularBooksItemsControl.ItemsSource = popularStub;

            var genreGroups = allBooks
                .GroupBy(b => b.Genre)
                .Take(4)
                .Select(group => new GenreGroup
                {
                    GenreName = group.Key,
                    Books = group.Take(6).ToList()
                })
                .ToList();

            GenresItemsControl.ItemsSource = genreGroups;
        }

        private static BitmapImage BuildImage(byte[] bytes)
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

        private static BitmapImage BuildPlaceholderImage()
        {
            byte[] imageBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAEAAAABQCAIAAAD6xG44AAAAiElEQVR4nO3PQQ3AIBDAsAP/nkEEj4ZkVbDX1pk5M+/w5wBnGdAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNBW5wFB7fN6mQAAAABJRU5ErkJggg==");
            return BuildImage(imageBytes);
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int bookId)
            {
                return;
            }

            Book? selectedBook = allBooks.FirstOrDefault(b => b.BookID == bookId);
            if (selectedBook == null)
            {
                return;
            }

            MessageBox.Show($"Книга добавлена в корзину!\n\n📚 {selectedBook.Title}\n✍️ {selectedBook.Author}\n💰 {selectedBook.Price} ₽",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                LoginWindow loginWindow = new()
                {
                    Owner = this
                };

                bool? dialogResult = loginWindow.ShowDialog();
                if (dialogResult == true)
                {
                    UpdateProfileButton();
                }

                return;
            }

            ProfileWindow profileWindow = new()
            {
                Owner = this
            };
            profileWindow.ShowDialog();
        }

        private void UpdateProfileButton()
        {
            ProfileButton.Content = UserSession.IsAuthenticated
                ? $"👤 {UserSession.UserName}"
                : "👤 Логин";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                SearchSuggestionsPopup.IsOpen = false;
                SearchSuggestionsList.ItemsSource = null;
                return;
            }

            List<Book> matches = allBooks
                .Where(book => book.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(7)
                .ToList();

            SearchSuggestionsList.ItemsSource = matches;
            SearchSuggestionsPopup.IsOpen = matches.Count > 0;
        }

        private void SearchSuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchSuggestionsList.SelectedItem is Book selected)
            {
                SearchBox.Text = selected.Title;
                SearchBox.CaretIndex = SearchBox.Text.Length;
                SearchSuggestionsPopup.IsOpen = false;
            }
        }

        private void SearchSuggestionsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SearchSuggestionsList.SelectedItem is Book selected)
            {
                MessageBox.Show($"Открываем карточку книги: {selected.Title}", "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);
                SearchSuggestionsPopup.IsOpen = false;
            }
        }
    }
}
