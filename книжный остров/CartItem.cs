using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WpfAppBookStore
{
    public class CartItem : INotifyPropertyChanged
    {
        private int quantity = 1;
        private bool isSelected = true;

        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public ImageSource? CoverImage { get; set; }

        public int Quantity
        {
            get => quantity;
            set
            {
                if (value == quantity || value < 1)
                {
                    return;
                }

                quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDiscount));
                OnPropertyChanged(nameof(CurrentPrice));
                OnPropertyChanged(nameof(OldPrice));
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value == isSelected)
                {
                    return;
                }

                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool HasDiscount => Quantity >= 2;
        public decimal CurrentPrice => HasDiscount ? decimal.Round(UnitPrice * 0.9m, 2) : UnitPrice;
        public decimal OldPrice => UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
