namespace WpfAppBookStore
{
    public static class UserSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static string UserName { get; private set; } = string.Empty;
        public static string LastName { get; private set; } = string.Empty;
        public static string PhoneNumber { get; private set; } = string.Empty;
        public static int UserId { get; private set; }
        public static string RegistrationDateText { get; private set; } = string.Empty;

        public static void Login(string userName, string lastName = "", string phoneNumber = "", int userId = 0, string registrationDateText = "")
        {
            UserName = userName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            UserId = userId;
            RegistrationDateText = registrationDateText;
            IsAuthenticated = true;
        }

        public static void Logout()
        {
            UserName = string.Empty;
            LastName = string.Empty;
            PhoneNumber = string.Empty;
            UserId = 0;
            RegistrationDateText = string.Empty;
            IsAuthenticated = false;
        }
    }
}
