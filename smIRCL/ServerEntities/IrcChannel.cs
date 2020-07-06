using System;
using System.Collections.Generic;

namespace smIRCL.ServerEntities
{
    /// <summary>
    /// An IRC channel which exists on the connected IRC server
    /// </summary>
    public class IrcChannel
    {
        /// <summary>
        /// Instantiates a new channel
        /// </summary>
        /// <param name="name">The name of the channel</param>
        /// <param name="checkerChars">Optional list of valid chars for server-specific checking from ISupport</param>
        public IrcChannel(string name, List<char> checkerChars = null)
        {
            if (!IsValidName(name, checkerChars)) throw new ArgumentException("Not a valid channel name", nameof(name));
            Name = name;
        }

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
        public List<KeyValuePair<char, string>> Modes { get; internal set; } = new List<KeyValuePair<char, string>>();
        /// <summary>
        /// The modes set on the client in this channel
        /// </summary>
        public List<char> ClientModes { get; internal set; } = new List<char>();
        /// <summary>
        /// The users in the channel
        /// </summary>
        public List<string> Users { get; internal set; } = new List<string>();
        /// <summary>
        /// Whether the Users collection has been fully populated yet
        /// </summary>
        public bool UserCollectionComplete { get; internal set; }

        /// <summary>
        /// Checks if a given string forms a valid channel name
        /// </summary>
        /// <param name="channelName">String to validate</param>
        /// <param name="checkerChars">Optional list of valid chars for server-specific checking from ISupport</param>
        /// <returns>Whether the string is a valid channel name</returns>
        public static bool IsValidName(string channelName, List<char> checkerChars = null)
        {
            if (checkerChars == null) checkerChars = Constants.GeneralConstants.Rcf1459ValidChannelChars;

            foreach (char c in checkerChars)
            {
                if (channelName.StartsWith(c.ToString()) && !channelName.Contains(" ")) return true;
            }

            return false;
        }
    }
}
