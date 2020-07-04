using System;
using System.Collections.Generic;
using System.Linq;
using smIRCL.Constants;
using smIRCL.Enums;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    /// <summary>
    /// A controller which handles the ongoing state of an IRC server connection and allows control over that connection
    /// </summary>
    public class IrcController
    {
        #region Public Properties

        /// <summary>
        /// The connector for handling and accessing the IRC server connection
        /// </summary>
        public IrcConnector Connector { get; internal set; }
        /// <summary>
        /// A list of Users who are within scope of the client
        /// </summary>
        public readonly List<IrcUser> Users = new List<IrcUser>();
        /// <summary>
        /// A list of channels currently joined by the client
        /// </summary>
        public readonly List<IrcChannel> Channels = new List<IrcChannel>();

        /// <summary>
        /// The Nick of the client
        /// </summary>
        public string Nick { get; internal set; }
        /// <summary>
        /// The User name of the client
        /// </summary>
        public string UserName { get; internal set; }
        /// <summary>
        /// The Real name (GECOS) of the client
        /// </summary>
        public string RealName { get; internal set; }
        /// <summary>
        /// The Hostname or Mask of the client
        /// </summary>
        public string Host { get; internal set; }

        /// <summary>
        /// The channel characters which are valid for the connected server
        /// </summary>
        public List<char> SupportedChannelTypes = new List<char>();
        /// <summary>
        /// The modes supported for channels for the connected server
        /// </summary>
        public SupportedChannelModes SupportedChannelModes = new SupportedChannelModes();

        /// <summary>
        /// The IRCv3 capabilities supported by the server
        /// </summary>
        public List<KeyValuePair<string, List<string>>> AvailableCapabilities = new List<KeyValuePair<string, List<string>>>();
        /// <summary>
        /// The IRCv3 capabilities negotiated and activated with the server
        /// </summary>
        public List<string> NegotiatedCapabilities = new List<string>();


        #endregion

        #region Public Command Handling

        /// <summary>
        /// An IRC message handler for commands and numerics
        /// </summary>
        /// <param name="connector">The connector which fired the message</param>
        /// <param name="controller">The controller handling the message</param>
        /// <param name="message">THe message received</param>
        public delegate void IrcMessageHandler(IrcConnector connector, IrcController controller, IrcMessage message);
        /// <summary>
        /// The collection of handlers which handle commands and numerics
        /// </summary>
        public IrcHandlerList Handlers = new IrcHandlerList();

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
                Handlers.Add("CAP", OnCapability);

                Handlers.Add(Numerics.RPL_MYINFO, OnWelcomeEnd);
                Handlers.Add(Numerics.RPL_WHOREPLY, OnWhoReply);
                Handlers.Add(Numerics.RPL_WHOISUSER, OnWhoIsUserReply);
                Handlers.Add(Numerics.RPL_WHOISCHANNELS, OnWhoIsChannelsReply);
                Handlers.Add(Numerics.RPL_TOPIC, OnTopicInform);
                Handlers.Add(Numerics.RPL_HOSTHIDDEN, OnHostMaskCloak);
                Handlers.Add(Numerics.RPL_NAMREPLY, OnNamesReply);
                Handlers.Add(Numerics.RPL_ENDOFNAMES, OnNamesEnd);
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
            Users.RemoveAll(u => u.MutualChannels.Count == 0 && (u.LastDirectMessage == null || u.LastDirectMessage + Connector.Config.DirectMessageHoldingPeriod < DateTime.Now));
        }

        private void CompleteCapabilityRequesting()
        {
            string capabilityRequest = "";

            if (Connector.Config.AuthMode == AuthMode.SASL) capabilityRequest += "sasl";

            foreach (string capability in Connector.Config.DesiredCapabilities)
            {
                if (capabilityRequest != "")
                {
                    capabilityRequest += " " + capability.ToIrcLower();
                }
                else
                {
                    capabilityRequest += capability.ToIrcLower();
                }
            }

            if (capabilityRequest != "") Connector.Transmit($"CAP REQ :{capabilityRequest}");

            Connector.Transmit("CAP :END");
        }

        private void FinaliseCapabilities()
        {
            //TODO SASL auth if acknowledged
            //TODO add support for away-notify for users
        }

        #endregion

        #region Public Methods

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
            if (!IrcChannel.IsValidName(channelName))
            {
                throw new ArgumentException("Not a valid channel name", nameof(channelName));
            }

            if (Channels.All(ch => ch.Name.ToIrcLower() != channelName.ToIrcLower()))
            {
                Connector.Transmit($"JOIN :{channelName}");
            }
        }

        /// <summary>
        /// Request a channel PART
        /// </summary>
        /// <param name="channelName">The Channel to leave</param>
        public void Part(string channelName)
        {
            if (!IrcChannel.IsValidName(channelName))
            {
                throw new ArgumentException("Not a valid channel name", nameof(channelName));
            }

            if (Channels.Any(ch => ch.Name.ToIrcLower() == channelName.ToIrcLower()))
            {
                Connector.Transmit($"PART :{channelName}");
            }
        }

        #endregion

        #region Connector Handlers

        private void ConnectorOnConnected()
        {
            //See https://modern.ircdocs.horse/index.html#connection-registration
            Connector.Transmit("CAP LS :302");
            //Capability negotiation will occur in a handler if supported by the server
            if (!string.IsNullOrWhiteSpace(Connector.Config.ServerPassword)) Connector.Transmit($"PASS :{Connector.Config.ServerPassword}");
            _unconfirmedNick = Connector.Config.Nick;
            SetNick(Connector.Config.Nick);
            Connector.Transmit($"USER {Connector.Config.UserName} 0 * :{Connector.Config.RealName}");
            //SASL will be completed by CompleteCapabilityRequesting
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
                ircMessageHandler.Value.Invoke(Connector, this, message);
            }
        }

        #endregion

        #region Handlers

        private void OnPing(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            Connector.Transmit($"PONG :{message.Parameters[0]}");
            Ping?.Invoke(client, controller, message);
        }

        private void OnPrivMsg(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            PrivMsg?.Invoke(connector, controller, message);
        }

        private void OnNotice(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            Notice?.Invoke(connector, controller, message);
        }

        private void OnUnrecoverableError(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            Connector.Dispose();
        }

        private void OnNickError(IrcConnector connector, IrcController controller, IrcMessage message)
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

        private void OnNickSet(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower()) //Minimum requirement of a source is Nick which is always unique
            {
                Nick = message.Parameters[0];
            }
            else if (Users.Any(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower()))
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                if (user != null) user.Nick = message.Parameters[0];
            }

            WhoIs(message.Parameters[0]);
        }

        private void OnWelcomeEnd(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            Nick = _unconfirmedNick;
            _unconfirmedNick = null;
            WhoIs(Nick);

            foreach (string channel in Connector.Config.AutoJoinChannels)
            {
                Join(channel);
            }
        }

        private void OnWhoReply(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.Parameters[5].ToIrcLower() == Nick.ToIrcLower())
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
                RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];
            }
            else if (Users.Any(u => u.Nick.ToIrcLower() == message.Parameters[5].ToIrcLower()))
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[5].ToIrcLower());
                if (user != null)
                {
                    user.UserName = message.Parameters[2];
                    user.Host = message.Parameters[3];
                    user.RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];
                }
            }
        }

        private void OnWhoIsUserReply(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() == Nick.ToIrcLower())
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
                RealName = message.Parameters[5];
            }
            else if (Users.Any(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower()))
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower());
                if (user != null)
                {
                    user.UserName = message.Parameters[2];
                    user.Host = message.Parameters[3];
                    user.RealName = message.Parameters[5];
                }
            }
        }

        private void OnWhoIsChannelsReply(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() != Nick.ToIrcLower() && Users.Any(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower()))
            {
                string[] channels = message.Parameters[2].Split(' ');
                List<string> trimmedChannels = new List<string>();
                foreach (string channel in channels)
                {
                    trimmedChannels.Add(channel.TrimStart('@', '+'));
                }

                foreach (string channel in trimmedChannels)
                {
                    if (Channels.Any(ch => ch.Name.ToIrcLower() == channel.ToIrcLower()))
                    {
                        IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower());
                        if (user != null && user.MutualChannels.All(uch => uch.ToIrcLower() != channel.ToIrcLower())) user.MutualChannels.Add(channel);
                    }
                }
            }
        }

        private void OnHostMaskCloak(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            Host = message.Parameters[1];
        }

        private void OnJoin(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if(message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                if (Channels.All(ch => ch.Name.ToIrcLower() != message.Parameters[0].ToIrcLower())) Channels.Add(new IrcChannel(message.Parameters[0]));
            }
            else
            {
                if (Users.All(u => u.Nick.ToIrcLower() != message.SourceNick.ToIrcLower()))
                {
                    Users.Add(new IrcUser
                    {
                        MutualChannels = new List<string>(),
                        HostMask = message.SourceHostMask,
                        Nick = message.SourceNick,
                        Host = message.SourceHost,
                        UserName = message.SourceUserName
                    });
                    WhoIs(message.SourceNick);
                }
                else
                {
                    Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower())?.MutualChannels.Add(message.Parameters[0]);
                }

                Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower())?.Users.Add(message.SourceNick);
            }
        }

        private void OnNamesReply(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[2].ToIrcLower());
            if (channel != null)
            {
                if (channel.UserCollectionComplete)
                {
                    channel.UserCollectionComplete = false;
                    channel.Users = new List<string>();
                }

                string[] users = message.Parameters[3].Split(' ');

                foreach (string user in users)
                {
                    if (user.ToIrcLower() != Nick.ToIrcLower())
                    {
                        channel.Users.Add(user.TrimStart('@', '+'));
                        if (Users.All(u => u.Nick.ToLower() != user.ToIrcLower()))
                        {
                            Users.Add(new IrcUser
                            {
                                MutualChannels = new List<string>
                                {
                                    channel.Name
                                },
                                Nick = user.TrimStart('@', '+')
                            });
                        }
                        else
                        {
                            Users.FirstOrDefault(u => u.Nick.ToIrcLower() == user.ToIrcLower())?.MutualChannels.Add(channel.Name);
                        }
                    }
                }
            }
        }

        private void OnNamesEnd(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[1].ToIrcLower());
            if (channel != null) channel.UserCollectionComplete = true;

            Who(message.Parameters[1]);
        }

        private void OnPart(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                if (channel != null) Channels.Remove(channel);

                List<IrcUser> usersWithMutualChannels = Users.Where(u => u.MutualChannels.Any(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower())).ToList();
                foreach (IrcUser user in usersWithMutualChannels)
                {
                    user.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());
                }

                DoUserGarbageCollection();
            }
            else
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                channel?.Users.RemoveAll(u => u.ToIrcLower() == message.SourceNick.ToIrcLower());

                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                user?.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());
                
                DoUserGarbageCollection();
            }
        }

        private void OnKick(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() == Nick.ToIrcLower())
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                if (channel != null) Channels.Remove(channel);

                List<IrcUser> usersWithMutualChannels = Users.Where(u => u.MutualChannels.Any(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower())).ToList();
                foreach (IrcUser user in usersWithMutualChannels)
                {
                    user.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());
                }

                DoUserGarbageCollection();
            }
            else
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                channel?.Users.RemoveAll(u => u.ToIrcLower() == message.Parameters[1].ToIrcLower());

                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower());
                user?.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());

                DoUserGarbageCollection();
            }
        }

        private void OnQuit(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() != Nick.ToIrcLower())
            {
                List<IrcChannel> mutualChannels = Channels.Where(ch => ch.Users.Any(u => u.ToIrcLower() == message.SourceNick.ToIrcLower())).ToList();
                foreach (IrcChannel mutualChannel in mutualChannels)
                {
                    mutualChannel.Users.RemoveAll(u => u.ToIrcLower() == message.SourceNick.ToIrcLower());
                }

                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                if (user != null) Users.Remove(user);
            }
        }

        private void OnTopicInform(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[1].ToIrcLower());
            if (channel != null) channel.Topic = message.Parameters[2];
        }

        private void OnTopicUpdate(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
            if (channel != null) channel.Topic = message.Parameters[1] != "" ? message.Parameters[1] : null;
        }

        private void OnISupport(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            foreach (string parameter in message.Parameters)
            {
                string[] keyPair = parameter.Split(new[] { '=' }, 2);

                if (keyPair.Length < 2) continue;

                switch (keyPair[0].ToIrcLower())
                {
                    case "chantypes":
                        foreach (char c in keyPair[1])
                        {
                            if (SupportedChannelTypes.Any(sct => sct == c)) continue;
                            SupportedChannelTypes.Add(c);
                        }
                        break;

                    case "chanmodes":
                        string[] chanModeGroups = keyPair[1].Split(','); //0 A, 1 B, 2 C, 3 D

                        int currentChanModeGroup = 0;

                        foreach (string chanModeGroup in chanModeGroups)
                        {
                            List<char> chanModeList = null;

                            switch (currentChanModeGroup)
                            {
                                case 0:
                                    chanModeList = SupportedChannelModes.A; 
                                    break;
                                case 1:
                                    chanModeList = SupportedChannelModes.B;
                                    break;
                                case 2:
                                    chanModeList = SupportedChannelModes.C;
                                    break;
                                case 3:
                                    chanModeList = SupportedChannelModes.D;
                                    break;
                            }

                            foreach (char chanMode in chanModeGroup)
                            {
                                chanModeList?.Add(chanMode);
                            }

                            currentChanModeGroup++;
                        }

                        break;

                }
            }
        }

        private void OnCapability(IrcConnector connector, IrcController controller, IrcMessage message)
        {
            switch (message.Parameters[1].ToIrcLower())
            {
                case "ls":
                    string[] capabilitiesGiven = message.Parameters[message.Parameters.Count - 1].Split(' ');

                    foreach (string cap in capabilitiesGiven)
                    {
                        string[] capabilityAndParameters = cap.Split('=');
                        List<string> parameters = capabilityAndParameters.Length > 1 ? capabilityAndParameters[1].Split(',').ToList() : new List<string>();

                        if (AvailableCapabilities.All(acap => acap.Key != capabilityAndParameters[0])) AvailableCapabilities.Add(new KeyValuePair<string, List<string>>(capabilityAndParameters[0], parameters));
                    }

                    if (message.Parameters[2] != "*") CompleteCapabilityRequesting();
                    break;

                case "ack":
                    string[] capabilitiesAcknowledged = message.Parameters[message.Parameters.Count - 1].Split(' ');

                    foreach (string cap in capabilitiesAcknowledged)
                    {
                        if (NegotiatedCapabilities.All(ncap => ncap != cap.ToIrcLower())) NegotiatedCapabilities.Add(cap.ToIrcLower());
                    }

                    FinaliseCapabilities();
                    break;
            }
        }

        #endregion
    }
}
