namespace WpfAppBookStore
{
    public static class UserSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static string UserName { get; private set; } = string.Empty;
        public static string LastName { get; private set; } = string.Empty;
        public static string PhoneNumber { get; private set; } = string.Empty;
        public static int UserId { get; private set; }

        public static void Login(string userName, string lastName = "", string phoneNumber = "", int userId = 0)
        {
            UserName = userName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            UserId = userId;
            IsAuthenticated = true;
        }

        public static void Logout()
        {
            UserName = string.Empty;
            LastName = string.Empty;
            PhoneNumber = string.Empty;
            UserId = 0;
            IsAuthenticated = false;
        }
    }
}
