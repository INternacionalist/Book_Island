using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfAppBookStore
{
    public partial class AddressDialog : Window
    {
        public AddressInfo? Address { get; private set; }

        public AddressDialog(AddressInfo? existingAddress = null)
        {
            InitializeComponent();
            if (existingAddress != null)
            {
                SetField(CityBox, existingAddress.City, "Город");
                SetField(DistrictBox, existingAddress.District, "Район");
                SetField(StreetBox, existingAddress.Street, "Улица");
                SetField(HouseBox, existingAddress.House, "Дом");
                SetField(ApartmentBox, existingAddress.Apartment, "Квартира (опционально)");
                SetField(IntercomBox, existingAddress.Intercom, "Домофон (опционально)");
                SetField(FloorBox, existingAddress.Floor, "Этаж (опционально)");
            }

            ApplyPlaceholderStyle();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            string city = GetValue(CityBox, "Город");
            string district = GetValue(DistrictBox, "Район");
            string street = GetValue(StreetBox, "Улица");
            string house = GetValue(HouseBox, "Дом");
            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(house))
            {
                new ErrorDialog("Заполните обязательные поля адреса").ShowDialog();
                return;
            }

            Address = new AddressInfo
            {
                City = city,
                District = district,
                Street = street,
                House = house,
                Apartment = GetValue(ApartmentBox, "Квартира (опционально)"),
                Intercom = GetValue(IntercomBox, "Домофон (опционально)"),
                Floor = GetValue(FloorBox, "Этаж (опционально)")
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Field_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox box || box.Tag is not string placeholder) return;
            if (box.Text != placeholder) return;
            box.Text = string.Empty;
            box.Foreground = Brushes.Black;
        }

        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox box || box.Tag is not string placeholder) return;
            if (!string.IsNullOrWhiteSpace(box.Text)) return;
            box.Text = placeholder;
            box.Foreground = Brushes.Gray;
        }

        private static void SetField(TextBox box, string value, string placeholder)
        {
            box.Tag = placeholder;
            box.Text = string.IsNullOrWhiteSpace(value) ? placeholder : value;
        }

        private void ApplyPlaceholderStyle()
        {
            ApplyColor(CityBox);
            ApplyColor(DistrictBox);
            ApplyColor(StreetBox);
            ApplyColor(HouseBox);
            ApplyColor(ApartmentBox);
            ApplyColor(IntercomBox);
            ApplyColor(FloorBox);
        }

        private static void ApplyColor(TextBox box)
        {
            if (box.Tag is not string placeholder) return;
            box.Foreground = box.Text == placeholder ? Brushes.Gray : Brushes.Black;
        }

        private static string GetValue(TextBox box, string placeholder)
        {
            return box.Text == placeholder ? string.Empty : box.Text.Trim();
        }
    }
}
