using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using smIRCL.Core;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.EventArgs
{
    public class PrivateCtcpMessageEventArgs : System.EventArgs
    {
        public readonly IrcUser Author;
        public readonly string RawContent;
        public string Command => _parts[0];
        public readonly string AllArguments;
        public ReadOnlyCollection<string> SeparatedArguments => new(_parts.Skip(1).ToList());

        private readonly List<string> _parts;
        
        public PrivateCtcpMessageEventArgs(IrcController source, IrcMessage message)
        {
            if (message.Command.ToIrcLower() != "privmsg" || source.IsValidChannelName(message.Parameters[0]) || source.Nick.ToIrcLower() != message.Parameters[0].ToIrcLower()) throw new ArgumentException("Not a private PRIVMSG", nameof(message));
            
            Author = source.Users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user) ? user : null;
            RawContent = message.Parameters[1].Trim('\x01');

            _parts = RawContent.Split(" ").ToList();

            AllArguments = _parts.Count > 1 ? RawContent.Substring(Command.Length + 1, RawContent.Length - (Command.Length + 1)) : "";
        }
    }
}