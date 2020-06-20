using System;
using System.Collections.Generic;
using System.Linq;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL
{
    public class IrcController
    {
        #region Public Properties

        public IrcConnector Connector { get; internal set; }
        public readonly List<IrcUser> Users = new List<IrcUser>();
        public readonly List<IrcChannel> Channels = new List<IrcChannel>();

        public string Nick { get; internal set; }
        public string UserName { get; internal set; }
        public string RealName { get; internal set; }
        //public string HostMask { get; internal set; }
        public string Host { get; internal set; }


        #endregion

        #region Public Command Handling

        public delegate void IrcMessageHandler(IrcConnector client, IrcController controller, IrcMessage message);
        //public Dictionary<string, IrcMessageHandler> Handlers = new Dictionary<string, IrcMessageHandler>();
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

        public IrcController(IrcConnector connector, bool registerInternalHandlers = true)
        {
            Connector = connector ?? throw new ArgumentNullException(nameof(connector));
            connector.Connected += ConnectorOnConnected;
            connector.MessageReceived += ConnectorOnMessageReceived;

            if (registerInternalHandlers)
            {
                Handlers.Add("PING", OnPing);
                Handlers.Add("ERROR", OnUnrecoverableError);
                Handlers.Add("NICK", OnNickSet);
                Handlers.Add("JOIN", OnJoin);
                Handlers.Add("PART", OnPart);
                Handlers.Add("TOPIC", OnTopicUpdate);

                Handlers.Add(Numerics.RPL_MYINFO, OnWelcomeEnd);
                Handlers.Add(Numerics.RPL_WHOREPLY, OnWhoReply);
                Handlers.Add(Numerics.RPL_WHOISUSER, OnWhoIsUserReply);
                Handlers.Add(Numerics.RPL_WHOISCHANNELS, OnWhoIsChannelsReply);
                Handlers.Add(Numerics.RPL_TOPIC, OnTopicInform);
                Handlers.Add(Numerics.RPL_HOSTHIDDEN, OnHostMaskCloak);
                Handlers.Add(Numerics.RPL_NAMREPLY, OnNamesReply);
                Handlers.Add(Numerics.RPL_ENDOFNAMES, OnNamesEnd);

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

        public void Who(string channelOrNick)
        {
            Connector.Transmit($"WHO :{channelOrNick}");
        }

        public void WhoIs(string nick)
        {
            Connector.Transmit($"WHOIS :{nick}");
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
                ircMessageHandler.Value.Invoke(Connector, this, message);
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
            lock (Users)
            {
                if (message.SourceNick.ToLowerNick() == Nick.ToLowerNick()) //Minimum requirement of a source is Nick which is always unique
                {
                    Nick = message.Parameters[0];
                }
                else if (Users.Any(u => u.Nick.ToLowerNick() == message.SourceNick.ToLowerNick()))
                {
                    IrcUser user = Users.FirstOrDefault(u => u.Nick.ToLowerNick() == message.SourceNick.ToLowerNick());
                    if (user != null) user.Nick = message.Parameters[0];
                }
            }
            WhoIs(message.Parameters[0]);
        }

        private void OnWelcomeEnd(IrcConnector client, IrcController controller, IrcMessage message)
        {
            Nick = _unconfirmedNick;
            _unconfirmedNick = null;
            WhoIs(Nick);
        }

        private void OnWhoReply(IrcConnector client, IrcController controller, IrcMessage message)
        {
            lock (Users)
            {
                if (message.Parameters[5].ToLowerNick() == Nick.ToLowerNick())
                {
                    UserName = message.Parameters[2];
                    Host = message.Parameters[3];
                    RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];
                }
                else if (Users.Any(u => u.Nick.ToLowerNick() == message.Parameters[5].ToLowerNick()))
                {
                    IrcUser user = Users.FirstOrDefault(u => u.Nick.ToLowerNick() == message.Parameters[5].ToLowerNick());
                    if (user != null)
                    {
                        user.UserName = message.Parameters[2];
                        user.Host = message.Parameters[3];
                        user.RealName = message.Parameters[7].Split(new[] {' '}, 2)[1];
                    }
                }
            }
        }

        private void OnWhoIsUserReply(IrcConnector client, IrcController controller, IrcMessage message)
        {
            lock (Users)
            {
                if (message.Parameters[1].ToLowerNick() == Nick.ToLowerNick())
                {
                    UserName = message.Parameters[2];
                    Host = message.Parameters[3];
                    RealName = message.Parameters[5];
                }
                else if (Users.Any(u => u.Nick.ToLowerNick() == message.Parameters[1].ToLowerNick()))
                {
                    IrcUser user = Users.FirstOrDefault(u => u.Nick.ToLowerNick() == message.Parameters[1].ToLowerNick());
                    if (user != null)
                    {
                        user.UserName = message.Parameters[2];
                        user.Host = message.Parameters[3];
                        user.RealName = message.Parameters[5];
                    }
                }
            }
        }

        private void OnWhoIsChannelsReply(IrcConnector client, IrcController controller, IrcMessage message)
        {
            lock (Users)
            {
                if (message.Parameters[1].ToLowerNick() != Nick.ToLowerNick() && Users.Any(u => u.Nick.ToLowerNick() == message.Parameters[1].ToLowerNick()))
                {
                    string[] channels = message.Parameters[2].Split(' ');
                    List<string> trimmedChannels = new List<string>();
                    foreach (string channel in channels)
                    {
                        trimmedChannels.Add(channel.TrimStart('@', '+'));
                    }

                    lock (Channels)
                    {
                        foreach (string channel in trimmedChannels)
                        {
                            if (Channels.Any(ch => ch.Name == channel))
                            {
                                IrcUser user = Users.FirstOrDefault(u => u.Nick.ToLowerNick() == message.Parameters[1].ToLowerNick());
                                if(user != null && user.MutualChannels.All(uch => uch != channel)) user.MutualChannels.Add(channel);
                            }
                        }
                    }
                }
            }
        }

        private void OnHostMaskCloak(IrcConnector client, IrcController controller, IrcMessage message)
        {
            Host = message.Parameters[1];
        }

        private void OnJoin(IrcConnector client, IrcController controller, IrcMessage message)
        {
            if(message.SourceNick.ToLowerNick() == Nick.ToLowerNick())
            {
                IrcChannel channel = new IrcChannel(message.Parameters[0]);
                lock (Channels)
                {
                    if (Channels.All(ch => ch.Name != message.Parameters[0])) Channels.Add(channel);
                }
            }
            else
            {
                lock (Users)
                {
                    if (Users.All(u => u.Nick.ToLowerNick() != message.SourceNick.ToLowerNick()))
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
                        Users.FirstOrDefault(u => u.Nick.ToLowerNick() == message.SourceNick.ToLowerNick())?.MutualChannels.Add(message.Parameters[0]);
                    }
                }

                lock (Channels)
                {
                    Channels.FirstOrDefault(ch => ch.Name == message.Parameters[0])?.Users.Add(message.SourceNick);
                }
            }
        }

        private void OnNamesReply(IrcConnector client, IrcController controller, IrcMessage message)
        {
            lock (Channels)
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name == message.Parameters[2]);
                if (channel != null)
                {
                    if (channel.UserCollectionComplete)
                    {
                        channel.UserCollectionComplete = false;
                        channel.Users = new List<string>();
                    }

                    string[] users = message.Parameters[3].Split(' ');
                    
                    lock (Users)
                    {
                        foreach (string user in users)
                        {
                            if (user.ToLowerNick() != Nick.ToLowerNick())
                            {
                                channel.Users.Add(user.TrimStart('@', '+'));
                                if (Users.All(u => u.Nick.ToLower() != user.ToLowerNick()))
                                {
                                    Users.Add(new IrcUser
                                    {
                                        MutualChannels = new List<string>
                                        {
                                            channel.Name
                                        },
                                        Nick = user
                                    });
                                }
                                else
                                {
                                    Users.FirstOrDefault(u => u.Nick.ToLowerNick() == user.ToLowerNick())?.MutualChannels.Add(channel.Name);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnNamesEnd(IrcConnector client, IrcController controller, IrcMessage message)
        {
            lock (Channels)
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name == message.Parameters[1]);
                if (channel != null) channel.UserCollectionComplete = true;
            }

            Who(message.Parameters[1]);
        }

        private void OnPart(IrcConnector client, IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToLowerNick() == Nick.ToLowerNick())
            {
                IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name == message.Parameters[0]);
                if (channel != null) Channels.Remove(channel);
            }
            else
            {
                //TODO update channel to remove user
                //TODO update user to remove channel
                //TODO garbage collect user if no more mutual channels
            }
        }

        private void OnTopicInform(IrcConnector client, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name == message.Parameters[1]);
            if (channel != null) channel.Topic = message.Parameters[2];
        }

        private void OnTopicUpdate(IrcConnector client, IrcController controller, IrcMessage message)
        {
            IrcChannel channel = Channels.FirstOrDefault(ch => ch.Name == message.Parameters[0]);
            if (channel != null) channel.Topic = message.Parameters[1] != "" ? message.Parameters[1] : null;
        }

        #endregion
    }
}
