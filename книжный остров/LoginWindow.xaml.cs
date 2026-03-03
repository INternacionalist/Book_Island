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
                    
                    string query = "SELECT * FROM читатели WHERE Login=@login AND Password=@pass";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@pass", pass);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                new SuccessDialog("Вы успешно вошли!").ShowDialog();
                                this.Close();
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
            string login = UserLogin.Text.Trim();
            string pass = UserPass.Password.Trim();

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

                    // Проверяем существование
                    string checkQuery = "SELECT COUNT(*) FROM читатели WHERE Login=@login";
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

                    // Добавляем нового пользователя
                    string insertQuery = "INSERT INTO читатели (Login, Password) VALUES (@login, @pass)";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@login", login);
                        insertCmd.Parameters.AddWithValue("@pass", pass);
                        insertCmd.ExecuteNonQuery();
                    }

                    new SuccessDialog("Регистрация прошла успешно!").ShowDialog();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                new ErrorDialog($"ОШИБКА:\n{ex.Message}").ShowDialog();
            }
        }
    }
}