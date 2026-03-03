using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppBookStore
{
    public partial class CartWindow : Window
    {
        private readonly ObservableCollection<CartItem> cartItems;

        public CartWindow(ObservableCollection<CartItem> cartItems)
        {
            InitializeComponent();
            this.cartItems = cartItems;
            CartItemsControl.ItemsSource = this.cartItems;
            RefreshState();
        }

        private void RefreshState()
        {
            EmptyText.Visibility = cartItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SelectAllCheckBox.IsChecked = cartItems.Count > 0 && cartItems.All(item => item.IsSelected);
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool shouldSelectAll = SelectAllCheckBox.IsChecked == true;
            foreach (CartItem item in cartItems)
            {
                item.IsSelected = shouldSelectAll;
            }

            RefreshState();
        }

        private void ItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RefreshState();
        }

        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                item.Quantity += 1;
            }
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item && item.Quantity > 1)
            {
                item.Quantity -= 1;
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                cartItems.Remove(item);
                RefreshState();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
