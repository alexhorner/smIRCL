using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace smIRCL
{
    /// <summary>
    /// Controls the handling of IrcMessages from an IrcClient
    /// </summary>
    public class IrcController
    {
        public IrcClient Client { get; internal set; }

        /// <summary>
        /// The delegate representing a message handler
        /// </summary>
        /// <param name="client">The client to handle the message for</param>
        /// <param name="message">The IrcMessage to handle</param>
        public delegate void IrcMessageHandler(IrcClient client, IrcController controller, IrcMessage message);
        /// <summary>
        /// The handlers configured with this controller associated with the commands they handle
        /// </summary>
        public Dictionary<string, IrcMessageHandler> Handlers = new Dictionary<string, IrcMessageHandler>();

        /// <summary>
        /// The IrcClient this IrcController is attached to
        /// </summary>

        private string _unconfirmedNick;

        #region Events

        /// <summary>
        /// Fired when a PRIVMSG is received
        /// </summary>
        public event IrcMessageHandler PrivMsg;

        /// <summary>
        /// Fired when a NOTICE is received
        /// </summary>
        public event IrcMessageHandler Notice;

        /// <summary>
        /// Fired when a PING is received
        /// </summary>
        public event IrcMessageHandler Ping;

        #endregion

        public IrcController(bool registerInternalHandlers = true)
        {
            if (registerInternalHandlers)
            {
                Handlers.Add("PING", OnPing);
                Handlers.Add(Numerics.RPL_MYINFO, OnWelcomeEnd);
                Handlers.Add("ERROR", OnUnrecoverableError);

                Handlers.Add(Numerics.ERR_NONICKNAMEGIVEN, OnNickError);
                Handlers.Add(Numerics.ERR_ERRONEUSNICKNAME, OnNickError);
                Handlers.Add(Numerics.ERR_NICKNAMEINUSE, OnNickError);
                Handlers.Add(Numerics.ERR_NICKCOLLISION, OnNickError);
            }
        }

        /// <summary>
        /// Attach an IrcClient to this IrcController if one has not been attached at instantiation
        /// </summary>
        /// <param name="client">The IrcClient to be attached</param>
        public void AttachClient(IrcClient client)
        {
            if (Client != null) throw new InvalidOperationException("Cannot attach an IrcClient when one is already attached");
            Client = client;
            Client.RawMessageReceived += ClientOnRawMessageReceived;
            Client.Connected += ClientOnConnected;
        }

        /// <summary>
        /// Process and IrcMessage and take appropriate action
        /// </summary>
        /// <param name="rawMessage">The string which was parsed into and IrcMessage for message</param>
        /// <param name="message">The IrcMessage parsed from rawMessage</param>
        public void ClientOnRawMessageReceived(string rawMessage, IrcMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (CheckOperationValid(message.Command == "ERROR"))
            {
                foreach (KeyValuePair<string, IrcMessageHandler> ircMessageHandler in Handlers.Where(h => h.Key == message.Command))
                {
                    new Task(() => ircMessageHandler.Value.Invoke(Client, this, message)).Start();
                }
            }
        }

        private void ClientOnConnected()
        {
            //TODO If password for connection is set, send first.
            _unconfirmedNick = Client.ClientConfig.Nick;
            Client.Transmit($"NICK {Client.ClientConfig.Nick}");
            Client.Transmit($"USER {Client.ClientConfig.UserName} 0 * :{Client.ClientConfig.RealName}");
            //TODO If SASL is enabled, authenticate using it
        }

        private bool CheckOperationValid(bool bypassThrow = false)
        {
            if (Client == null || Client.IsDisposed || !Client.IsConnected)
            {
                if (!bypassThrow)
                {
                    throw new InvalidOperationException("Attached IrcClient is not available");
                }

                return false;
            }

            return true;
        }




        #region Handlers

        private void OnPing(IrcClient client, IrcController controller, IrcMessage message)
        {
            Client.Transmit($"PONG :{message.Parameters[0]}");
        }

        private void OnUnrecoverableError(IrcClient client, IrcController controller, IrcMessage message)
        {
            Client.Dispose();
        }

        private void OnNickError(IrcClient client, IrcController controller, IrcMessage message)
        {
            if (Client.Nick == null)
            {
                if (Client.ClientConfig.AlternativeNicks.Count > 0)
                {
                    _unconfirmedNick = Client.ClientConfig.AlternativeNicks.Dequeue();
                    Client.Transmit($"NICK {_unconfirmedNick}");
                }
                else
                {
                    Client.Quit("Unable to find a usable Nick");
                }
            }
        }

        private void OnNickSet(IrcClient client, IrcController controller, IrcMessage message)
        {
            Client.Nick = message.Parameters[0];
        }

        private void OnWelcomeEnd(IrcClient client, IrcController controller, IrcMessage message)
        {
            Client.Nick = _unconfirmedNick;
            _unconfirmedNick = null;
        }

        #endregion
    }
}
