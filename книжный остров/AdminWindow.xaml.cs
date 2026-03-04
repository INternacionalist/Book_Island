using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppBookStore
{
    public partial class AdminWindow : Window
    {
        private string coverPath = string.Empty;
        private List<MainWindow.Book> allBooks = new();
        private List<(int Id, string Login)> users = new();

        public AdminWindow()
        {
            InitializeComponent();
            ReloadData();
        }

        private void ReloadData()
        {
            try
            {
                // Сбрасываем флаг чтобы инфраструктура перепроверилась
                DatabaseService.ResetEnsured();
                DatabaseService.EnsureInfrastructure();

                allBooks = DatabaseService.LoadBooks(false);
                BooksGrid.ItemsSource = null;
                BooksGrid.ItemsSource = allBooks;

                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("SELECT id, Login FROM dbo.читатели ORDER BY id DESC", conn);
                using SqlDataReader reader = cmd.ExecuteReader();
                users = new();
                while (reader.Read())
                    users.Add((reader.GetInt32(0), reader[1]?.ToString() ?? string.Empty));

                UsersGrid.ItemsSource = null;
                UsersGrid.ItemsSource = users.Select(u => new { u.Id, u.Login }).ToList();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("AdminWindow.ReloadData", ex);
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PickCover_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new() { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                coverPath = dlg.FileName;
                CoverPathText.Text = Path.GetFileName(coverPath);
            }
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleBox.Text) ||
                    string.IsNullOrWhiteSpace(GenreBox.Text) ||
                    string.IsNullOrWhiteSpace(PriceBox.Text))
                {
                    new ErrorDialog("Заполните название, жанр и цену").ShowDialog();
                    return;
                }

                byte[]? cover = File.Exists(coverPath) ? File.ReadAllBytes(coverPath) : null;

                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();
                string q = @"INSERT INTO dbo.Books(Title, Author, Price, Description, Genre, PublishYear, InStock, CoverImage)
                             VALUES(@t, @a, @p, @d, @g, YEAR(GETDATE()), 1, @img)";
                using SqlCommand cmd = new(q, conn);
                cmd.Parameters.AddWithValue("@t", TitleBox.Text.Trim());
                cmd.Parameters.AddWithValue("@a", string.IsNullOrWhiteSpace(AuthorBox.Text)
                    ? "Неизвестный автор" : AuthorBox.Text.Trim());
                cmd.Parameters.AddWithValue("@p", decimal.TryParse(PriceBox.Text, out decimal p) ? p : 0m);
                cmd.Parameters.AddWithValue("@d", DescriptionBox.Text.Trim());
                cmd.Parameters.AddWithValue("@g", GenreBox.Text.Trim());
                cmd.Parameters.AddWithValue("@img", (object?)cover ?? DBNull.Value);
                cmd.ExecuteNonQuery();

                // Очищаем поля после публикации
                TitleBox.Text = string.Empty;
                AuthorBox.Text = string.Empty;
                GenreBox.Text = string.Empty;
                PriceBox.Text = string.Empty;
                DescriptionBox.Text = string.Empty;
                coverPath = string.Empty;
                CoverPathText.Text = string.Empty;

                new SuccessDialog("Книга опубликована").ShowDialog();
                ReloadData();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("AdminWindow.Publish_Click", ex);
                MessageBox.Show($"Ошибка публикации:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int bookId }) return;
            if (MessageBox.Show("Точно хотите удалить книгу?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM dbo.Books WHERE BookID=@id", conn);
                cmd.Parameters.AddWithValue("@id", bookId);
                cmd.ExecuteNonQuery();
                ReloadData();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("AdminWindow.DeleteBook_Click", ex);
                MessageBox.Show($"Ошибка удаления:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int userId }) return;
            if (MessageBox.Show("Удалить пользователя?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();
                using SqlCommand cmd = new("DELETE FROM dbo.читатели WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
                ReloadData();
            }
            catch (Exception ex)
            {
                DbLogger.LogError("AdminWindow.DeleteUser_Click", ex);
                MessageBox.Show($"Ошибка удаления:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BookSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = BookSearchBox.Text.Trim();
            BooksGrid.ItemsSource = string.IsNullOrWhiteSpace(q)
                ? allBooks
                : allBooks.Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = UserSearchBox.Text.Trim();
            UsersGrid.ItemsSource = string.IsNullOrWhiteSpace(q)
                ? users.Select(u => new { u.Id, u.Login }).ToList()
                : users.Where(u => u.Login.Contains(q, StringComparison.OrdinalIgnoreCase))
                       .Select(u => new { u.Id, u.Login }).ToList();
        }
    }
}