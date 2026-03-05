using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAppBookStore
{
    public partial class MainWindow : Window
    {
        private readonly List<Book> allBooks = new();

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                DatabaseService.EnsureInfrastructure();
                LoadBooks();
                UpdateProfileButton();
                UpdateRoleButtons();
                CartSession.Items.CollectionChanged += (_, _) => RefreshBooksCartState();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("MainWindow.ctor", ex);
            }
        }

        public class Book : INotifyPropertyChanged
        {
            private bool isInCart;
            public int BookID { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Author { get; set; } = "Неизвестный автор";
            public decimal Price { get; set; }
            public string Description { get; set; } = string.Empty;
            public string SearchDescription => BuildSearchDescription(Description);
            public string Genre { get; set; } = "Без жанра";
            public int PublishYear { get; set; }
            public bool InStock { get; set; }
            public int SalesCount { get; set; }
            public int CartAddsCount { get; set; }
            public double PopularityScore => (SalesCount * 0.5) + (CartAddsCount * 0.5);
            public ImageSource? CoverImage { get; set; }
            public bool IsInCart
        
            {
                get => isInCart;
                set
                {
                    if (isInCart == value) return;
                    isInCart = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInCart)));
                }
            }
            public event PropertyChangedEventHandler? PropertyChanged;

            private static string BuildSearchDescription(string source)
            {
                if (string.IsNullOrWhiteSpace(source)) return string.Empty;
                string[] words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return words.Length <= 10 ? source : string.Join(' ', words.Take(10)) + "...";
            }

            private bool isDescriptionExpanded;
            public bool IsDescriptionExpanded
            {
                get => isDescriptionExpanded;
                set
                {
                    if (isDescriptionExpanded == value) return;
                    isDescriptionExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDescriptionExpanded)));
                }
            }
        }

        public class GenreGroup
        {
            public string GenreName { get; set; } = string.Empty;
            public List<Book> Books { get; set; } = new();
        }

        private void LoadBooks()
        {
            try
            {
                allBooks.Clear();
                allBooks.AddRange(DatabaseService.LoadBooks());

                if (UserSession.UserId > 0)
                {
                    CartSession.LoadForUser(UserSession.UserId, allBooks);
                }

                BindSections();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("MainWindow.LoadBooks", ex);
            }
        }

        private void BindSections()
        {
            PopularBooksItemsControl.ItemsSource = allBooks.Take(5).ToList();
            TopBooksItemsControl.ItemsSource = allBooks.OrderByDescending(b => b.PopularityScore).Take(10).ToList();
            GenresItemsControl.ItemsSource = allBooks
                .GroupBy(b => b.Genre)
                .Select(group => new GenreGroup { GenreName = group.Key, Books = group.Take(6).ToList() })
                .ToList();
        }

        public static BitmapImage BuildPlaceholderImage()
        {
            byte[] imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAEAAAABQCAIAAAD6xG44AAAAiElEQVR4nO3PQQ3AIBDAsAP/nkEEj4ZkVbDX1pk5M+/w5wBnGdAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNAyQMsALQO0DNBW5wFB7fN6mQAAAABJRU5ErkJggg==");
            return DatabaseService.BuildImage(imageBytes);
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button { DataContext: Book selectedBook }) return;
                if (selectedBook.IsInCart) { OpenCartWindow(); return; }
                CartSession.AddBook(selectedBook);
                selectedBook.IsInCart = true;
            }
            catch (Exception ex) { DbLogger.LogError("MainWindow.BuyButton_Click", ex); }
        }

        private void CartButton_Click(object sender, RoutedEventArgs e) => OpenCartWindow();

        private void OpenCartWindow()
        {
            CartWindow cartWindow = new() { Owner = this };
            cartWindow.ShowDialog();
            RefreshBooksCartState();
            UpdateRoleButtons();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserSession.IsAuthenticated)
                {
                    LoginWindow loginWindow = new() { Owner = this };
                    if (loginWindow.ShowDialog() == true)
                    {
                        LoadBooks();
                        UpdateProfileButton();
                        UpdateRoleButtons();
                        if (UserSession.IsAdmin)
                        {
                            new AdminWindow { Owner = this }.ShowDialog();
                            LoadBooks();
                        }
                    }
                    return;
                }

                new ProfileWindow { Owner = this }.ShowDialog();
            }
            catch (Exception ex) { DbLogger.LogError("MainWindow.ProfileButton_Click", ex); }
        }

        private void UpdateProfileButton()
        {
            ProfileButton.Content = UserSession.IsAuthenticated ? $"👤 {UserSession.UserName}" : "👤 Логин";
        }

        private void UpdateRoleButtons()
        {
            try
            {
                AdminButton.Visibility = UserSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                bool hasOrders = UserSession.UserId > 0 && DatabaseService.GetOrdersByUser(UserSession.UserId).Count > 0;
                OrderHistoryButton.Visibility = hasOrders ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex) { DbLogger.LogError("MainWindow.UpdateRoleButtons", ex); }
        }

        private void RefreshBooksCartState()
        {
            foreach (Book book in allBooks)
            {
                book.IsInCart = CartSession.ContainsBook(book.BookID);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string query = SearchBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(query))
                {
                    SearchSuggestionsPopup.IsOpen = false;
                    SearchSuggestionsList.ItemsSource = null;
                    return;
                }
                List<Book> matches = allBooks.Where(book => book.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(7).ToList();
                SearchSuggestionsList.ItemsSource = matches;
                SearchSuggestionsPopup.IsOpen = matches.Count > 0;
            }
            catch (Exception ex) { DbLogger.LogError("MainWindow.SearchBox_TextChanged", ex); }
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

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            new OrderHistoryWindow { Owner = this }.ShowDialog();
            UpdateRoleButtons();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            new AdminWindow { Owner = this }.ShowDialog();
            LoadBooks();
        }

        private void OpenAllBooksButton_Click(object sender, RoutedEventArgs e)
        {
            new AllBooksWindow(allBooks) { Owner = this }.ShowDialog();
        }
    }
}
