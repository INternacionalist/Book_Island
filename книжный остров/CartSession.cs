using System;
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
        private bool isDescriptionExpanded;

        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ImageSource? CoverImage { get; set; }

        public int Quantity
        {
            get => quantity;
            set
            {
                int normalizedValue = value < 1 ? 1 : value;
                if (quantity == normalizedValue) return;
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
                if (isSelected == value) return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsDescriptionExpanded
        {
            get => isDescriptionExpanded;
            set
            {
                if (isDescriptionExpanded == value) return;
                isDescriptionExpanded = value;
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
            if (propertyName is nameof(Quantity) or nameof(IsSelected))
            {
                CartSession.PersistItem(this);
            }
        }
    }

    public static class CartSession
    {
        public static ObservableCollection<CartItem> Items { get; } = new();

        static CartSession()
        {
            Items.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (CartItem item in e.NewItems)
                    {
                        PersistItem(item);
                    }
                }
            };
        }

        public static void LoadForUser(int userId, System.Collections.Generic.List<MainWindow.Book> books)
        {
            Items.Clear();
            if (userId <= 0) return;

            try
            {
                foreach (CartItem item in DatabaseService.LoadUserCart(userId, books))
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                DbLogger.LogError("CartSession.LoadForUser", ex);
            }
        }

        public static bool ContainsBook(int bookId) => Items.Any(item => item.BookID == bookId);

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
                Description = book.Description,
                Price = book.Price,
                CoverImage = book.CoverImage,
                Quantity = 1,
                IsSelected = true
            });

            if (UserSession.UserId > 0)
            {
                try { DatabaseService.IncrementCartAdds(book.BookID); } catch (Exception ex) { DbLogger.LogError("CartSession.AddBook", ex); }
            }
        }

        public static void RemoveBook(int bookId)
        {
            CartItem? existingItem = Items.FirstOrDefault(item => item.BookID == bookId);
            if (existingItem == null) return;

            Items.Remove(existingItem);
            if (UserSession.UserId > 0)
            {
                try { DatabaseService.DeleteCartItem(UserSession.UserId, bookId); } catch (Exception ex) { DbLogger.LogError("CartSession.RemoveBook", ex); }
            }
        }

        public static void PersistItem(CartItem item)
        {
            if (UserSession.UserId <= 0) return;
            try { DatabaseService.UpsertCartItem(UserSession.UserId, item); } catch (Exception ex) { DbLogger.LogError("CartSession.PersistItem", ex); }
        }
    }
}
