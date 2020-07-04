using System.Collections.Generic;

namespace smIRCL.Extensions
{
    public static class StringExtensions
    {
        private static Dictionary<char, char> IrcLowers = new Dictionary<char, char>
        {
            { 'A', 'a' },
            { 'B', 'b' },
            { 'C', 'c' },
            { 'D', 'd' },
            { 'E', 'e' },
            { 'F', 'f' },
            { 'G', 'g' },
            { 'H', 'h' },
            { 'I', 'i' },
            { 'J', 'j' },
            { 'K', 'k' },
            { 'L', 'l' },
            { 'M', 'm' },
            { 'N', 'n' },
            { 'O', 'o' },
            { 'P', 'p' },
            { 'Q', 'q' },
            { 'R', 'r' },
            { 'S', 's' },
            { 'T', 't' },
            { 'U', 'u' },
            { 'V', 'v' },
            { 'W', 'w' },
            { 'X', 'x' },
            { 'Y', 'y' },
            { 'Z', 'z' },

            { '[', '{' },
            { ']', '}' },
            { '\\', '|' },
            { '~', '^' }
        };

        /// <summary>
        /// Converts strings to IRC lowercase as described in RFC1459
        /// </summary>
        /// <param name="str">The string to convert to IRC lowercase</param>
        /// <returns>The IRC lowercased string</returns>
        public static string ToIrcLower(this string str)
        {
            string retNick = "";

            foreach (char c in str)
            {
                retNick += IrcLowers.TryGetValue(c, out char cLower) ? cLower : c;
            }

            return retNick;
        }
    }
}
