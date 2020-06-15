using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    public class IrcUser
    {
        public string Nick { get; set; }
        public string RealName { get; set; }
        public string HostMask { get; set; }
        public List<IrcChannel> Channels { get; set; }
    }
}
