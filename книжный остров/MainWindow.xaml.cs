using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data.SqlClient;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private string connectionString = @"Server=144.31.48.85,1433;Database=книжный остров;User Id=sa;Password=Database33;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;";
        private readonly List<Book> cartBooks = new List<Book>();

        public MainWindow()
        {
            InitializeComponent();
            LoadBooks(); // Загружаем книги при запуске
        }

        // Класс для хранения данных о книге
        public class Book
        {
            public int BookID { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public decimal Price { get; set; }
            public string Description { get; set; }
            public string CoverURL { get; set; }
            public string Genre { get; set; }
            public int PublishYear { get; set; }
            public bool InStock { get; set; }
        }

        // Загрузка книг из БД
        private void LoadBooks()
        {
            List<Book> books = new List<Book>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT BookID, Title, Author, Price, Description, CoverURL, Genre, PublishYear, InStock FROM dbo.Books WHERE InStock = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new Book
                                {
                                    BookID = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Author = reader.IsDBNull(2) ? "Неизвестный автор" : reader.GetString(2),
                                    Price = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                    Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    CoverURL = reader.IsDBNull(5) ? "https://via.placeholder.com/200x300?text=No+Image" : reader.GetString(5),
                                    Genre = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    PublishYear = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                    InStock = reader.GetBoolean(8)
                                });
                            }
                        }
                    }
                }

                // Привязываем данные к интерфейсу
                BooksItemsControl.ItemsSource = books;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Купить"
        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int bookId = (int)btn.Tag;

            // Находим книгу по ID
            Book selectedBook = null;
            foreach (Book book in BooksItemsControl.ItemsSource)
            {
                if (book.BookID == bookId)
                {
                    selectedBook = book;
                    break;
                }
            }

            if (selectedBook != null)
            {
                cartBooks.Add(selectedBook);
                CartBtn.Content = $"🛒 корзина ({cartBooks.Count})";

                MessageBox.Show(
                    $"Книга добавлена в корзину!\n\n" +
                    $"📚 {selectedBook.Title}\n" +
                    $"✍️ {selectedBook.Author}\n" +
                    $"💰 {selectedBook.Price} ₽",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void CartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (cartBooks.Count == 0)
            {
                MessageBox.Show("Корзина пока пустая.", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal totalPrice = 0;
            List<string> cartItems = new List<string>();

            foreach (Book book in cartBooks)
            {
                totalPrice += book.Price;
                cartItems.Add($"• {book.Title} — {book.Price} ₽");
            }

            MessageBox.Show(
                $"В корзине {cartBooks.Count} книг(и):\n\n{string.Join("\n", cartItems)}\n\nИтого: {totalPrice} ₽",
                "Корзина",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // Открытие окна логина
        private void reg(object sender, RoutedEventArgs e)
        {
            // Если у тебя есть окно LoginWindow
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Owner = this;
            loginWindow.ShowDialog();
        }

        // Фокус на поиске
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == "Поиск...")
            {
                tb.Text = "";
                tb.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
