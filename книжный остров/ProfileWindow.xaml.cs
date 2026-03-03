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
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string textBlockName })
            {
                return;
            }

            if (FindName(textBlockName) is not TextBlock textBlock || string.IsNullOrWhiteSpace(textBlock.Text))
            {
                return;
            }

            Clipboard.SetText(textBlock.Text);
            new SuccessDialog("Скопировано в буфер обмена").ShowDialog();
        }
    }
}
