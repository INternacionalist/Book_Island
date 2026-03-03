using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private bool isDarkTheme;
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

                string query = @"SELECT BookID, Title, Author, Price, Description, CoverImage, Genre, PublishYear, InStock 
                                 FROM dbo.Books
                                 WHERE InStock = 1";

                using SqlCommand cmd = new(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    allBooks.Add(new Book
                    {
                        BookID = reader.GetInt32(0),
                        Title = reader.IsDBNull(1) ? "Без названия" : reader.GetString(1),
                        Author = reader.IsDBNull(2) ? "Неизвестный автор" : reader.GetString(2),
                        Price = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        CoverImage = reader.IsDBNull(5) ? BuildPlaceholderImage() : BuildImage(reader.GetSqlBinary(5).Value),
                        Genre = reader.IsDBNull(6) ? "Без жанра" : reader.GetString(6),
                        PublishYear = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                        InStock = reader.GetBoolean(8)
                    });
                }

                BindSections();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorAnimation foregroundAnimation = new()
            {
                Duration = TimeSpan.FromMilliseconds(300)
            };

            if (isDarkTheme)
            {
                Background = new SolidColorBrush(Color.FromRgb(242, 246, 255));
                foregroundAnimation.To = Color.FromRgb(24, 32, 51);
                ThemeBtn.Content = "🌙 Тема";
                isDarkTheme = false;
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(23, 30, 52));
                foregroundAnimation.To = Color.FromRgb(229, 236, 255);
                ThemeBtn.Content = "☀️ Тема";
                isDarkTheme = true;
            }

            foreach (TextBlock textBlock in FindVisualChildren<TextBlock>(this))
            {
                if (textBlock.Foreground is SolidColorBrush brush)
                {
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, foregroundAnimation);
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    yield return typed;
                }

                foreach (T sub in FindVisualChildren<T>(child))
                {
                    yield return sub;
                }
            }
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
