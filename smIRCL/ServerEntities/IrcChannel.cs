using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using smIRCL.Core;

namespace smIRCL.ServerEntities
{
    /// <summary>
    /// An IRC channel which exists on the connected IRC server
    /// </summary>
    public class IrcChannel
    {
        protected readonly IrcController SourceController;
        
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; internal set; }
        
        /// <summary>
        /// The topic of the channel
        /// </summary>
        public string Topic { get; internal set; }
        
        /// <summary>
        /// The modes and their parameters set on the channel
        /// </summary>
        public ReadOnlyCollection<KeyValuePair<char, string>> Modes => new(ModesInternal);
        /// <summary>
        /// The modes and their parameters set on the channel (internal)
        /// </summary>
        protected internal readonly List<KeyValuePair<char, string>> ModesInternal = new();
        
        /// <summary>
        /// The modes set on the client in this channel
        /// </summary>
        public ReadOnlyCollection<char> ClientModes => new(ClientModesInternal);
        /// <summary>
        /// The modes set on the client in this channel (internal)
        /// </summary>
        protected internal readonly List<char> ClientModesInternal = new();
        
        /// <summary>
        /// The users in the channel
        /// </summary>
        public ReadOnlyCollection<string> Users => new(UsersInternal);
        /// <summary>
        /// The users in the channel (internal)
        /// </summary>
        protected internal readonly List<string> UsersInternal = new();
        
        /// <summary>
        /// Whether the Users collection has been fully populated yet
        /// </summary>
        public bool UserCollectionComplete { get; internal set; }

        /// <summary>
        /// Instantiates a new channel
        /// </summary>
        /// <param name="sourceController">The controller the channel is associated with</param>
        /// <param name="name">The name of the channel</param>
        /// <param name="supportedChannelTypes">List of valid chars for server-specific checking from ISupport</param>
        public IrcChannel(IrcController sourceController, string name, List<char> supportedChannelTypes)
        {
            if (!IsValidName(name, supportedChannelTypes)) throw new ArgumentException("Not a valid channel name", nameof(name));
            
            SourceController = sourceController;
            Name = name;
        }
        
        /// <summary>
        /// Checks if a given string forms a valid channel name
        /// </summary>
        /// <param name="channelName">String to validate</param>
        /// <param name="checkerChars">Optional list of valid chars for server-specific checking from ISupport</param>
        /// <returns>Whether the string is a valid channel name</returns>
        public static bool IsValidName(string channelName, List<char> checkerChars = null)
        {
            checkerChars ??= Constants.GeneralConstants.Rcf1459ValidChannelChars;

            foreach (char c in checkerChars)
            {
                if (channelName.StartsWith(c.ToString()) && !channelName.Contains(" ")) return true;
            }

            return false;
        }

        /// <summary>
        /// Send a private message to the IRC channel
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            SourceController.SendPrivMsg(Name, message);
        }

        /// <summary>
        /// Send a private notice to the IRC channel
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendNotice(string message)
        {
            SourceController.SendNotice(Name, message);
        }
        
        /// <summary>
        /// Send a private CTCP message to the IRC channel
        /// </summary>
        /// <param name="fullCommand">The full command, including all arguments, to be sent</param>
        public void SendCtcp(string fullCommand)
        {
            SourceController.SendPrivMsg(Name, '\x01' + fullCommand + '\x01');
        }
    }
}
