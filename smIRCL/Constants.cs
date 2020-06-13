using System.Collections.Generic;

namespace smIRCL
{
    public static class Constants
    {
        public static readonly Dictionary<string, string> TagEscapeDictionary = new Dictionary<string, string>
        {
            {@"\:", ";"},
            {@"\s", " "},
            {@"\\", @"\"},
            {@"\r", "\r"},
            {@"\n", "\n"}
        };
    }
}
