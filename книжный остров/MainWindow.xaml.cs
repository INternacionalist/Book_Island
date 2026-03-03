using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private const decimal BulkDiscountFactor = 0.9m;

        private bool isDarkTheme = false;
        private bool suppressSelectAllSync = false;
        private readonly string connectionString = @"Server=144.31.48.85,1433;Database=книжный остров;User Id=sa;Password=Database33;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;";
        private readonly ObservableCollection<CartItem> cartItems = new ObservableCollection<CartItem>();

        public MainWindow()
        {
            InitializeComponent();
            CartItemsControl.ItemsSource = cartItems;
            LoadBooks();
            UpdateSelectAllState();
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
            public string CoverURL { get; set; }
            public decimal UnitPrice { get; set; }

            public int Quantity
            {
                get => quantity;
                set
                {
                    var newValue = value < 1 ? 1 : value;
                    if (quantity == newValue)
                    {
                        return;
                    }

                    quantity = newValue;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(HasDiscount));
                    OnPropertyChanged(nameof(DiscountedUnitPrice));
                    OnPropertyChanged(nameof(OldTotalPrice));
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }

            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected == value)
                    {
                        return;
                    }

                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }

            public bool HasDiscount => Quantity >= 2;
            public decimal DiscountedUnitPrice => HasDiscount ? UnitPrice * BulkDiscountFactor : UnitPrice;
            public decimal OldTotalPrice => UnitPrice * Quantity;
            public decimal TotalPrice => DiscountedUnitPrice * Quantity;

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private void LoadBooks()
        {
            var books = new List<Book>();

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    const string query = "SELECT BookID, Title, Author, Price, Description, CoverURL, Genre, PublishYear, InStock FROM dbo.Books WHERE InStock = 1";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                BookID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Author = reader.IsDBNull(2) ? "Неизвестный автор" : reader.GetString(2),
                                Price = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                CoverURL = reader.IsDBNull(5) ? "https://via.placeholder.com/200x300?text=No+Image" : reader.GetString(5),
                                Genre = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
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

            var anim1 = new ColorAnimation { Duration = new TimeSpan(0, 0, 0, 0, 500) };
            var anim2 = new ColorAnimation { Duration = new TimeSpan(0, 0, 0, 0, 500) };

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
            if (BooksItemsControl.ItemsSource is not IEnumerable<Book> books)
            {
                return;
            }

            var btn = (Button)sender;
            var bookId = (int)btn.Tag;
            var selectedBook = books.FirstOrDefault(book => book.BookID == bookId);

            if (selectedBook == null)
            {
                return;
            }

            var existingItem = cartItems.FirstOrDefault(item => item.BookID == selectedBook.BookID);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                existingItem.IsSelected = true;
            }
            else
            {
                var cartItem = new CartItem
                {
                    BookID = selectedBook.BookID,
                    Title = selectedBook.Title,
                    CoverURL = selectedBook.CoverURL,
                    UnitPrice = selectedBook.Price,
                    Quantity = 1,
                    IsSelected = true
                };

                cartItem.PropertyChanged += CartItem_PropertyChanged;
                cartItems.Add(cartItem);
            }

            CartPanel.Visibility = Visibility.Visible;
            UpdateSelectAllState();
            UpdateCartSummary();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            CartPanel.Visibility = CartPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CloseCartButton_Click(object sender, RoutedEventArgs e)
        {
            CartPanel.Visibility = Visibility.Collapsed;
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (suppressSelectAllSync)
            {
                return;
            }

            var isChecked = SelectAllCheckBox.IsChecked == true;
            suppressSelectAllSync = true;
            foreach (var cartItem in cartItems)
            {
                cartItem.IsSelected = isChecked;
            }
            suppressSelectAllSync = false;

            UpdateSelectAllState();
            UpdateCartSummary();
        }

        private void CartItemSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!suppressSelectAllSync)
            {
                UpdateSelectAllState();
                UpdateCartSummary();
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var item = FindCartItemByButton(sender);
            if (item == null)
            {
                return;
            }

            item.Quantity += 1;
            UpdateCartSummary();
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var item = FindCartItemByButton(sender);
            if (item == null)
            {
                return;
            }

            item.Quantity -= 1;
            UpdateCartSummary();
        }

        private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            var item = FindCartItemByButton(sender);
            if (item == null)
            {
                return;
            }

            item.PropertyChanged -= CartItem_PropertyChanged;
            cartItems.Remove(item);

            UpdateSelectAllState();
            UpdateCartSummary();
        }

        private CartItem FindCartItemByButton(object sender)
        {
            var button = (Button)sender;
            var bookId = (int)button.Tag;
            return cartItems.FirstOrDefault(item => item.BookID == bookId);
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.IsSelected))
            {
                UpdateSelectAllState();
                UpdateCartSummary();
            }
        }

        private void UpdateSelectAllState()
        {
            suppressSelectAllSync = true;
            SelectAllCheckBox.IsChecked = cartItems.Count > 0 && cartItems.All(item => item.IsSelected);
            suppressSelectAllSync = false;
        }

        private void UpdateCartSummary()
        {
            var selectedItems = cartItems.Where(item => item.IsSelected).ToList();
            var selectedCount = selectedItems.Sum(item => item.Quantity);
            var total = selectedItems.Sum(item => item.TotalPrice);

            CartSummaryText.Text = selectedCount > 0
                ? $"Выбрано товаров: {selectedCount}"
                : "Выберите книги для оформления";
            CartTotalText.Text = $"Итого: {total:F0} ₽";
        }

        private void reg(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Owner = this;
            loginWindow.ShowDialog();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (tb.Text == "Поиск...")
            {
                tb.Text = string.Empty;
                tb.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
