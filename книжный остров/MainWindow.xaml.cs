using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = false;
        private readonly string connectionString = @"Server=144.31.48.85,1433;Database=книжный остров;User Id=sa;Password=Database33;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;";
        private readonly ObservableCollection<CartItem> cartItems = new ObservableCollection<CartItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadBooks();
            CartItemsControl.ItemsSource = cartItems;
            UpdateCartSummary();
        }

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

        public class CartItem : INotifyPropertyChanged
        {
            private int quantity = 1;
            private bool isSelected = true;

            public int BookID { get; set; }
            public string Title { get; set; }
            public decimal Price { get; set; }
            public string CoverURL { get; set; }

            public int Quantity
            {
                get => quantity;
                set
                {
                    if (quantity == value) return;
                    quantity = Math.Max(1, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasDiscount));
                    OnPropertyChanged(nameof(CurrentUnitPrice));
                    OnPropertyChanged(nameof(OldUnitPrice));
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }

            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected == value) return;
                    isSelected = value;
                    OnPropertyChanged();
                }
            }

            public bool HasDiscount => Quantity >= 2;
            public decimal CurrentUnitPrice => HasDiscount ? Math.Round(Price * 0.9m, 2) : Price;
            public decimal OldUnitPrice => Price;
            public decimal TotalPrice => CurrentUnitPrice * Quantity;

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

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

                BooksItemsControl.ItemsSource = books;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            var gradient = (LinearGradientBrush)MainGrid.Background;

            ColorAnimation anim1 = new ColorAnimation { Duration = TimeSpan.FromMilliseconds(500) };
            ColorAnimation anim2 = new ColorAnimation { Duration = TimeSpan.FromMilliseconds(500) };

            if (isDarkTheme)
            {
                anim1.To = Color.FromRgb(255, 255, 255);
                anim2.To = Color.FromRgb(245, 245, 245);
                isDarkTheme = false;
                ThemeBtn.Content = "🌙 тема";
            }
            else
            {
                anim1.To = Color.FromRgb(40, 40, 40);
                anim2.To = Color.FromRgb(60, 60, 60);
                isDarkTheme = true;
                ThemeBtn.Content = "☀️ тема";
            }

            gradient.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, anim1);
            gradient.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, anim2);
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int bookId = (int)btn.Tag;

            if (BooksItemsControl.ItemsSource is not IEnumerable<Book> books)
            {
                return;
            }

            Book selectedBook = null;
            foreach (Book book in books)
            {
                if (book.BookID == bookId)
                {
                    selectedBook = book;
                    break;
                }
            }

            if (selectedBook == null)
            {
                return;
            }

            CartItem existing = null;
            foreach (var item in cartItems)
            {
                if (item.BookID == selectedBook.BookID)
                {
                    existing = item;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                var newItem = new CartItem
                {
                    BookID = selectedBook.BookID,
                    Title = selectedBook.Title,
                    Price = selectedBook.Price,
                    CoverURL = selectedBook.CoverURL,
                    Quantity = 1,
                    IsSelected = true
                };
                newItem.PropertyChanged += CartItem_PropertyChanged;
                cartItems.Add(newItem);
            }

            UpdateCartSummary();
            CartPanel.Visibility = Visibility.Visible;
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.IsSelected))
            {
                UpdateCartSummary();
            }
        }

        private void reg(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Owner = this;
            loginWindow.ShowDialog();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == "Поиск...")
            {
                tb.Text = "";
                tb.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            CartPanel.Visibility = CartPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool selectAll = SelectAllCheckBox.IsChecked == true;
            foreach (var item in cartItems)
            {
                item.IsSelected = selectAll;
            }
            UpdateCartSummary();
        }

        private void ItemSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCartSummary();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CartItem item })
            {
                item.Quantity++;
                UpdateCartSummary();
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CartItem item } && item.Quantity > 1)
            {
                item.Quantity--;
                UpdateCartSummary();
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: CartItem item }) return;

            item.PropertyChanged -= CartItem_PropertyChanged;
            cartItems.Remove(item);
            UpdateCartSummary();
        }

        private void UpdateCartSummary()
        {
            decimal total = 0;
            int selectedCount = 0;

            foreach (var item in cartItems)
            {
                if (item.IsSelected)
                {
                    total += item.TotalPrice;
                    selectedCount++;
                }
            }

            CartTotalText.Text = $"Итого: {total:0.##} ₽";
            CartCountBadge.Text = cartItems.Count.ToString();

            bool hasItems = cartItems.Count > 0;
            CartEmptyText.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            CartItemsControl.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;

            if (!hasItems)
            {
                SelectAllCheckBox.IsChecked = false;
                return;
            }

            SelectAllCheckBox.IsChecked = selectedCount == cartItems.Count;
        }
    }
}
