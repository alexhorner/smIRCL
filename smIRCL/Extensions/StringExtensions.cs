using smIRCL.Constants;

namespace smIRCL.Extensions
{
    public static class StringExtensions
    {
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
                retNick += GeneralConstants.IrcLowers.TryGetValue(c, out char cLower) ? cLower : c;
            }

            return retNick;
        }
    }
}
