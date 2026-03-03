namespace WpfAppBookStore
{
    public static class UserSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static string UserName { get; private set; } = string.Empty;

        public static void Login(string userName)
        {
            UserName = userName;
            IsAuthenticated = true;
        }

        public static void Logout()
        {
            UserName = string.Empty;
            IsAuthenticated = false;
        }
    }
}
