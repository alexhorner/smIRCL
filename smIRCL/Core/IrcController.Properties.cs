using System.Collections.Generic;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    public partial class IrcController
    {

        /// <summary>
        /// The connector for handling and accessing the IRC server connection
        /// </summary>
        public IrcConnector Connector { get; internal set; }
        

        /// <summary>
        /// A list of Users who are within scope of the client
        /// </summary>
        public readonly List<IrcUser> Users = new List<IrcUser>(); //TODO externally readonly collection

        /// <summary>
        /// A list of channels currently joined by the client
        /// </summary>
        public readonly List<IrcChannel> Channels = new List<IrcChannel>(); //TODO externally readonly collection
        

        /// <summary>
        /// The Nick of the client
        /// </summary>
        public string Nick { get; internal set; }

        /// <summary>
        /// The User name of the client
        /// </summary>
        public string UserName { get; internal set; }

        /// <summary>
        /// The Real name (GECOS) of the client
        /// </summary>
        public string RealName { get; internal set; }

        /// <summary>
        /// The Hostname or Mask of the client
        /// </summary>
        public string Host { get; internal set; }

        /// <summary>
        /// The Away status and message of the client
        /// </summary>
        public string Away { get; internal set; }


        /// <summary>
        /// The channel characters which are valid for the connected server
        /// </summary>
        public List<char> SupportedChannelTypes { get; internal set; } = new List<char>(); //TODO externally readonly collection

        /// <summary>
        /// The modes supported for channels for the connected server
        /// </summary>
        public SupportedChannelModes SupportedChannelModes { get; internal set; } = new SupportedChannelModes(); //TODO externally readonly collection

        /// <summary>
        /// The prefixes representing modes assignable to users in channels
        /// </summary>
        public List<KeyValuePair<char, char>> SupportedUserPrefixes { get; internal set; } = new List<KeyValuePair<char, char>>(); //TODO externally readonly collection

        /// <summary>
        /// The IRCv3 capabilities supported by the server
        /// </summary>
        public List<KeyValuePair<string, List<string>>> AvailableCapabilities { get; internal set; } = new List<KeyValuePair<string, List<string>>>(); //TODO externally readonly collection

        /// <summary>
        /// The IRCv3 capabilities negotiated and activated with the server
        /// </summary>
        public List<string> NegotiatedCapabilities { get; internal set; } = new List<string>(); //TODO externally readonly collection

        /// <summary>
        /// The MOTD of the connected server, if one is available
        /// </summary>
        public string ServerMotd { get; internal set; }
        
        /// <summary>
        /// Whether the client is ready to handle operations
        /// </summary>
        public bool IsReady => Connector != null && !Connector.IsDisposed && Connector.IsConnected && _sessionReady;
    }
}