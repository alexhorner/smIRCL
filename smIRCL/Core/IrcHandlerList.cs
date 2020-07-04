using System.Collections.Generic;

namespace smIRCL.Core
{
    /// <summary>
    /// A collection of IRC message handlers for a controller
    /// </summary>
    public class IrcHandlerList : List<KeyValuePair<string, IrcController.IrcMessageHandler>>
    {
        /// <summary>
        /// Adds a message handler to the collection
        /// </summary>
        /// <param name="eventName">the IRC command or numeric the handler should respond to</param>
        /// <param name="handler">The handler to call when the named event is received</param>
        public void Add(string eventName, IrcController.IrcMessageHandler handler)
        {
            base.Add(new KeyValuePair<string, IrcController.IrcMessageHandler>(eventName, handler));
        }
    }
}
