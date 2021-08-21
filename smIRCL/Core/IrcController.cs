using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using smIRCL.Constants;
using smIRCL.Enums;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    /// <summary>
    /// A controller which handles the ongoing state of an IRC server connection and allows control over that connection
    /// </summary>
    public partial class IrcController
    {
        private Timer _userGarbageCollectionTimer;

        private string _unconfirmedNick;

        private bool _expectingTermination;

        private bool _sessionReady;

        private readonly List<char> _trimmableUserPrefixes = new List<char>();

        private readonly Queue<CapabilityStep> _capabilitySteps = new Queue<CapabilityStep>();

        /// <summary>
        /// Instantiates a new controller
        /// </summary>
        /// <param name="connector">The connector for connecting to the IRC server and retrieving the configuration</param>
        /// <param name="registerInternalHandlers">Whether to register all the internal state handlers</param>
        public IrcController(IrcConnector connector, bool registerInternalHandlers = true)
        {
            Connector = connector ?? throw new ArgumentNullException(nameof(connector));
            connector.Connected += ConnectorOnConnected;
            connector.MessageReceived += ConnectorOnMessageReceived;
            connector.Disconnected += ConnectorOnDisconnected;

            _userGarbageCollectionTimer = new Timer(Connector.Config.DirectMessageHoldingPeriod.TotalMilliseconds);
            _userGarbageCollectionTimer.Elapsed += DoUserGarbageCollection;
            _userGarbageCollectionTimer.AutoReset = true;
            _userGarbageCollectionTimer.Enabled = true;

            if (registerInternalHandlers)
            {
                Handlers.Add("PING", PingHandler);
                Handlers.Add("PRIVMSG", PrivMsgHandler);
                Handlers.Add("NOTICE", NoticeHandler);
                Handlers.Add("ERROR", UnrecoverableErrorHandler);
                Handlers.Add("NICK", NickSetHandler);
                Handlers.Add("JOIN", JoinHandler);
                Handlers.Add("PART", PartHandler);
                Handlers.Add("KICK", KickHandler);
                Handlers.Add("QUIT", QuitHandler);
                Handlers.Add("TOPIC", TopicUpdateHandler);
                Handlers.Add("MODE", ModeHandler);
                Handlers.Add("CAP", CapabilityHandler);
                Handlers.Add("AUTHENTICATE", SaslHandler);
                Handlers.Add("AWAY", AwayNotifyHandler);
                Handlers.Add("CHGHOST", ChangeHostHandler);

                Handlers.Add(Numerics.RPL_SASLSUCCESS, SaslCompleteHandler);
                Handlers.Add(Numerics.RPL_LOGGEDOUT, SaslFailureHandler);
                Handlers.Add(Numerics.ERR_NICKLOCKED, SaslFailureHandler);
                Handlers.Add(Numerics.ERR_SASLFAIL, SaslFailureHandler);
                Handlers.Add(Numerics.ERR_SASLTOOLONG, SaslFailureHandler);
                Handlers.Add(Numerics.ERR_SASLABORTED, SaslFailureHandler);
                Handlers.Add(Numerics.ERR_SASLALREADY, SaslFailureHandler);

                Handlers.Add(Numerics.RPL_MYINFO, WelcomeEndHandler);
                Handlers.Add(Numerics.RPL_MOTD, MotdPartHandler);
                Handlers.Add(Numerics.RPL_ENDOFMOTD, MotdEndHandler);
                Handlers.Add(Numerics.ERR_NOMOTD, NoMotdHandler);
                Handlers.Add(Numerics.RPL_WHOREPLY, WhoReplyHandler);
                Handlers.Add(Numerics.RPL_WHOISUSER, WhoIsUserReplyHandler);
                Handlers.Add(Numerics.RPL_WHOISCHANNELS, WhoIsChannelsReplyHandler);
                Handlers.Add(Numerics.RPL_TOPIC, TopicInformHandler);
                Handlers.Add(Numerics.RPL_HOSTHIDDEN, HostMaskCloakHandler);
                Handlers.Add(Numerics.RPL_NAMREPLY, NamesReplyHandler);
                Handlers.Add(Numerics.RPL_ENDOFNAMES, NamesEndHandler);
                Handlers.Add(Numerics.RPL_CHANNELMODEIS, ChannelModesHandler);
                Handlers.Add(Numerics.RPL_ISUPPORT, ServerISupportHandler);

                Handlers.Add(Numerics.ERR_NONICKNAMEGIVEN, NickErrorHandler);
                Handlers.Add(Numerics.ERR_ERRONEUSNICKNAME, NickErrorHandler);
                Handlers.Add(Numerics.ERR_NICKNAMEINUSE, NickErrorHandler);
                Handlers.Add(Numerics.ERR_NICKCOLLISION, NickErrorHandler);
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

        private void DoUserGarbageCollection()
        {
            _users.RemoveAll(u => u.MutualChannels.Count == 0 && (u.LastPrivateMessage == null ||  u.LastPrivateMessage + Connector.Config.DirectMessageHoldingPeriod < DateTime.Now));
        }
        private void DoUserGarbageCollection(Object source, ElapsedEventArgs e)
        {
            DoUserGarbageCollection();
        }

        /// <summary>
        /// Determines what the next step is for completing capability negotiation and setup, and executes it
        /// </summary>
        private void DoNextCapabilityCompletionStep()
        {
            if (!_capabilitySteps.Any()) return;
            
            CapabilityStep nextStep = _capabilitySteps.Dequeue();
            
            switch (nextStep)
            {
                case CapabilityStep.Start:
                    Connector.Transmit("CAP LS :302");
                    break;
                
                case CapabilityStep.RequestCapabilities:
                    RequestCapabilities();
                    break;
                
                case CapabilityStep.RequestSaslAuthentication:
                    Connector.Transmit("AUTHENTICATE :PLAIN");
                    break;
                
                case CapabilityStep.End:
                    Connector.Transmit("CAP :END");
                    break;
            }
        }
        
        /// <summary>
        /// Based on available capabilities, request the desired capabilities
        /// </summary>
        private void RequestCapabilities()
        {
            string capabilityRequest = "";

            if (Connector.Config.AuthMode == AuthMode.SASL) //SASL is a special capability, added to the request separately
            {
                if (AvailableCapabilities.Any(acap => acap.Key.ToIrcLower() == "sasl")) //If SASL is available, add it
                {
                    capabilityRequest += "sasl";
                }
                else //If SASL is not available, we mustn't continue
                {
                    Quit("SASL required but not available");
                    return;
                }
            }

            foreach (string capability in Connector.Config.DesiredCapabilities)
            {
                if (AvailableCapabilities.All(acap => acap.Key.ToIrcLower() != capability.ToIrcLower())) continue;

                if (capabilityRequest != "")
                {
                    capabilityRequest += " " + capability.ToIrcLower();
                }
                else
                {
                    capabilityRequest += capability.ToIrcLower();
                }
            }
            
            //Kick the capability negotiation process off
            if (capabilityRequest != "") Connector.Transmit($"CAP REQ :{capabilityRequest}");
        }
        
        /// <summary>
        /// Join channels configured to be joined automatically
        /// </summary>
        private void RunAutoJoins()
        {
            foreach (string channel in Connector.Config.AutoJoinChannels)
            {
                Join(channel);
            }
        }
        
        /// <summary>
        /// When the connection is considered properly established (MOTD has been received or we have been notified one isn't available) consider the client ready
        /// </summary>
        private void ControllerReady()
        {
            RunAutoJoins();
            _sessionReady = true;
            Ready?.Invoke(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a given string forms a valid channel name, specific to the connected server
        /// </summary>
        /// <param name="channelName">String to validate</param>
        /// <returns>Whether the string is a valid channel name</returns>
        public bool IsValidChannelName(string channelName)
        {
            return IrcChannel.IsValidName(channelName, SupportedChannelTypes);
        }



        /// <summary>
        /// Send a QUIT to the server to safely disconnect
        /// </summary>
        /// <param name="quitMessage">The message for quitting</param>
        public void Quit(string quitMessage = "Client quit")
        {
            _expectingTermination = true;
            Connector.Transmit($"QUIT :{quitMessage}");
            Connector.Dispose();
        }

        /// <summary>
        /// Set a new NICK
        /// </summary>
        /// <param name="newNick">The NICK to request</param>
        public void SetNick(string newNick)
        {
            Connector.Transmit($"NICK :{newNick}");
        }

        /// <summary>
        /// Request WHO information on a Channel or Nick to update state cache
        /// </summary>
        /// <param name="channelOrNick">The Channel or Nick to update</param>
        public void Who(string channelOrNick)
        {
            Connector.Transmit($"WHO :{channelOrNick}");
        }

        /// <summary>
        /// Request WHOIS information on a Nick to update state cache
        /// </summary>
        /// <param name="nick">The Nick to update</param>
        public void WhoIs(string nick)
        {
            Connector.Transmit($"WHOIS :{nick}");
        }

        /// <summary>
        /// Request a channel JOIN
        /// </summary>
        /// <param name="channelName">The Channel to join</param>
        public void Join(string channelName)
        {
            if (!IsValidChannelName(channelName))
            {
                throw new ArgumentException("Not a valid channel name", nameof(channelName));
            }

            if (_channels.All(ch => ch.Name.ToIrcLower() != channelName.ToIrcLower()))
            {
                Connector.Transmit($"JOIN :{channelName}");
                Connector.Transmit($"MODE :{channelName}");
            }
        }

        /// <summary>
        /// Request a channel PART
        /// </summary>
        /// <param name="channelName">The Channel to leave</param>
        public void Part(string channelName)
        {
            if (!IsValidChannelName(channelName))
            {
                throw new ArgumentException("Not a valid channel name", nameof(channelName));
            }

            if (_channels.Any(ch => ch.Name.ToIrcLower() == channelName.ToIrcLower()))
            {
                Connector.Transmit($"PART :{channelName}");
            }
        }

        /// <summary>
        /// Send a PRIVMSG to the specified Nick or Channel (if the client is in the requested channel)
        /// </summary>
        /// <param name="channelOrNick">The Nick or Channel recipient</param>
        /// <param name="message">The message to send</param>
        public void SendPrivMsg(string channelOrNick, string message)
        {
            if (IsValidChannelName(channelOrNick))
            {
                if (_channels.All(ch => ch.Name.ToIrcLower() != channelOrNick.ToIrcLower()))
                {
                    throw new ArgumentException("Not in the requested channel", nameof(channelOrNick));
                }
            }
            else
            {
                IrcUser user = _users.FirstOrDefault(u => u.Nick.ToIrcLower() == channelOrNick.ToIrcLower());
                
                if (user != null)
                {
                    user.LastPrivateMessage = DateTime.Now;
                }
                else
                {
                    _users.Add(new IrcUser
                    {
                        Nick = channelOrNick,
                        LastPrivateMessage = DateTime.Now
                    });
                    
                    WhoIs(channelOrNick);
                }
            }

            Connector.Transmit($"PRIVMSG {channelOrNick} :{message}");
        }

        /// <summary>
        /// Send a NOTICE to the specified Nick or Channel (if the client is in the requested channel)
        /// </summary>
        /// <param name="channelOrNick">The Nick or Channel recipient</param>
        /// <param name="message">The message to send</param>
        public void SendNotice(string channelOrNick, string message)
        {
            if (IsValidChannelName(channelOrNick))
            {
                if (_channels.All(ch => ch.Name.ToIrcLower() != channelOrNick.ToIrcLower()))
                {
                    throw new ArgumentException("Not in the requested channel", nameof(channelOrNick));
                }
            }
            else
            {
                IrcUser user = _users.FirstOrDefault(u => u.Nick.ToIrcLower() == channelOrNick.ToIrcLower());
                if (user != null)
                {
                    user.LastPrivateMessage = DateTime.Now;
                }
                else
                {
                    _users.Add(new IrcUser
                    {
                        Nick = channelOrNick,
                        LastPrivateMessage = DateTime.Now
                    });
                    WhoIs(channelOrNick);
                }
            }

            Connector.Transmit($"NOTICE {channelOrNick} :{message}");
        }

        /// <summary>
        /// Set the client's status to AWAY
        /// </summary>
        /// <param name="reason">The reason for the away</param>
        public void SetAway(string reason = "Busy")
        {
            Connector.Transmit($"AWAY :{reason}");
        }

        /// <summary>
        /// Set the client's status to no longer AWAY
        /// </summary>
        public void SetUnAway()
        {
            Connector.Transmit("AWAY");
        }

        #endregion
    }
}
