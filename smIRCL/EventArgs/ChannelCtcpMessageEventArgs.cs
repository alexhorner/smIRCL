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
            
            Author = source.Users.FirstOrDefault(user => user.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
            Channel = source.Channels.FirstOrDefault(channel => channel.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
            RawContent = message.Parameters[1].Trim('\x01');

            _parts = RawContent.Split(" ").ToList();

            AllArguments = RawContent.Substring(Command.Length + 1, RawContent.Length - (Command.Length + 1));
        }
    }
}