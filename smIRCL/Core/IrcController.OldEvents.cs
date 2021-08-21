using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    public partial class IrcController
    {
        /// <summary>
        /// An IRC message handler for commands and numerics
        /// </summary>
        /// <param name="connector">The connector which fired the message</param>
        /// <param name="controller">The controller handling the message</param>
        /// <param name="message">The message received</param>
        public delegate void IrcMessageHandler(IrcController controller, IrcMessage message);
        
        /// <summary>
        /// The handler for disconnects
        /// </summary>
        /// <param name="controller">The controller who's connector died</param>
        public delegate void ControllerDisconnectedHandler(IrcController controller);
        
        /// <summary>
        /// The handler for readying
        /// </summary>
        /// <param name="controller">The controller that is now ready</param>
        public delegate void ControllerReadyHandler(IrcController controller);

        /// <summary>
        /// The collection of handlers which handle commands and numerics
        /// </summary>
        public IrcHandlerList Handlers = new();

        /// <summary>
        /// Fired when a PING is received
        /// </summary>
        public event IrcMessageHandler Ping;


        /// <summary>
        /// Fired when the internal connector dies
        /// </summary>
        public event ControllerDisconnectedHandler Disconnected;
        
        /// <summary>
        /// Fired when the controller is ready
        /// </summary>
        public event ControllerReadyHandler Ready;
    }
}