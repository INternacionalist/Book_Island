using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Media;

namespace WpfAppBookStore
{
    public partial class LoginWindow : Window
    {
        private string connStr = DatabaseConfig.ConnectionString;
        private static readonly HttpClient HttpClient = new();

        public LoginWindow()
        {
            InitializeComponent();
            try { DatabaseService.EnsureInfrastructure(); } catch (Exception ex) { DbLogger.LogError("LoginWindow.ctor", ex); }
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

                    string registrationDateColumn = ResolveRegistrationDateColumn(conn);
                    string registrationDateSelect = string.IsNullOrWhiteSpace(registrationDateColumn)
                        ? "CAST(NULL AS datetime2) AS [RegistrationDate]"
                        : $"[{registrationDateColumn}] AS [RegistrationDate]";

                    string query = $"SELECT TOP 1 [id], [Login], [Фамилия], [номер телефона], {registrationDateSelect} FROM [dbo].[читатели] WHERE [Login]=@login AND [Password]=@pass";
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
                                int regDateOrdinal = reader.GetOrdinal("RegistrationDate");

                                string dbLogin = reader.IsDBNull(loginOrdinal) ? login : reader.GetString(loginOrdinal);
                                string dbLastName = reader.IsDBNull(lastNameOrdinal) ? string.Empty : reader.GetString(lastNameOrdinal);
                                string dbPhone = reader.IsDBNull(phoneOrdinal) ? string.Empty : reader.GetString(phoneOrdinal);
                                int userId = reader.IsDBNull(idOrdinal) ? 0 : reader.GetInt32(idOrdinal);
                                string dbRegDate = reader.IsDBNull(regDateOrdinal)
                                    ? string.Empty
                                    : reader.GetDateTime(regDateOrdinal).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

                                new SuccessDialog("Вы успешно вошли!").ShowDialog();
                                UserSession.Login(dbLogin, dbLastName, dbPhone, userId, dbRegDate);
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
        private async void RegBtn_Click(object sender, RoutedEventArgs e)
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
                DateTime moscowNow = await GetMoscowDateTimeAsync();
                string registrationDateText = moscowNow.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

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

                    string registrationDateColumn = ResolveRegistrationDateColumn(conn);
                    string insertQuery;

                    if (string.IsNullOrWhiteSpace(registrationDateColumn))
                    {
                        insertQuery = @"INSERT INTO [dbo].[читатели] ([Login], [Password], [Фамилия], [номер телефона])
                                        VALUES (@login, @pass, @lastName, @phone);
                                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    }
                    else
                    {
                        insertQuery = $@"INSERT INTO [dbo].[читатели] ([Login], [Password], [Фамилия], [номер телефона], [{registrationDateColumn}])
                                         VALUES (@login, @pass, @lastName, @phone, @registrationDate);
                                         SELECT CAST(SCOPE_IDENTITY() as int);";
                    }

                    int newUserId;
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@login", login);
                        insertCmd.Parameters.AddWithValue("@pass", pass);
                        insertCmd.Parameters.AddWithValue("@lastName", lastName);
                        insertCmd.Parameters.AddWithValue("@phone", phoneNumber);

                        if (!string.IsNullOrWhiteSpace(registrationDateColumn))
                        {
                            insertCmd.Parameters.AddWithValue("@registrationDate", moscowNow);
                        }

                        object? result = insertCmd.ExecuteScalar();
                        newUserId = result is int id ? id : 0;
                    }

                    new SuccessDialog("Регистрация прошла успешно!").ShowDialog();
                    UserSession.Login(login, lastName, phoneNumber, newUserId, registrationDateText);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                new ErrorDialog($"ОШИБКА:\n{ex.Message}").ShowDialog();
            }
        }

        private static async Task<DateTime> GetMoscowDateTimeAsync()
        {
            try
            {
                using HttpResponseMessage response = await HttpClient.GetAsync("https://worldtimeapi.org/api/timezone/Europe/Moscow");
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(json);

                if (document.RootElement.TryGetProperty("datetime", out JsonElement datetimeElement)
                    && DateTimeOffset.TryParse(datetimeElement.GetString(), out DateTimeOffset dto))
                {
                    return dto.LocalDateTime;
                }
            }
            catch
            {
                // fallback ниже
            }

            try
            {
                TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);
            }
            catch
            {
                TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);
            }
        }

        private static string ResolveRegistrationDateColumn(SqlConnection connection)
        {
            const string query = @"SELECT TOP 1 c.COLUMN_NAME
                                   FROM INFORMATION_SCHEMA.COLUMNS c
                                   WHERE c.TABLE_SCHEMA = N'dbo'
                                     AND c.TABLE_NAME = N'читатели'
                                     AND (c.COLUMN_NAME LIKE N'%дат%' OR c.COLUMN_NAME LIKE N'%рег%')
                                   ORDER BY CASE
                                              WHEN c.COLUMN_NAME = N'дата регистрации' THEN 0
                                              WHEN c.COLUMN_NAME = N'Дата регистрации' THEN 1
                                              WHEN c.COLUMN_NAME LIKE N'%дата%рег%' THEN 2
                                              ELSE 3
                                            END, c.ORDINAL_POSITION";

            using SqlCommand cmd = new(query, connection);
            object? value = cmd.ExecuteScalar();
            return value?.ToString() ?? string.Empty;
        }
    }
}
