using System;
using System.Collections.Generic;
using System.Linq;
using smIRCL.Enums;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    public partial class IrcController
    {
        /// <summary>
        /// Send initial commands after connection establishment to set up the session
        /// </summary>
        private void ConnectorOnConnected()
        {
            //See https://modern.ircdocs.horse/index.html#connection-registration
            
            //Configure the capability processing steps and order
            _capabilitySteps.Enqueue(CapabilityStep.Start);
            _capabilitySteps.Enqueue(CapabilityStep.RequestCapabilities);
            if (Connector.Config.AuthMode == AuthMode.SASL) _capabilitySteps.Enqueue(CapabilityStep.RequestSaslAuthentication);
            _capabilitySteps.Enqueue(CapabilityStep.End);
            
            DoNextCapabilityCompletionStep(); //Start the connection supporting IRCv3 capabilities
            
            if (!string.IsNullOrWhiteSpace(Connector.Config.ServerPassword)) Connector.Transmit($"PASS :{Connector.Config.ServerPassword}"); //Send the server password where applicable
            
            _unconfirmedNick = Connector.Config.Nick; //Keep track of the nick we are looking to use
            SetNick(Connector.Config.Nick); //Initial nick request
            
            Connector.Transmit($"USER {Connector.Config.UserName} 0 * :{Connector.Config.RealName}"); //Send initial user information
        }
        
        /// <summary>
        /// Invokes the public facing disconnected event
        /// </summary>
        private void ConnectorOnDisconnected()
        {
            Disconnected?.Invoke(this);
        }
        
        /// <summary>
        /// Process an incoming message from the server
        /// </summary>
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
                ircMessageHandler.Value.Invoke(this, message);
            }
        }
    }
}