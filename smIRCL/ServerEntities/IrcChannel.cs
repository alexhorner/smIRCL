using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    public class IrcChannel
    {
        public IrcChannel(string name)
        {
            Name = name;
        }

        public string Name { get; internal set; }
        public string Topic { get; set; }
        public string Modes { get; set; } //TODO retrieve modes and mode changes
        public List<IrcChannelUser> Users { get; set; } //TODO Receive names then WHO channel to get user info
    }
}
