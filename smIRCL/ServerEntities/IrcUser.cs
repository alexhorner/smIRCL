using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    public class IrcUser
    {
        
        public string Nick { get; internal set; }
        public string UserName { get; internal set; }
        public string RealName { get; internal set; }
        public string HostMask { get; internal set; }
        public string Host { get; internal set; }
        public List<string> MutualChannels { get; set; }
    }
}
