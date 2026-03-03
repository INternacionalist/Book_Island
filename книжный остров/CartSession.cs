using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ImageSource? CoverImage { get; set; }

        public int Quantity
        {
            get => quantity;
            set
            {
                int normalizedValue = value < 1 ? 1 : value;
                if (quantity == normalizedValue)
                {
                    return;
                }

                quantity = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasBulkDiscount));
                OnPropertyChanged(nameof(DiscountedUnitPrice));
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(OriginalTotalPrice));
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                {
                    return;
                }

                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool HasBulkDiscount => Quantity >= 2;
        public decimal DiscountedUnitPrice => HasBulkDiscount ? Price * 0.9m : Price;
        public decimal TotalPrice => DiscountedUnitPrice * Quantity;
        public decimal OriginalTotalPrice => Price * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class CartSession
    {
        public static ObservableCollection<CartItem> Items { get; } = new();

        public static bool ContainsBook(int bookId)
        {
            return Items.Any(item => item.BookID == bookId);
        }

        public static void AddBook(MainWindow.Book book)
        {
            CartItem? existingItem = Items.FirstOrDefault(item => item.BookID == book.BookID);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                existingItem.IsSelected = true;
                return;
            }

            Items.Add(new CartItem
            {
                BookID = book.BookID,
                Title = book.Title,
                Author = book.Author,
                Price = book.Price,
                CoverImage = book.CoverImage,
                Quantity = 1,
                IsSelected = true
            });
        }

        public static void RemoveBook(int bookId)
        {
            CartItem? existingItem = Items.FirstOrDefault(item => item.BookID == bookId);
            if (existingItem == null)
            {
                return;
            }

            Items.Remove(existingItem);
        }
    }
}
