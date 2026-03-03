using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppBookStore
{
    public partial class CartWindow : Window
    {
        public CartWindow()
        {
            InitializeComponent();
            CartItemsControl.ItemsSource = CartSession.Items;

            foreach (CartItem item in CartSession.Items)
            {
                item.PropertyChanged += CartItem_PropertyChanged;
            }

            CartSession.Items.CollectionChanged += Items_CollectionChanged;
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CartItem item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CartItem item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            UpdateSelectAllState();
            UpdateTotal();
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CartItem.IsSelected) or nameof(CartItem.Quantity) or nameof(CartItem.TotalPrice))
            {
                UpdateSelectAllState();
                UpdateTotal();
            }
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool shouldSelect = SelectAllCheckBox.IsChecked == true;
            foreach (CartItem item in CartSession.Items)
            {
                item.IsSelected = shouldSelect;
            }

            UpdateTotal();
        }

        private void ItemSelectionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void IncreaseQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: CartItem item })
            {
                return;
            }

            item.Quantity += 1;
            UpdateTotal();
        }

        private void DecreaseQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: CartItem item })
            {
                return;
            }

            item.Quantity -= 1;
            UpdateTotal();
        }


        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: CartItem item })
            {
                return;
            }

            CartSession.RemoveBook(item.BookID);
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void UpdateSelectAllState()
        {
            if (CartSession.Items.Count == 0)
            {
                SelectAllCheckBox.IsChecked = false;
                return;
            }

            bool areAllSelected = CartSession.Items.All(item => item.IsSelected);
            SelectAllCheckBox.IsChecked = areAllSelected;
        }

        private void UpdateTotal()
        {
            decimal selectedTotal = CartSession.Items
                .Where(item => item.IsSelected)
                .Sum(item => item.TotalPrice);

            SelectedTotalText.Text = $"{selectedTotal:N0} ₽";
        }
    }
}
