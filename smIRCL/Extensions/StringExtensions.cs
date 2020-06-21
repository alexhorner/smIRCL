using System.Collections.Generic;
using System.Linq;

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

        public static string ToIrcLower(this string nick)
        {
            string retNick = "";

            foreach (char c in nick)
            {
                retNick += IrcLowers.TryGetValue(c, out char cLower) ? cLower : c;
            }

            return retNick;
        }
    }
}
