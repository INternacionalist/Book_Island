using System.Windows;

namespace WpfAppBookStore
{
    public partial class AddressDialog : Window
    {
        public AddressInfo? Address { get; private set; }

        public AddressDialog()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CityBox.Text) || string.IsNullOrWhiteSpace(DistrictBox.Text) ||
                string.IsNullOrWhiteSpace(StreetBox.Text) || string.IsNullOrWhiteSpace(HouseBox.Text))
            {
                new ErrorDialog("Заполните обязательные поля адреса").ShowDialog();
                return;
            }

            Address = new AddressInfo
            {
                City = CityBox.Text.Trim(),
                District = DistrictBox.Text.Trim(),
                Street = StreetBox.Text.Trim(),
                House = HouseBox.Text.Trim(),
                Apartment = ApartmentBox.Text.Trim(),
                Intercom = IntercomBox.Text.Trim(),
                Floor = FloorBox.Text.Trim()
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
