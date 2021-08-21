using System;
using System.Linq;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.EventArgs
{
    public class ChannelMessageEventArgs : System.EventArgs
    {
        public readonly IrcUser Author;
        public readonly IrcChannel Channel;
        public readonly string Content;
        
        public ChannelMessageEventArgs(IrcController source, IrcMessage message)
        {
            if (message.Command.ToIrcLower() != "privmsg" || !source.IsValidChannelName(message.Parameters[0])) throw new ArgumentException("Not a channel PRIVMSG", nameof(message));
            
            Author = source.Users.FirstOrDefault(user => user.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
            Channel = source.Channels.FirstOrDefault(channel => channel.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
            Content = message.Parameters[1];
        }
    }
}