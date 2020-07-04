using System.Collections.Generic;

namespace smIRCL.Core
{
    /// <summary>
    /// The channel modes supported by the connected IRC server, split into their type groups
    /// </summary>
    public class SupportedChannelModes
    {
        public List<char> A { get; set; } = new List<char>();
        public List<char> B { get; set; } = new List<char>();
        public List<char> C { get; set; } = new List<char>();
        public List<char> D { get; set; } = new List<char>();
    }
}
