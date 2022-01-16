using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using smIRCL.Core;

namespace smIRCL.ServerEntities
{
    /// <summary>
    /// An IRC user who exists on the connected IRC server
    /// </summary>
    public class IrcUser
    {
        protected readonly IrcController SourceController;

        /// <summary>
        /// The Nick of the IRC user
        /// </summary>
        public string Nick { get; internal set; }
        
        /// <summary>
        /// The User name of the IRC user
        /// </summary>
        public string UserName { get; internal set; }
        
        /// <summary>
        /// The Real name (GECOS) of the IRC user
        /// </summary>
        public string RealName { get; internal set; }
        
        /// <summary>
        /// The Host mask of the IRC user
        /// </summary>
        public string HostMask { get; internal set; }
        
        /// <summary>
        /// The Hostname or Mask of the IRC user
        /// </summary>
        public string Host { get; internal set; }
        
        /// <summary>
        /// The Away status and message of the IRC user
        /// </summary>
        public string Away { get; internal set; }
        
        /// <summary>
        /// The account the IRC user has identified with (if known) using a service such as SASL or NickServ
        /// </summary>
        public string IdentifiedAccount { get; internal set; }
        
        /// <summary>
        /// The channels shared between the IRC user and the client
        /// </summary>
        public ReadOnlyCollection<string> MutualChannels => new(MutualChannelsInternal);
        /// <summary>
        /// The channels shared between the IRC user and the client (internal)
        /// </summary>
        protected internal readonly List<string> MutualChannelsInternal = new();
        
        /// <summary>
        /// The modes the IRC user has in the associated mutual channel
        /// </summary>
        public ReadOnlyCollection<KeyValuePair<string, List<char>>> MutualChannelModes => new(MutualChannelModesInternal);
        /// <summary>
        /// The modes the IRC user has in the associated mutual channel (internal)
        /// </summary>
        protected internal readonly List<KeyValuePair<string, List<char>>> MutualChannelModesInternal = new();
        
        /// <summary>
        /// The time of the last private message from or to the IRC user
        /// </summary>
        public DateTime? LastPrivateMessage { get; internal set; }
        
        /// <summary>
        /// Instantiates a new IRC user
        /// </summary>
        /// <param name="sourceController">The controller the user is associated with</param>
        public IrcUser(IrcController sourceController)
        {
            SourceController = sourceController;
        }
        
        /// <summary>
        /// Send a private message to the IRC user
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            SourceController.SendPrivMsg(Nick, message);
        }

        /// <summary>
        /// Send a private notice to the IRC user
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendNotice(string message)
        {
            SourceController.SendNotice(Nick, message);
        }
        
        /// <summary>
        /// Send a private CTCP message to the IRC user
        /// </summary>
        /// <param name="fullCommand">The full command, including all arguments, to be sent</param>
        public void SendCtcp(string fullCommand)
        {
            SourceController.SendPrivMsg(Nick, '\x01' + fullCommand + '\x01');
        }

        /// <summary>
        /// Send a CTCP response message
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendCtcpResponse(string message)
        {
            SourceController.SendCtcpResponse(Nick, message);
        }
    }
}
