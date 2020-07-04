using System.Collections.Generic;

namespace smIRCL.Constants
{
    public static class GeneralConstants
    {
        /// <summary>
        /// Elements of IRCv3 Tags when escaping
        /// </summary>
        public static readonly Dictionary<string, string> TagEscapeDictionary = new Dictionary<string, string>
        {
            {@"\:", ";"},
            {@"\s", " "},
            {@"\\", @"\"},
            {@"\r", "\r"},
            {@"\n", "\n"}
        };

        /// <summary>
        /// The IRC lowercase mapping as described in in RFC1459
        /// </summary>
        public static Dictionary<char, char> IrcLowers = new Dictionary<char, char>
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
    }
}
