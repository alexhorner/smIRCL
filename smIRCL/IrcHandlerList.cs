using System.Collections.Generic;

namespace smIRCL
{
    public class IrcHandlerList : List<KeyValuePair<string, IrcController.IrcMessageHandler>>
    {
        public void Add(string eventName, IrcController.IrcMessageHandler handler)
        {
            base.Add(new KeyValuePair<string, IrcController.IrcMessageHandler>(eventName, handler));
        }
    }
}
