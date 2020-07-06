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
        /// The Away status and message of the client
        /// </summary>
        public string Away { get; internal set; }


        /// <summary>
        /// The channel characters which are valid for the connected server
        /// </summary>
        public List<char> SupportedChannelTypes { get; internal set; } = new List<char>();

        /// <summary>
        /// The modes supported for channels for the connected server
        /// </summary>
        public SupportedChannelModes SupportedChannelModes { get; internal set; } = new SupportedChannelModes();

        /// <summary>
        /// The prefixes representing modes assignable to users in channels
        /// </summary>
        public List<KeyValuePair<char, char>> SupportedUserPrefixes { get; internal set; } = new List<KeyValuePair<char, char>>();

        /// <summary>
        /// The IRCv3 capabilities supported by the server
        /// </summary>
        public List<KeyValuePair<string, List<string>>> AvailableCapabilities { get; internal set; } = new List<KeyValuePair<string, List<string>>>();

        /// <summary>
        /// The IRCv3 capabilities negotiated and activated with the server
        /// </summary>
        public List<string> NegotiatedCapabilities { get; internal set; } = new List<string>();

        /// <summary>
        /// The MOTD of the connected server, if one is available
        /// </summary>
        public string ServerMotd { get; internal set; } = null;

        #endregion

        #region Public Command Handling

        /// <summary>
        /// An IRC message handler for commands and numerics
        /// </summary>
        /// <param name="connector">The connector which fired the message</param>
        /// <param name="controller">The controller handling the message</param>
        /// <param name="message">THe message received</param>
        public delegate void IrcMessageHandler(IrcController controller, IrcMessage message);

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

        private Timer _userGarbageCollectionTimer;

        private string _unconfirmedNick;

        private bool _expectingTermination;

        private readonly List<char> _trimmableUserPrefixes = new List<char>();

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
            Users.RemoveAll(u =>
                u.MutualChannels.Count == 0 && (u.LastDirectMessage == null ||
                                                u.LastDirectMessage + Connector.Config.DirectMessageHoldingPeriod <
                                                DateTime.Now));
        }
        private void DoUserGarbageCollection(Object source, ElapsedEventArgs e)
        {
            DoUserGarbageCollection();
        }

        private void CompleteCapabilityRequesting()
        {
            string capabilityRequest = "";

            if (Connector.Config.AuthMode == AuthMode.SASL)
            {
                if (AvailableCapabilities.Any(acap => acap.Key.ToIrcLower() == "sasl"))
                {
                    capabilityRequest += "sasl";
                }
                else
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

            if (capabilityRequest != "") Connector.Transmit($"CAP REQ :{capabilityRequest}");
        }

        private void FinaliseCapabilities()
        {
            if (Connector.Config.AuthMode == AuthMode.SASL)
            {
                Connector.Transmit("AUTHENTICATE :PLAIN");
            }
            else
            {
                Connector.Transmit("CAP :END");
            }
        }

        private void RunAutoJoins()
        {
            foreach (string channel in Connector.Config.AutoJoinChannels)
            {
                Join(channel);
            }
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

        #region Connector Handlers

        private void ConnectorOnConnected()
        {
            //See https://modern.ircdocs.horse/index.html#connection-registration
            Connector.Transmit("CAP LS :302");
            //Capability negotiation will occur in a handler if supported by the server
            if (!string.IsNullOrWhiteSpace(Connector.Config.ServerPassword))
                Connector.Transmit($"PASS :{Connector.Config.ServerPassword}");
            _unconfirmedNick = Connector.Config.Nick;
            SetNick(Connector.Config.Nick);
            Connector.Transmit($"USER {Connector.Config.UserName} 0 * :{Connector.Config.RealName}");
            //SASL will be completed by FinaliseCapabilities
        }

        private void ConnectorOnMessageReceived(string rawMessage, IrcMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message.Command == "ERROR" && _expectingTermination)
            {
                return;
            }

            CheckOperationValid();

            foreach (KeyValuePair<string, IrcMessageHandler> ircMessageHandler in Handlers.Where(h =>
                h.Key == message.Command))
            {
                ircMessageHandler.Value.Invoke(this, message);
            }
        }

        #endregion

        #region Handlers

        private void OnPing(IrcController controller, IrcMessage message)
        {
            Connector.Transmit($"PONG :{message.Parameters[0]}");
            Ping?.Invoke(controller, message);
        }

        private void OnPrivMsg(IrcController controller, IrcMessage message)
        {
            if (!controller.IsValidChannelName(message.Parameters[0]))
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                if (user != null)
                {
                    user.LastDirectMessage = DateTime.Now;
                }
                else
                {
                    Users.Add(new IrcUser
                    {
                        HostMask = message.SourceHostMask,
                        Nick = message.SourceNick,
                        Host = message.SourceHost,
                        UserName = message.SourceUserName,
                        LastDirectMessage = DateTime.Now
                    });
                    WhoIs(message.SourceNick);
                }
            }
        }

        private void OnNotice(IrcController controller, IrcMessage message)
        {
            Notice?.Invoke(controller, message);
        }

        private void OnUnrecoverableError(IrcController controller, IrcMessage message)
        {
            Connector.Dispose();
        }

        private void OnNickError(IrcController controller, IrcMessage message)
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

        private void OnNickSet(IrcController controller, IrcMessage message)
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

        private void OnWelcomeEnd(IrcController controller, IrcMessage message)
        {
            Nick = _unconfirmedNick;
            _unconfirmedNick = null;
            WhoIs(Nick);
        }

        private void OnMotdPart(IrcController controller, IrcMessage message)
        {
            if (ServerMotd == null)
            {
                ServerMotd = message.Parameters[1];
            }
            else
            {
                ServerMotd += "\n" + message.Parameters[1];
            }
        }

        private void OnMotdEnd(IrcController controller, IrcMessage message)
        {
            //Now safe to assume ISupport has been received and processed
            RunAutoJoins();
        }

        private void OnNoMotd(IrcController controller, IrcMessage message)
        {
            //Now safe to assume ISupport has been received and processed
            RunAutoJoins();
        }

        private void OnWhoReply(IrcController controller, IrcMessage message)
        {
            char[] statuses = message.Parameters[6].ToCharArray();

            if (message.Parameters[5].ToIrcLower() == Nick.ToIrcLower())
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
                RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];

                foreach (char status in statuses)
                {
                    switch (status)
                    {
                        case 'G':
                            Away = "Unknown";
                            break;

                        case 'H':
                            Away = null;
                            break;
                    }
                }
            }
            else if (Users.Any(u => u.Nick.ToIrcLower() == message.Parameters[5].ToIrcLower()))
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[5].ToIrcLower());
                if (user != null)
                {
                    user.UserName = message.Parameters[2];
                    user.Host = message.Parameters[3];
                    user.RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];

                    foreach (char status in statuses)
                    {
                        switch (status)
                        {
                            case 'G':
                                user.Away = "Unknown";
                                break;

                            case 'H':
                                user.Away = null;
                                break;
                        }
                    }
                }
            }
        }

        private void OnWhoIsUserReply(IrcController controller, IrcMessage message)
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

        private void OnWhoIsChannelsReply(IrcController controller, IrcMessage message)
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

        private void OnHostMaskCloak(IrcController controller, IrcMessage message)
        {
            Host = message.Parameters[1];
        }

        private void OnJoin(IrcController controller, IrcMessage message)
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

        private void OnNamesReply(IrcController controller, IrcMessage message)
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
                    string userNick = user.TrimStart(_trimmableUserPrefixes.ToArray());

                    if (userNick.ToIrcLower() != Nick.ToIrcLower())
                    {
                        channel.Users.Add(userNick);

                        List<char> userPrefixes = new List<char>();
                        string userSplice = user;

                        foreach (char trimmableUserPrefix in _trimmableUserPrefixes)
                        {
                            if (userSplice.StartsWith(trimmableUserPrefix.ToString()))
                            {
                                userPrefixes.Add(trimmableUserPrefix);
                                userSplice = userSplice.TrimStart(trimmableUserPrefix);
                            }
                        }

                        List<char> userModes = new List<char>();

                        foreach (char prefix in userPrefixes)
                        {
                            if (SupportedUserPrefixes.Any(p => p.Value == prefix)) userModes.Add(SupportedUserPrefixes.FirstOrDefault(p => p.Value == prefix).Key);
                        }

                        if (Users.All(u => u.Nick.ToLower() != userNick.ToIrcLower()))
                        {
                            Users.Add(new IrcUser
                            {
                                MutualChannels = new List<string>
                                {
                                    channel.Name
                                },
                                MutualChannelModes = new List<KeyValuePair<string, List<char>>>
                                {
                                    new KeyValuePair<string, List<char>>(channel.Name, userModes)
                                },
                                Nick = userNick
                            });
                        }
                        else
                        {
                            IrcUser globalUser = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == userNick.ToIrcLower());
                            if (globalUser != null)
                            {
                                if (globalUser.MutualChannels.All(ch => ch.ToIrcLower() != channel.Name.ToIrcLower())) globalUser.MutualChannels.Add(channel.Name);
                                if (globalUser.MutualChannelModes.All(ch => ch.Key.ToIrcLower() != channel.Name.ToIrcLower())) globalUser.MutualChannelModes.Add(new KeyValuePair<string, List<char>>(channel.Name, userModes));
                            }
                        }
                    }
                    else
                    {
                        List<char> userPrefixes = new List<char>();
                        string userSplice = user;

                        foreach (char trimmableUserPrefix in _trimmableUserPrefixes)
                        {
                            if (userSplice.StartsWith(trimmableUserPrefix.ToString()))
                            {
                                userPrefixes.Add(trimmableUserPrefix);
                                userSplice = userSplice.TrimStart(trimmableUserPrefix);
                            }
                        }

                        List<char> userModes = new List<char>();

                        foreach (char prefix in userPrefixes)
                        {
                            if (SupportedUserPrefixes.Any(p => p.Value == prefix)) userModes.Add(SupportedUserPrefixes.FirstOrDefault(p => p.Value == prefix).Key);
                        }

                        channel.ClientModes = userModes;
                    }
                }
            }
        }

        private void OnNamesEnd(IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[1].ToIrcLower());
            if (channel != null) channel.UserCollectionComplete = true;

            Who(message.Parameters[1]);
        }

        private void OnChannelModes(IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[1].ToIrcLower());
            if (channel != null)
            {
                char[] channelModes = message.Parameters[2].TrimStart('+').ToCharArray();
                Queue<string> channelModeParameters = new Queue<string>();

                for (int i = 3; i < message.Parameters.Count; i++)
                {
                    channelModeParameters.Enqueue(message.Parameters[i]);
                }

                foreach (char channelMode in channelModes)
                {
                    if (SupportedChannelModes.A.Contains(channelMode))
                    {
                        channelModeParameters.Dequeue(); //Not listening for A, discard
                    }
                    else if (SupportedChannelModes.B.Contains(channelMode) || SupportedChannelModes.C.Contains(channelMode))
                    {
                        channel.Modes.RemoveAll(m => m.Key == channelMode);
                        channel.Modes.Add(new KeyValuePair<char, string>(channelMode, channelModeParameters.Dequeue()));
                    }
                    else if (SupportedChannelModes.D.Contains(channelMode))
                    {
                        channel.Modes.RemoveAll(m => m.Key == channelMode);
                        channel.Modes.Add(new KeyValuePair<char, string>(channelMode, null));
                    }
                }
            }
        }

        private void OnMode(IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
            if (channel != null)
            {
                char[] sentModes = message.Parameters[1].ToCharArray();
                bool removal = false;

                Queue<string> otherParams = new Queue<string>();

                for (int i = 2; i < message.Parameters.Count; i++)
                {
                    otherParams.Enqueue(message.Parameters[i]);
                }

                foreach (char sentMode in sentModes)
                {
                    if (sentMode == '+')
                    {
                        removal = false;
                        continue;
                    }
                    
                    if (sentMode == '-')
                    {
                        removal = true;
                        continue;
                    }

                    if (SupportedChannelModes.A.Contains(sentMode))
                    {
                        otherParams.Dequeue(); //Not listening for A, discard
                    }
                    else if (SupportedChannelModes.B.Contains(sentMode) || SupportedChannelModes.C.Contains(sentMode))
                    {
                        channel.Modes.RemoveAll(m => m.Key == sentMode);
                        if (!removal)
                        {
                            channel.Modes.Add(new KeyValuePair<char, string>(sentMode, otherParams.Dequeue()));
                        }
                    }
                    else if (SupportedChannelModes.D.Contains(sentMode))
                    {
                        channel.Modes.RemoveAll(m => m.Key == sentMode);
                        if (!removal)
                        {
                            channel.Modes.Add(new KeyValuePair<char, string>(sentMode, null));
                        }
                    }
                    else if (SupportedUserPrefixes.Any(sup => sup.Key == sentMode))
                    {
                        string associatedNick = otherParams.Dequeue();

                        if (Nick.ToIrcLower() == associatedNick.ToIrcLower())
                        {
                            channel.ClientModes.RemoveAll(m => m == sentMode);
                            if (!removal)
                            {
                                channel.ClientModes.Add(sentMode);
                            }
                        }
                        else
                        {
                            IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == associatedNick.ToIrcLower());
                            if (user != null)
                            {
                                KeyValuePair<string, List<char>> mutualChannelModes = user.MutualChannelModes.FirstOrDefault(mcm => mcm.Key.ToIrcLower() == channel.Name.ToIrcLower());
                                mutualChannelModes.Value.RemoveAll(m => m == sentMode);
                                if (!removal)
                                {
                                    mutualChannelModes.Value.Add(sentMode);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPart(IrcController controller, IrcMessage message)
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
            }
            else
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                channel?.Users.RemoveAll(u => u.ToIrcLower() == message.SourceNick.ToIrcLower());

                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                user?.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());
            }

            DoUserGarbageCollection();
        }

        private void OnKick(IrcController controller, IrcMessage message)
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
            }
            else
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
                channel?.Users.RemoveAll(u => u.ToIrcLower() == message.Parameters[1].ToIrcLower());

                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.Parameters[1].ToIrcLower());
                user?.MutualChannels.RemoveAll(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower());
            }

            DoUserGarbageCollection();
        }

        private void OnQuit(IrcController controller, IrcMessage message)
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

        private void OnTopicInform(IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[1].ToIrcLower());
            if (channel != null) channel.Topic = message.Parameters[2];
        }

        private void OnTopicUpdate(IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name.ToIrcLower() == message.Parameters[0].ToIrcLower());
            if (channel != null) channel.Topic = message.Parameters[1] != "" ? message.Parameters[1] : null;
        }

        private void OnISupport(IrcController controller, IrcMessage message)
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

                    case "prefix":
                        string[] modePairs = keyPair[1].TrimStart('(').Split(')');

                        for (int i = 0; i < modePairs[0].Length; i++)
                        {
                            if (SupportedUserPrefixes.All(pref => pref.Key != modePairs[0][i])) SupportedUserPrefixes.Add(new KeyValuePair<char, char>(modePairs[0][i], modePairs[1][i]));

                            if (_trimmableUserPrefixes.All(tup => tup != modePairs[1][i])) _trimmableUserPrefixes.Add(modePairs[1][i]);
                        }
                        break;
                }
            }
        }

        private void OnCapability(IrcController controller, IrcMessage message)
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

        private void OnSasl(IrcController controller, IrcMessage message)
        {
            switch (message.Parameters[0].ToIrcLower())
            {
                case "+":
                    Connector.Transmit($"AUTHENTICATE {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Connector.Config.AuthUsername}\x00{Connector.Config.AuthUsername}\x00{Connector.Config.AuthPassword}"))}");
                    break;
            }
        }

        private void OnSaslComplete(IrcController controller, IrcMessage message)
        {
            Connector.Transmit("CAP :END");
        }

        private void OnSaslFailure(IrcController controller, IrcMessage message)
        {
            Quit("SASL authentication has failed");
        }

        private void OnAwayNotify(IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                if (message.Parameters.Count == 0)
                {
                    Away = null;
                }
                else
                {
                    Away = message.Parameters[0];
                }
            }
            else
            {
                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower());
                if (user != null)
                {
                    if (message.Parameters.Count == 0)
                    {
                        user.Away = null;
                    }
                    else
                    {
                        user.Away = message.Parameters[0];
                    }
                }
            }
        }

        #endregion
    }
}
