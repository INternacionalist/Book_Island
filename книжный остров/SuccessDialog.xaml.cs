using System.Windows;

namespace WpfAppBookStore
{
    public partial class SuccessDialog : Window
    {
        public SuccessDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}