using Microsoft.Data.SqlClient;
using System;

namespace WpfAppBookStore
{
    public static class DbLogger
    {
        public static void LogError(string source, Exception ex)
        {
            try
            {
                DatabaseService.EnsureInfrastructure();
                using SqlConnection conn = new(DatabaseConfig.ConnectionString);
                conn.Open();

                const string query = @"INSERT INTO dbo.ErrorLogs(Source, Message, StackTrace, CreatedAtUtc, UserId)
                                       VALUES (@source, @message, @stack, SYSUTCDATETIME(), @userId);";
                using SqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@source", source);
                cmd.Parameters.AddWithValue("@message", ex.Message);
                cmd.Parameters.AddWithValue("@stack", (object?)ex.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Последний рубеж: логгер не должен ломать приложение.
            }
        }
    }
}
