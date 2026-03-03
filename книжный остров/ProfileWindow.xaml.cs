using System.Windows;

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
            UserIdText.Text = UserSession.UserId > 0 ? UserSession.UserId.ToString() : "—";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
