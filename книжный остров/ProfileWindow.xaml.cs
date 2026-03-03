using System.Windows;

namespace WpfAppBookStore
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            UserNameText.Text = string.IsNullOrWhiteSpace(UserSession.UserName) ? "Гость" : UserSession.UserName;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
