using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace smIRCL
{
    public class IrcController
    {
        #region Public Properties

        public IrcConnector Connector { get; internal set; }
        public string Nick { get; internal set; }
        public string UserName { get; internal set; }
        public string RealName { get; internal set; }
        public string HostMask { get; internal set; }
        public string Host { get; internal set; }


        #endregion

        #region Public Command Handling

        public delegate void IrcMessageHandler(IrcConnector client, IrcController controller, IrcMessage message);
        public Dictionary<string, IrcMessageHandler> Handlers = new Dictionary<string, IrcMessageHandler>();

        #endregion

        #region Public Events

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

        #region Private Properties

        private string _unconfirmedNick;

        private bool _expectingTermination;

        #endregion

        public IrcController(IrcConnector connector, bool registerInternalHandlers = true)
        {
            Connector = connector ?? throw new ArgumentNullException(nameof(connector));

            if (registerInternalHandlers)
            {
                connector.Connected += ConnectorOnConnected;
                connector.MessageReceived += ConnectorOnMessageReceived;

                Handlers.Add("PING", OnPing);
                Handlers.Add(Numerics.RPL_MYINFO, OnWelcomeEnd);
                Handlers.Add("ERROR", OnUnrecoverableError);
                Handlers.Add("NICK", OnNickSet);
                Handlers.Add(Numerics.RPL_WHOREPLY, OnWhoReply);

                Handlers.Add(Numerics.ERR_NONICKNAMEGIVEN, OnNickError);
                Handlers.Add(Numerics.ERR_ERRONEUSNICKNAME, OnNickError);
                Handlers.Add(Numerics.ERR_NICKNAMEINUSE, OnNickError);
                Handlers.Add(Numerics.ERR_NICKCOLLISION, OnNickError);
            }
        }

        #region Private Methods

        private void CheckOperationValid()
        {
            if (Connector == null || Connector.IsDisposed || !Connector.IsConnected)
            {
                throw new InvalidOperationException("Connector not available");
            }
        }

        #endregion

        #region Public Methods

        public void Quit(string quitMessage = "Client quit")
        {
            _expectingTermination = true;
            Connector.Transmit($"QUIT :{quitMessage}");
            Connector.Dispose();
        }

        public void SetNick(string newNick)
        {
            Connector.Transmit($"NICK :{newNick}");
        }

        #endregion

        #region Connector Handlers

        private void ConnectorOnConnected()
        {
            //See https://modern.ircdocs.horse/index.html#connection-registration
            //TODO If password for connection is set, send first.
            _unconfirmedNick = Connector.Config.Nick;
            SetNick(Connector.Config.Nick);
            Connector.Transmit($"USER {Connector.Config.UserName} 0 * :{Connector.Config.RealName}");
            //TODO If SASL is enabled, authenticate using it
        }

        private void ConnectorOnMessageReceived(string rawMessage, IrcMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message.Command == "ERROR" && _expectingTermination)
            {
                return;
            }

            CheckOperationValid();

            foreach (KeyValuePair<string, IrcMessageHandler> ircMessageHandler in Handlers.Where(h => h.Key == message.Command))
            {
                new Task(() => ircMessageHandler.Value.Invoke(Connector, this, message)).Start();
            }
        }

        #endregion

        #region Handlers

        private void OnPing(IrcConnector client, IrcController controller, IrcMessage message)
        {
            Connector.Transmit($"PONG :{message.Parameters[0]}");
        }

        private void OnUnrecoverableError(IrcConnector client, IrcController controller, IrcMessage message)
        {
            Connector.Dispose();
        }

        private void OnNickError(IrcConnector client, IrcController controller, IrcMessage message)
        {
            if (Nick == null)
            {
                if (Connector.Config.AlternativeNicks.Count > 0)
                {
                    _unconfirmedNick = Connector.Config.AlternativeNicks.Dequeue();
                    Connector.Transmit($"NICK {_unconfirmedNick}");
                }
                else
                {
                    Quit("Unable to find a usable Nick");
                }
            }
        }

        private void OnNickSet(IrcConnector client, IrcController controller, IrcMessage message)
        {
            if (message.SourceNick == Nick) //Minimum requirement of a source is Nick which is always unique
            {
                Nick = message.Parameters[0];
                Connector.Transmit($"WHO :{Nick}");
            }
            else
            {
                //TODO update Nick of internally kept user
            }
        }

        private void OnWelcomeEnd(IrcConnector client, IrcController controller, IrcMessage message)
        {
            Nick = _unconfirmedNick;
            _unconfirmedNick = null;
            Connector.Transmit($"WHO :{Nick}");
        }

        private void OnWhoReply(IrcConnector client, IrcController controller, IrcMessage message)
        {
            if (message.Parameters[5] == Nick)
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
            }
            else
            {
                //TODO update WHO details of internally kept user
            }
        }

        #endregion
    }
}
