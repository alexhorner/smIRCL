using System.Linq;
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
            if (str == null) return null;
            
            string retStr = "";

            foreach (char c in str)
            {
                retStr += GeneralConstants.IrcLowers.TryGetValue(c, out char cLower) ? cLower : c;
            }

            return retStr;
        }

        /// <summary>
        /// Converts strings to IRC uppercase as described in RFC1459
        /// </summary>
        /// <param name="str">The string to convert to IRC uppercase</param>
        /// <returns>The IRC uppercased string</returns>
        public static string ToIrcUpper(this string str)
        {
            string retStr = "";

            foreach (char c in str)
            {
                retStr += GeneralConstants.IrcLowers.Any(lwr => lwr.Value == c) ? GeneralConstants.IrcLowers.FirstOrDefault(lwr => lwr.Value == c).Key : c;
            }

            return retStr;
        }
    }
}
