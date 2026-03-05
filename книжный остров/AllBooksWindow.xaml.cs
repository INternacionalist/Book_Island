using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfAppBookStore
{
    public partial class AllBooksWindow : Window
    {
        private readonly List<MainWindow.Book> source;
        private List<MainWindow.Book> filtered;

        public AllBooksWindow(List<MainWindow.Book> books)
        {
            InitializeComponent();
            source = books;
            filtered = source.ToList();
            BooksItemsControl.ItemsSource = filtered;

            // Подписка на событие вместо переопределения метода OnPreviewMouseDown.
            PreviewMouseDown += AllBooksWindow_PreviewMouseDown;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchBox.Text.Trim();
            filtered = string.IsNullOrWhiteSpace(q)
                ? source.ToList()
                : source.Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            BooksItemsControl.ItemsSource = filtered;
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: MainWindow.Book selectedBook }) return;
            if (selectedBook.IsInCart)
            {
                new CartWindow { Owner = this }.ShowDialog();
                return;
            }

            CartSession.AddBook(selectedBook);
            selectedBook.IsInCart = true;
        }

        private void BuyAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (MainWindow.Book book in filtered.Where(book => !book.IsInCart))
            {
                CartSession.AddBook(book);
                book.IsInCart = true;
            }
        }

        // Новый обработчик события PreviewMouseDown вместо override.
        private void AllBooksWindow_PreviewMouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (BooksItemsControl.ItemsSource is IEnumerable<MainWindow.Book> books)
            {
                foreach (MainWindow.Book b in books)
                    b.IsDescriptionExpanded = false;
            }
        }

        private void DescriptionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock { DataContext: MainWindow.Book book }) return;
            book.IsDescriptionExpanded = true;
            e.Handled = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
