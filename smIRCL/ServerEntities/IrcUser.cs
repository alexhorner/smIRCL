using System;
using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    /// <summary>
    /// An IRC user who exists on the connected IRC server
    /// </summary>
    public class IrcUser
    {
        /// <summary>
        /// The Nick of the IRC user
        /// </summary>
        public string Nick { get; internal set; } = null;
        /// <summary>
        /// The User name of the IRC user
        /// </summary>
        public string UserName { get; internal set; } = null;
        /// <summary>
        /// The Real name (GECOS) of the IRC user
        /// </summary>
        public string RealName { get; internal set; } = null;
        /// <summary>
        /// The Host mask of the IRC user
        /// </summary>
        public string HostMask { get; internal set; } = null;
        /// <summary>
        /// The Hostname or Mask of the IRC user
        /// </summary>
        public string Host { get; internal set; } = null;
        /// <summary>
        /// The Away status and message of the IRC user
        /// </summary>
        public string Away { get; internal set; } = null;
        /// <summary>
        /// The account the IRC user has identified with (if known) using a service such as SASL or NickServ
        /// </summary>
        public string IdentifiedAccount { get; internal set; } = null;
        /// <summary>
        /// The channels shared between the IRC user and the client
        /// </summary>
        public List<string> MutualChannels { get; set; } = new List<string>();
        /// <summary>
        /// The modes the IRC user has in the associated mutual channel
        /// </summary>
        public List<KeyValuePair<string, List<char>>> MutualChannelModes { get; set; } = new List<KeyValuePair<string, List<char>>>();
        /// <summary>
        /// The time of the last Direct Message from or to the IRC user
        /// </summary>
        public DateTime? LastDirectMessage { get; internal set; } = null;
    }
}
