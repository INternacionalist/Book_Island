using Microsoft.Data.SqlClient;
using System;
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
            AddressText.Text = LoadAddress();
        }

        private string LoadAddress()
        {
            if (UserSession.UserId <= 0) return "—";
            try
            {
                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();
                const string q = @"SELECT AddressCity, AddressDistrict, AddressStreet, AddressHouse, AddressApartment
                                   FROM dbo.читатели WHERE id=@id";
                using SqlCommand cmd = new(q, conn);
                cmd.Parameters.AddWithValue("@id", UserSession.UserId);
                using SqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return "—";
                if (reader.IsDBNull(0)) return "—";
                string apt = reader[4]?.ToString() ?? string.Empty;
                return $"{reader[0]}, {reader[1]}, {reader[2]}, д. {reader[3]}" + (string.IsNullOrWhiteSpace(apt) ? string.Empty : $", кв. {apt}");
            }
            catch (Exception ex)
            {
                DbLogger.LogError("ProfileWindow.LoadAddress", ex);
                return "—";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void CopyCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string textBlockName }) return;
            if (FindName(textBlockName) is not TextBlock textBlock || string.IsNullOrWhiteSpace(textBlock.Text)) return;
            Clipboard.SetText(textBlock.Text);
            new SuccessDialog("Скопировано в буфер обмена").ShowDialog();
        }
    }
}
