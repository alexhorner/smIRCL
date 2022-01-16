using System;
using System.Linq;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.EventArgs
{
    public class ChannelNoticeEventArgs : System.EventArgs
    {
        public readonly IrcUser Author;
        public readonly IrcChannel Channel;
        public readonly string Content;
        
        public ChannelNoticeEventArgs(IrcController source, IrcMessage message)
        {
            if (message.Command.ToIrcLower() != "notice" || !source.IsValidChannelName(message.Parameters[0])) throw new ArgumentException("Not a channel NOTICE", nameof(message));
            
            Author = source.Users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user) ? user : null;
            Channel = source.Channels.TryGetValue(message.Parameters[0].ToIrcLower(), out IrcChannel channel) ? channel : null;
            Content = message.Parameters[1];
        }
    }
}