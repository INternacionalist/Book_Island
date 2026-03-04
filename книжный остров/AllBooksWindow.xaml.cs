using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppBookStore
{
    public partial class AllBooksWindow : Window
    {
        private readonly List<MainWindow.Book> source;

        public AllBooksWindow(List<MainWindow.Book> books)
        {
            InitializeComponent();
            source = books;
            BooksGrid.ItemsSource = source;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchBox.Text.Trim();
            BooksGrid.ItemsSource = string.IsNullOrWhiteSpace(q)
                ? source
                : source.Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
