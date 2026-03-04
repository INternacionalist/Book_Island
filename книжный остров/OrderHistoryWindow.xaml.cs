using System;
using System.Windows;

namespace WpfAppBookStore
{
    public partial class OrderHistoryWindow : Window
    {
        public OrderHistoryWindow()
        {
            InitializeComponent();
            try
            {
                OrdersGrid.ItemsSource = UserSession.UserId > 0 ? DatabaseService.GetOrdersByUser(UserSession.UserId) : null;
            }
            catch (Exception ex)
            {
                DbLogger.LogError("OrderHistoryWindow.ctor", ex);
            }
        }
    }
}
