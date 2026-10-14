namespace EatMeLibrary
{
    /// <summary>
    /// Player names are 1 to 20 ASCII letters, digits, '.', '-' or '_'. Bot names don't have to follow these rules.
    /// </summary>
    public static class PlayerNameRules
    {
        public const int MIN_LENGTH = 1;
        public const int MAX_LENGTH = 20;

        public static bool IsValid(string name)
        {
            if (name == null || name.Length < MIN_LENGTH || name.Length > MAX_LENGTH)
                return false;

            foreach (var c in name)
                if (!IsAllowedCharacter(c))
                    return false;

            return true;
        }

        public static bool IsAllowedCharacter(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c == '.' || c == '-' || c == '_';
        }
    }
}
