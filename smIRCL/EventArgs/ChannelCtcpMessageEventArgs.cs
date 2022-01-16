using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.EventArgs
{
    public class ChannelCtcpMessageEventArgs : System.EventArgs
    {
        public readonly IrcUser Author;
        public readonly IrcChannel Channel;
        public readonly string RawContent;
        public string Command => _parts[0];
        public readonly string AllArguments;
        public ReadOnlyCollection<string> SeparatedArguments => new(_parts.Skip(1).ToList());

        private readonly List<string> _parts;
        
        public ChannelCtcpMessageEventArgs(IrcController source, IrcMessage message)
        {
            if (message.Command.ToIrcLower() != "privmsg" || !source.IsValidChannelName(message.Parameters[0])) throw new ArgumentException("Not a channel PRIVMSG", nameof(message));
            
            Author = source.Users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user) ? user : null;
            Channel = source.Channels.TryGetValue(message.Parameters[0].ToIrcLower(), out IrcChannel channel) ? channel : null;
            RawContent = message.Parameters[1].Trim('\x01');

            _parts = RawContent.Split(" ").ToList();

            AllArguments = _parts.Count > 1 ? RawContent.Substring(Command.Length + 1, RawContent.Length - (Command.Length + 1)) : "";
        }
    }
}