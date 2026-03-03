using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Media;

namespace WpfAppBookStore
{
    public partial class LoginWindow : Window
    {
        private string connStr = @"Server=144.31.48.85,1433;Database=книжный остров;User Id=sa;Password=Database33;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OpenRegistrationBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegistrationPanel.Visibility = Visibility.Visible;
            Title = "Регистрация";
        }

        private void BackToLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            RegistrationPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            Title = "Вход в систему";
        }

        // === ЛОГИКА ВХОДА (LOGIN) ===
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = UserLogin.Text.Trim();
            string pass = UserPass.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                new ErrorDialog("Введите логин и пароль!").ShowDialog();
                UserLogin.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT TOP 1 [id], [Login], [Фамилия], [номер телефона] FROM [dbo].[читатели] WHERE [Login]=@login AND [Password]=@pass";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@pass", pass);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idOrdinal = reader.GetOrdinal("id");
                                int loginOrdinal = reader.GetOrdinal("Login");
                                int lastNameOrdinal = reader.GetOrdinal("Фамилия");
                                int phoneOrdinal = reader.GetOrdinal("номер телефона");

                                string dbLogin = reader.IsDBNull(loginOrdinal) ? login : reader.GetString(loginOrdinal);
                                string dbLastName = reader.IsDBNull(lastNameOrdinal) ? string.Empty : reader.GetString(lastNameOrdinal);
                                string dbPhone = reader.IsDBNull(phoneOrdinal) ? string.Empty : reader.GetString(phoneOrdinal);
                                int userId = reader.IsDBNull(idOrdinal) ? 0 : reader.GetInt32(idOrdinal);

                                new SuccessDialog("Вы успешно вошли!").ShowDialog();
                                UserSession.Login(dbLogin, dbLastName, dbPhone, userId);
                                DialogResult = true;
                                Close();
                            }
                            else
                            {
                                new ErrorDialog("Такого пользователя нет\nили пароль неверный.").ShowDialog();
                                UserLogin.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                new ErrorDialog($"SQL ОШИБКА:\n{sqlEx.Message}").ShowDialog();
            }
            catch (Exception ex)
            {
                new ErrorDialog($"ОШИБКА:\n{ex.Message}").ShowDialog();
            }
        }

        // === ЛОГИКА РЕГИСТРАЦИИ (REGISTER) ===
        private void RegBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = RegName.Text.Trim();
            string lastName = RegLastName.Text.Trim();
            string phoneNumber = RegPhone.Text.Trim();
            string pass = RegPass.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(pass))
            {
                new ErrorDialog("Заполните все поля регистрации!").ShowDialog();
                return;
            }

            if (login.Length < 3 || pass.Length < 3)
            {
                new ErrorDialog("Логин и пароль должны быть\nдлиннее 3 символов!").ShowDialog();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM [dbo].[читатели] WHERE [Login]=@login";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            new ErrorDialog("Такой пользователь\nуже существует!").ShowDialog();
                            return;
                        }
                    }

                    string insertQuery = @"INSERT INTO [dbo].[читатели] ([Login], [Password], [Фамилия], [номер телефона], [дата регистрации])
                                           VALUES (@login, @pass, @lastName, @phone, GETDATE());
                                           SELECT CAST(SCOPE_IDENTITY() as int);";

                    int newUserId;
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@login", login);
                        insertCmd.Parameters.AddWithValue("@pass", pass);
                        insertCmd.Parameters.AddWithValue("@lastName", lastName);
                        insertCmd.Parameters.AddWithValue("@phone", phoneNumber);
                        object? result = insertCmd.ExecuteScalar();
                        newUserId = result is int id ? id : 0;
                    }

                    new SuccessDialog("Регистрация прошла успешно!").ShowDialog();
                    UserSession.Login(login, lastName, phoneNumber, newUserId);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                new ErrorDialog($"ОШИБКА:\n{ex.Message}").ShowDialog();
            }
        }
    }
}
