using System;
using System.Linq;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.EventArgs
{
    public class PrivateNoticeEventArgs : System.EventArgs
    {
        public readonly IrcUser Author;
        public readonly string Content;
        
        public PrivateNoticeEventArgs(IrcController source, IrcMessage message)
        {
            if (message.Command.ToIrcLower() != "notice" || source.IsValidChannelName(message.Parameters[0]) || source.Nick.ToIrcLower() == message.Parameters[0].ToIrcLower()) throw new ArgumentException("Not a private PRIVMSG", nameof(message));
            
            Author = source.Users.FirstOrDefault(user => user.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
            Content = message.Parameters[1];
        }
    }
}