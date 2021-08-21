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
                Handlers.Add("PING", OnPing);
                Handlers.Add("PRIVMSG", OnPrivMsg);
                Handlers.Add("NOTICE", OnNotice);
                Handlers.Add("ERROR", OnUnrecoverableError);
                Handlers.Add("NICK", OnNickSet);
                Handlers.Add("JOIN", OnJoin);
                Handlers.Add("PART", OnPart);
                Handlers.Add("KICK", OnKick);
                Handlers.Add("QUIT", OnQuit);
                Handlers.Add("TOPIC", OnTopicUpdate);
                Handlers.Add("MODE", OnMode);
                Handlers.Add("CAP", OnCapability);
                Handlers.Add("AUTHENTICATE", OnSasl);
                Handlers.Add("AWAY", OnAwayNotify);
                Handlers.Add("CHGHOST", OnChangeHost);

                Handlers.Add(Numerics.RPL_SASLSUCCESS, OnSaslComplete);
                Handlers.Add(Numerics.RPL_LOGGEDOUT, OnSaslFailure);
                Handlers.Add(Numerics.ERR_NICKLOCKED, OnSaslFailure);
                Handlers.Add(Numerics.ERR_SASLFAIL, OnSaslFailure);
                Handlers.Add(Numerics.ERR_SASLTOOLONG, OnSaslFailure);
                Handlers.Add(Numerics.ERR_SASLABORTED, OnSaslFailure);
                Handlers.Add(Numerics.ERR_SASLALREADY, OnSaslFailure);

                Handlers.Add(Numerics.RPL_MYINFO, OnWelcomeEnd);
                Handlers.Add(Numerics.RPL_MOTD, OnMotdPart);
                Handlers.Add(Numerics.RPL_ENDOFMOTD, OnMotdEnd);
                Handlers.Add(Numerics.ERR_NOMOTD, OnNoMotd);
                Handlers.Add(Numerics.RPL_WHOREPLY, OnWhoReply);
                Handlers.Add(Numerics.RPL_WHOISUSER, OnWhoIsUserReply);
                Handlers.Add(Numerics.RPL_WHOISCHANNELS, OnWhoIsChannelsReply);
                Handlers.Add(Numerics.RPL_TOPIC, OnTopicInform);
                Handlers.Add(Numerics.RPL_HOSTHIDDEN, OnHostMaskCloak);
                Handlers.Add(Numerics.RPL_NAMREPLY, OnNamesReply);
                Handlers.Add(Numerics.RPL_ENDOFNAMES, OnNamesEnd);
                Handlers.Add(Numerics.RPL_CHANNELMODEIS, OnChannelModes);
                Handlers.Add(Numerics.RPL_ISUPPORT, OnISupport);

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

        private void DoUserGarbageCollection()
        {
            Users.RemoveAll(u => u.MutualChannels.Count == 0 && (u.LastDirectMessage == null ||  u.LastDirectMessage + Connector.Config.DirectMessageHoldingPeriod < DateTime.Now));
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

            if (Channels.All(ch => ch.Name.ToIrcLower() != channelName.ToIrcLower()))
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

            if (Channels.Any(ch => ch.Name.ToIrcLower() == channelName.ToIrcLower()))
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
                if (Channels.All(ch => ch.Name.ToIrcLower() != channelOrNick.ToIrcLower()))
                {
                    throw new ArgumentException("Not in the requested channel", nameof(channelOrNick));
                }
            }
            else
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == channelOrNick.ToIrcLower());
                
                if (user != null)
                {
                    user.LastDirectMessage = DateTime.Now;
                }
                else
                {
                    Users.Add(new IrcUser
                    {
                        Nick = channelOrNick,
                        LastDirectMessage = DateTime.Now
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
                if (Channels.All(ch => ch.Name.ToIrcLower() != channelOrNick.ToIrcLower()))
                {
                    throw new ArgumentException("Not in the requested channel", nameof(channelOrNick));
                }
            }
            else
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == channelOrNick.ToIrcLower());
                if (user != null)
                {
                    user.LastDirectMessage = DateTime.Now;
                }
                else
                {
                    Users.Add(new IrcUser
                    {
                        Nick = channelOrNick,
                        LastDirectMessage = DateTime.Now
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
