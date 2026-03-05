namespace WpfAppBookStore
{
    public static class UserSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static bool IsAdmin { get; private set; }
        public static string UserName { get; private set; } = string.Empty;
        public static string LastName { get; private set; } = string.Empty;
        public static string PhoneNumber { get; private set; } = string.Empty;
        public static int UserId { get; private set; }
        public static string RegistrationDateText { get; private set; } = string.Empty;

        public static void Login(string userName, string lastName = "", string phoneNumber = "", int userId = 0, string registrationDateText = "", bool? isAdminOverride = null)
        {
            UserName = userName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            UserId = userId;
            RegistrationDateText = registrationDateText;
            if (isAdminOverride.HasValue)
            {
                IsAdmin = isAdminOverride.Value;
            }
            else
            {
                string normalized = userName.Trim().ToLowerInvariant();
                IsAdmin = normalized is "admin" or "administrator" or "джефри";
            }
            IsAuthenticated = true;
        }

        public static void Logout()
        {
            UserName = string.Empty;
            LastName = string.Empty;
            PhoneNumber = string.Empty;
            UserId = 0;
            RegistrationDateText = string.Empty;
            IsAdmin = false;
            IsAuthenticated = false;
        }
    }
}
