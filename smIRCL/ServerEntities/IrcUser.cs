using System;
using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    public class IrcUser
    {

        public string Nick { get; internal set; } = null;
        public string UserName { get; internal set; } = null;
        public string RealName { get; internal set; } = null;
        public string HostMask { get; internal set; } = null;
        public string Host { get; internal set; } = null;
        public string Away { get; set; } = null; //TODO implement away and IRCv3
        public List<string> MutualChannels { get; set; }
        public DateTime? LastDirectMessage { get; internal set; } = null;
    }
}
