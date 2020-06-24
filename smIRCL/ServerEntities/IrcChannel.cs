using System;
using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    public class IrcChannel
    {
        public IrcChannel(string name)
        {
            if (!IsValidName(name)) throw new ArgumentException("Not a valid channel name", nameof(name));
            Name = name;
        }

        public string Name { get; internal set; }
        public string Topic { get; internal set; }
        public string Modes { get; internal set; } //TODO retrieve modes and mode changes
        public List<string> Users { get; internal set; } = new List<string>();
        public bool UserCollectionComplete { get; internal set; }

        public static bool IsValidName(string channelName)
        {
            if ((channelName.StartsWith("&") ||
                channelName.StartsWith("#") ||
                channelName.StartsWith("+") ||
                channelName.StartsWith("!")) &&
                !channelName.Contains(" "))
            {
                return true;
            }

            return false;
        }
    }
}
