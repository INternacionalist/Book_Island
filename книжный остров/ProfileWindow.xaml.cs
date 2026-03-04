using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfAppBookStore
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            UserNameText.Text = string.IsNullOrWhiteSpace(UserSession.UserName) ? "Гость" : UserSession.UserName;
            LastNameText.Text = string.IsNullOrWhiteSpace(UserSession.LastName) ? "—" : UserSession.LastName;
            PhoneText.Text = string.IsNullOrWhiteSpace(UserSession.PhoneNumber) ? "—" : UserSession.PhoneNumber;
            string userIdText = UserSession.UserId > 0 ? UserSession.UserId.ToString() : "—";
            string registrationDateText = string.IsNullOrWhiteSpace(UserSession.RegistrationDateText) ? "—" : UserSession.RegistrationDateText;
            UserIdText.Text = $"{userIdText} ({registrationDateText})";
            AddressText.Text = LoadAddress();
        }

        private string LoadAddress()
        {
            if (UserSession.UserId <= 0) return "—";
            try
            {
                AddressInfo? address = DatabaseService.GetUserAddress(UserSession.UserId);
                return address == null ? "—" : address.AsSingleLine();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("ProfileWindow.LoadAddress", ex);
                return "—";
            }
        }


        private void AddressCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UserSession.UserId <= 0) return;
            try
            {
                AddressDialog dialog = new(DatabaseService.GetUserAddress(UserSession.UserId)) { Owner = this };
                if (dialog.ShowDialog() != true || dialog.Address == null) return;
                DatabaseService.SaveUserAddress(UserSession.UserId, dialog.Address);
                AddressText.Text = dialog.Address.AsSingleLine();
                new SuccessDialog("Адрес обновлен").ShowDialog();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("ProfileWindow.AddressCard_MouseDoubleClick", ex);
            }
            e.Handled = true;
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void CopyCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string textBlockName }) return;
            if (FindName(textBlockName) is not TextBlock textBlock || string.IsNullOrWhiteSpace(textBlock.Text)) return;
            Clipboard.SetText(textBlock.Text);
            new SuccessDialog("Скопировано в буфер обмена").ShowDialog();
        }
    }
}
