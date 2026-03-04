using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                foreach (CartItem item in e.NewItems)
                    item.PropertyChanged += CartItem_PropertyChanged;
            if (e.OldItems != null)
                foreach (CartItem item in e.OldItems)
                    item.PropertyChanged -= CartItem_PropertyChanged;
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

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool shouldSelect = CartSession.Items.Any(item => !item.IsSelected);
            foreach (CartItem item in CartSession.Items) item.IsSelected = shouldSelect;
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void ItemSelectionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void IncreaseQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item }) item.Quantity += 1;
            UpdateTotal();
        }

        private void DecreaseQuantityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item }) item.Quantity -= 1;
            UpdateTotal();
        }

        private void DescriptionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock { DataContext: CartItem clickedItem }) return;
            bool shouldExpand = !clickedItem.IsDescriptionExpanded;
            CollapseAllDescriptions();
            clickedItem.IsDescriptionExpanded = shouldExpand;
            e.Handled = true;
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e) => CollapseAllDescriptions();

        private void CollapseAllDescriptions()
        {
            foreach (CartItem item in CartSession.Items) item.IsDescriptionExpanded = false;
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item }) CartSession.RemoveBook(item.BookID);
            UpdateSelectAllState();
            UpdateTotal();
        }

        private void UpdateSelectAllState()
        {
            SelectAllButton.Content = CartSession.Items.Count == 0
                ? "Выбрать все"
                : (CartSession.Items.All(item => item.IsSelected) ? "Снять все" : "Выбрать все");
        }

        private void UpdateTotal()
        {
            decimal selectedTotal = CartSession.Items.Where(item => item.IsSelected).Sum(item => item.TotalPrice);
            SelectedTotalText.Text = $"{selectedTotal:N0} ₽";
        }

        private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UserSession.UserId <= 0)
                {
                    new ErrorDialog("Сначала войдите в аккаунт для оформления заказа").ShowDialog();
                    return;
                }

                var selected = CartSession.Items.Where(i => i.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    new ErrorDialog("Выберите книги для заказа").ShowDialog();
                    return;
                }

                AddressDialog dialog = new() { Owner = this };
                if (dialog.ShowDialog() != true || dialog.Address == null) return;

                int orderId = DatabaseService.CreateOrder(UserSession.UserId, dialog.Address, selected);

                foreach (var item in selected.ToList())
                {
                    CartSession.RemoveBook(item.BookID);
                }

                MessageBox.Show("Заказ сформирован. Доставка ожидается от двух дней, вам позвонит курьер.",
                    "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("CartWindow.PlaceOrderButton_Click", ex);
            }
        }
    }
}
