using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using smIRCL.EventArgs;
using smIRCL.Extensions;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    public partial class IrcController
    {
        /// <summary>
        /// Processes a PING command
        /// </summary>
        private void PingHandler(IrcController controller, IrcMessage message)
        {
            Connector.Transmit($"PONG :{message.Parameters[0]}");
            
            try
            {
                Ping?.Invoke(controller, message);
            }
            catch (Exception e)
            {
                try
                {
                    OnClientError?.Invoke(this, message, e);
                }
                catch (Exception criticalException)
                {
                    throw new AggregateException("Client error handler failed", criticalException, e);
                }
            }
        }
        
        /// <summary>
        /// Processes a PRIVMSG command
        /// </summary>
        private void PrivMsgHandler(IrcController controller, IrcMessage message)
        {
            bool isChannel = controller.IsValidChannelName(message.Parameters[0]);
            
            if (!isChannel)
            {
                if (_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user))
                {
                    user.LastPrivateMessage = DateTime.Now;
                }
                else
                {
                    _users.Add(message.SourceNick.ToIrcLower(), new IrcUser(this)
                    {
                        HostMask = message.SourceHostMask,
                        Nick = message.SourceNick,
                        Host = message.SourceHost,
                        UserName = message.SourceUserName,
                        LastPrivateMessage = DateTime.Now
                    });
                    
                    WhoIs(message.SourceNick);
                }
            }
            
            try
            {
                if (message.Parameters[1].StartsWith("\x01"))
                {
                    if (isChannel)
                    {
                        OnChannelCtcpMessage?.Invoke(controller, new ChannelCtcpMessageEventArgs(controller, message));
                    }
                    else
                    {
                        OnPrivateCtcpMessage?.Invoke(controller, new PrivateCtcpMessageEventArgs(controller, message));
                    }
                }
                else
                {
                    if (isChannel)
                    {
                        OnChannelMessage?.Invoke(controller, new ChannelMessageEventArgs(controller, message));
                    }
                    else
                    {
                        OnPrivateMessage?.Invoke(controller, new PrivateMessageEventArgs(controller, message));
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    OnClientError?.Invoke(this, message, e);
                }
                catch (Exception criticalException)
                {
                    throw new AggregateException("Client error handler failed", criticalException, e);
                }
            }
        }
        
        /// <summary>
        /// Processes a NOTICE command
        /// </summary>
        private void NoticeHandler(IrcController controller, IrcMessage message)
        {
            bool isChannel = controller.IsValidChannelName(message.Parameters[0]);
            
            if (!isChannel)
            {
                if (_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user))
                {
                    user.LastPrivateMessage = DateTime.Now;
                }
                else
                {
                    _users.Add(message.SourceNick.ToIrcLower(), new IrcUser(this)
                    {
                        HostMask = message.SourceHostMask,
                        Nick = message.SourceNick,
                        Host = message.SourceHost,
                        UserName = message.SourceUserName,
                        LastPrivateMessage = DateTime.Now
                    });
                    
                    WhoIs(message.SourceNick);
                }
            }
            
            try
            {
                if (isChannel)
                {
                    OnChannelNotice?.Invoke(controller, new ChannelNoticeEventArgs(controller, message));
                }
                else
                {
                    OnPrivateNotice?.Invoke(controller, new PrivateNoticeEventArgs(controller, message));
                }
            }
            catch (Exception e)
            {
                try
                {
                    OnClientError?.Invoke(this, message, e);
                }
                catch (Exception criticalException)
                {
                    throw new AggregateException("Client error handler failed", criticalException, e);
                }
            }
        }
        
        /// <summary>
        /// Processes an ERROR command
        /// </summary>
        private void UnrecoverableErrorHandler(IrcController controller, IrcMessage message)
        {
            Connector.Dispose();
        }
        
        /// <summary>
        /// Process one of these commands:
        ///     431 ERR_NONICKNAMEGIVEN
        ///     432 ERR_ERRONEUSNICKNAME
        ///     433 ERR_NICKNAMEINUSE
        ///     436 ERR_NICKCOLLISION
        /// </summary>
        private void NickErrorHandler(IrcController controller, IrcMessage message)
        {
            //If the current nick is null, we have no nickname and should try others, quitting in the worst case. If it isn't null, we have a nickname and this error was from attempting to change nick
            if (Nick != null) return;
            
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
        
        /// <summary>
        /// Process a NICK command
        /// </summary>
        private void NickSetHandler(IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower()) //Minimum requirement of a source is Nick which is always unique
            {
                Nick = message.Parameters[0];
            }
            else if (_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user))
            {
                user.Nick = message.Parameters[0];
            }

            WhoIs(message.Parameters[0]);
        }
        
        /// <summary>
        /// Process a 004 RPL_MYINFO command. Receiving this command confirms the nick we requested is now ours
        /// </summary>
        private void WelcomeEndHandler(IrcController controller, IrcMessage message)
        {
            Nick = _unconfirmedNick;
            _unconfirmedNick = null;
            WhoIs(Nick);
        }
        
        /// <summary>
        /// Process a 372 RPL_MOTD command
        /// </summary>
        private void MotdPartHandler(IrcController controller, IrcMessage message)
        {
            //TODO if a manual MOTD is fired, should we clear the existing known message?
            if (ServerMotd == null)
            {
                ServerMotd = message.Parameters[1];
            }
            else
            {
                ServerMotd += "\n" + message.Parameters[1];
            }
        }
        
        /// <summary>
        /// Process a 376 RPL_ENDOFMOTD command. Receiving this command is considered indication that connection establishment is complete, so the client will indicate it is ready
        /// </summary>
        private void MotdEndHandler(IrcController controller, IrcMessage message)
        {
            //TODO if a manual MOTD is fired, should we really be Ready() ing?
            ControllerReady(); //Now safe to assume ISupport has been received and processed
        }
        
        /// <summary>
        /// Process a 422 ERR_NOMOTD command. Receiving this command is considered indication that connection establishment is complete, so the client will indicate it is ready
        /// </summary>
        private void NoMotdHandler(IrcController controller, IrcMessage message)
        {
            //TODO if a manual MOTD is fired, should we really be Ready() ing?
            ControllerReady(); //Now safe to assume ISupport has been received and processed
        }
        
        /// <summary>
        /// Process a 352 RPL_WHOREPLY command
        /// </summary>
        private void WhoReplyHandler(IrcController controller, IrcMessage message)
        {
            char[] statuses = message.Parameters[6].ToCharArray();

            if (message.Parameters[5].ToIrcLower() == Nick.ToIrcLower()) //If the target user is us
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
                RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];

                if (!_channels.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcChannel channel)) return;
                
                channel.ClientModesInternal.Clear();

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

                        default:
                            if (SupportedUserPrefixes.Any(sup => sup.Value == status))
                            {
                                channel.ClientModesInternal.Add(SupportedUserPrefixes.FirstOrDefault(sup => sup.Value == status).Key);
                            }
                            break;
                    }
                }
            }
            else if (_users.TryGetValue(message.Parameters[5].ToIrcLower(), out IrcUser user)) //If we know of the existence of the target user
            {
                user.UserName = message.Parameters[2];
                user.Host = message.Parameters[3];
                user.RealName = message.Parameters[7].Split(new[] { ' ' }, 2)[1];

                KeyValuePair<string, List<char>> userMutualChannelModes = user.MutualChannelModes.FirstOrDefault(mcm => mcm.Key.ToIrcLower() == message.Parameters[1].ToIrcLower());
                if (userMutualChannelModes.Key != null) userMutualChannelModes.Value.Clear();

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

                        default:
                            if (SupportedUserPrefixes.Any(sup => sup.Value == status) && userMutualChannelModes.Key != null)
                            {
                                userMutualChannelModes.Value.Add(SupportedUserPrefixes.FirstOrDefault(sup => sup.Value == status).Key);
                            }
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Process a 311 RPL_WHOISUSER command
        /// </summary>
        private void WhoIsUserReplyHandler(IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() == Nick.ToIrcLower()) //If the target user is us
            {
                UserName = message.Parameters[2];
                Host = message.Parameters[3];
                RealName = message.Parameters[5];
            }
            else if (_users.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcUser user)) //If we know of the existence of the target user
            {
                user.UserName = message.Parameters[2];
                user.Host = message.Parameters[3];
                user.RealName = message.Parameters[5];
            }
        }
        
        /// <summary>
        /// Process a 319 RPL_WHOISCHANNELS command
        /// </summary>
        private void WhoIsChannelsReplyHandler(IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() != Nick.ToIrcLower() && _users.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcUser user)) //If we know of the existence of the target user and it is not us
            {
                string[] channels = message.Parameters[2].Split(' ');
                List<string> trimmedChannels = new List<string>();
                
                foreach (string channel in channels) trimmedChannels.Add(channel.TrimStart('@', '+'));

                foreach (string channel in trimmedChannels)
                {
                    if (!_channels.ContainsKey(channel.ToIrcLower())) continue;
                    
                    if (user.MutualChannels.All(uch => uch != channel.ToIrcLower())) user.MutualChannelsInternal.Add(channel.ToIrcLower());
                }
            }
        }
        
        /// <summary>
        /// Process a 396 RPL_HOSTHIDDEN command
        /// </summary>
        private void HostMaskCloakHandler(IrcController controller, IrcMessage message)
        {
            Host = message.Parameters[1];
        }
        
        /// <summary>
        /// Process a JOIN commmand
        /// </summary>
        private void JoinHandler(IrcController controller, IrcMessage message)
        {
            if(message.SourceNick.ToIrcLower() == Nick.ToIrcLower()) //If the target user is us
            {
                if (!_channels.ContainsKey(message.Parameters[0].ToIrcLower())) _channels.Add(message.Parameters[0].ToIrcLower(), new IrcChannel(this, message.Parameters[0], SupportedChannelTypes)); //Add to our channel list if it isn't on there already
            }
            else
            {
                if (_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user)) //If we dont already know of the existence of the target user
                {
                    user.MutualChannelsInternal.Add(message.Parameters[0].ToIrcLower());
                    user.MutualChannelModesInternal.Add(new KeyValuePair<string, List<char>>(message.Parameters[0], new List<char>()));
                }
                else
                {
                    string realName = null;
                    string identifiedAccount = null;

                    if (NegotiatedCapabilities.Any(cap => cap.ToIrcLower() == "extended-join")) //If the extended-join capability is enabled, so JOINs use that format
                    {
                        if (message.Parameters[1] != "*") identifiedAccount = message.Parameters[1];
                        realName = message.Parameters[2];
                    }

                    IrcUser newUser = new(this)
                    {
                        HostMask = message.SourceHostMask,
                        Nick = message.SourceNick,
                        Host = message.SourceHost,
                        UserName = message.SourceUserName,
                        RealName = realName,
                        IdentifiedAccount = identifiedAccount
                    };
                    
                    newUser.MutualChannelsInternal.Add(message.Parameters[0].ToIrcLower()); //This is the first time we know of the user, so we initialise them with the current mutual channel as their only known mutual
                    newUser.MutualChannelModesInternal.Add(new KeyValuePair<string, List<char>>(message.Parameters[0].ToIrcLower(), new List<char>()));
                    
                    _users.Add(message.SourceNick.ToIrcLower(), newUser);
                    
                    WhoIs(message.SourceNick); //Get more info on the user
                }

                if (_channels.TryGetValue(message.Parameters[0].ToIrcLower(), out IrcChannel channel)) channel.UsersInternal.Add(message.SourceNick);
            }
        }
        
        /// <summary>
        /// Process a 353 RPL_NAMREPLY command
        /// </summary>
        private void NamesReplyHandler(IrcController controller, IrcMessage message)
        {
            if (!_channels.TryGetValue(message.Parameters[2].ToIrcLower(), out IrcChannel channel)) return;
            
            if (channel.UserCollectionComplete)
            {
                channel.UserCollectionComplete = false;
                channel.UsersInternal.Clear(); //Clear users for repopulation
            }

            string[] users = message.Parameters[3].Split(' ');

            foreach (string user in users)
            {
                string userHostMaskOrNickOnly = user.TrimStart(_trimmableUserPrefixes.ToArray());
                string userHost = null;
                string userUserName = null;
                
                string[] detailsAndHost = userHostMaskOrNickOnly.Split('@');
                if (detailsAndHost.Length > 1) userHost = detailsAndHost[1];

                string[] nickAndUserName = detailsAndHost[0].Split('!');
                if (nickAndUserName.Length > 1) userUserName = nickAndUserName[1];

                string userNick = nickAndUserName[0];

                if (userNick.ToIrcLower() != Nick.ToIrcLower())
                {
                    //Add nick to user list
                    channel.UsersInternal.Add(userNick);
                    
                    //Get user prefixes
                    List<char> userPrefixes = new List<char>();
                    string userSplice = user;

                    foreach (char trimmableUserPrefix in _trimmableUserPrefixes)
                    {
                        if (!userSplice.StartsWith(trimmableUserPrefix.ToString())) continue;
                        
                        userPrefixes.Add(trimmableUserPrefix);
                        userSplice = userSplice.TrimStart(trimmableUserPrefix);
                    }

                    //Get user modes
                    List<char> userModes = new List<char>();

                    foreach (char prefix in userPrefixes)
                    {
                        if (SupportedUserPrefixes.Any(p => p.Value == prefix)) userModes.Add(SupportedUserPrefixes.FirstOrDefault(p => p.Value == prefix).Key);
                    }

                    //Check user against cache
                    if (_users.TryGetValue(userNick.ToIrcLower(), out IrcUser globalUser))
                    {
                        if (globalUser.MutualChannels.All(ch => ch != channel.Name.ToIrcLower())) globalUser.MutualChannelsInternal.Add(channel.Name.ToIrcLower());
                        
                        if (globalUser.MutualChannelModes.All(ch => ch.Key.ToIrcLower() != channel.Name.ToIrcLower())) globalUser.MutualChannelModesInternal.Add(new KeyValuePair<string, List<char>>(channel.Name, userModes));
                    }
                    else
                    {
                        IrcUser newUser = new(this)
                        {
                            Nick = userNick,
                            HostMask = userHost != null ? userHostMaskOrNickOnly : null,
                            Host = userHost,
                            UserName = userUserName
                        };
                        
                        newUser.MutualChannelsInternal.Add(channel.Name.ToIrcLower());
                        newUser.MutualChannelModesInternal.Add(new KeyValuePair<string, List<char>>(channel.Name.ToIrcLower(), userModes));
                        
                        _users.Add(userNick.ToIrcLower(), newUser);
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
                    
                    channel.ClientModesInternal.Clear();

                    foreach (char prefix in userPrefixes)
                    {
                        if (SupportedUserPrefixes.Any(p => p.Value == prefix)) channel.ClientModesInternal.Add(SupportedUserPrefixes.FirstOrDefault(p => p.Value == prefix).Key);
                    }
                }
            }
        }
        
        /// <summary>
        /// Process a 366 RPL_ENDOFNAMES command
        /// </summary>
        private void NamesEndHandler(IrcController controller, IrcMessage message)
        {
            if (_channels.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcChannel channel)) channel.UserCollectionComplete = true;

            Who(message.Parameters[1]);
        }
        
        /// <summary>
        /// Process a 324 RPL_CHANNELMODEIS command
        /// </summary>
        private void ChannelModesHandler(IrcController controller, IrcMessage message)
        {
            if (!_channels.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcChannel channel)) return;
            
            char[] channelModes = message.Parameters[2].TrimStart('+').ToCharArray(); //TODO what if this is - ?
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
                    channel.ModesInternal.RemoveAll(m => m.Key == channelMode);
                    channel.ModesInternal.Add(new KeyValuePair<char, string>(channelMode, channelModeParameters.Dequeue()));
                }
                else if (SupportedChannelModes.D.Contains(channelMode))
                {
                    channel.ModesInternal.RemoveAll(m => m.Key == channelMode);
                    channel.ModesInternal.Add(new KeyValuePair<char, string>(channelMode, null));
                }
            }
        }
        
        /// <summary>
        /// Process a MODE command
        /// </summary>
        private void ModeHandler(IrcController controller, IrcMessage message)
        {
            if (!_channels.TryGetValue(message.Parameters[0].ToIrcLower(), out IrcChannel channel)) return;
            
            char[] sentModes = message.Parameters[1].ToCharArray();
            bool removal = false;

            Queue<string> otherParams = new Queue<string>();

            for (int i = 2; i < message.Parameters.Count; i++) otherParams.Enqueue(message.Parameters[i]);

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

                if (SupportedChannelModes.A.Contains(sentMode)) //A type chanmode?
                {
                    otherParams.Dequeue(); //Not listening for A, discard
                }
                else if (SupportedChannelModes.B.Contains(sentMode) || SupportedChannelModes.C.Contains(sentMode)) //B or C type chanmode?
                {
                    channel.ModesInternal.RemoveAll(m => m.Key == sentMode); //If removal, remove, otherwise, dedupe (technically remove all, but added again below)
                        
                    if (!removal) channel.ModesInternal.Add(new KeyValuePair<char, string>(sentMode, otherParams.Dequeue()));
                }
                else if (SupportedChannelModes.D.Contains(sentMode)) //D type chanmode?
                {
                    channel.ModesInternal.RemoveAll(m => m.Key == sentMode); //If removal, remove, otherwise, dedupe (technically remove all, but added again below)
                        
                    if (!removal) channel.ModesInternal.Add(new KeyValuePair<char, string>(sentMode, null));
                }
                else if (SupportedUserPrefixes.Any(sup => sup.Key == sentMode)) //Channel specific usermode?
                {
                    string targetNick = otherParams.Dequeue();

                    if (Nick.ToIrcLower() == targetNick.ToIrcLower()) //If the target user is us
                    {
                        channel.ClientModesInternal.RemoveAll(m => m == sentMode); //If removal, remove, otherwise, dedupe (technically remove all, but added again below)
                            
                        if (!removal) channel.ClientModesInternal.Add(sentMode);
                    }
                    else //If we know of the existence of the target user and it is not us
                    {
                        if (_users.TryGetValue(targetNick.ToIrcLower(), out IrcUser user))
                        {
                            KeyValuePair<string, List<char>> mutualChannelModes = user.MutualChannelModes.FirstOrDefault(mcm => mcm.Key.ToIrcLower() == channel.Name.ToIrcLower()); //Find the mutual channel for the known user
                                
                            mutualChannelModes.Value?.RemoveAll(m => m == sentMode); //If removal, remove, otherwise, dedupe (technically remove all, but added again below)
                                
                            if (!removal) mutualChannelModes.Value?.Add(sentMode);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Process a PART command
        /// </summary>
        private void PartHandler(IrcController controller, IrcMessage message)
        {
            string lowerNamedChannel = message.Parameters[0].ToIrcLower();
            
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                _channels.Remove(lowerNamedChannel);

                IEnumerable<KeyValuePair<string, IrcUser>> usersWithMutualChannels = _users.Where(u => u.Value.MutualChannels.Any(ch => ch == lowerNamedChannel));
                
                foreach (KeyValuePair<string, IrcUser> user in usersWithMutualChannels) user.Value.MutualChannelsInternal.RemoveAll(ch => ch == lowerNamedChannel);
            }
            else
            {
                if (_channels.TryGetValue(lowerNamedChannel, out IrcChannel channel))  channel.UsersInternal.RemoveAll(u => u == message.SourceNick.ToIrcLower());

                if (_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user)) user.MutualChannelsInternal.RemoveAll(ch => ch == lowerNamedChannel);
            }

            DoUserGarbageCollection();
        }
        
        /// <summary>
        /// Process a KICK command
        /// </summary>
        private void KickHandler(IrcController controller, IrcMessage message)
        {
            if (message.Parameters[1].ToIrcLower() == Nick.ToIrcLower())
            {
                _channels.Remove(message.Parameters[0].ToIrcLower());

                IEnumerable<KeyValuePair<string, IrcUser>> usersWithMutualChannels = _users.Where(u => u.Value.MutualChannels.Any(ch => ch.ToIrcLower() == message.Parameters[0].ToIrcLower()));
                
                foreach (KeyValuePair<string, IrcUser> user in usersWithMutualChannels)
                {
                    user.Value.MutualChannelsInternal.RemoveAll(ch => ch == message.Parameters[0].ToIrcLower());
                }
            }
            else
            {
                if (_channels.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcChannel channel)) channel.UsersInternal.RemoveAll(u => u == message.Parameters[1].ToIrcLower());

                if (_users.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcUser user)) user.MutualChannelsInternal.RemoveAll(ch => ch == message.Parameters[0].ToIrcLower());
            }

            DoUserGarbageCollection();
        }
        
        /// <summary>
        /// Process a QUIT command
        /// </summary>
        private void QuitHandler(IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower()) return;
            
            IEnumerable<KeyValuePair<string, IrcChannel>> mutualChannels = _channels.Where(ch => ch.Value.UsersInternal.Any(u => u == message.SourceNick.ToIrcLower()));
            
            foreach (KeyValuePair<string, IrcChannel> mutualChannel in mutualChannels) mutualChannel.Value.UsersInternal.RemoveAll(u => u == message.SourceNick.ToIrcLower());

            _users.Remove(message.SourceNick.ToIrcLower());
        }
        
        /// <summary>
        /// Process a 332 RPL_TOPIC command
        /// </summary>
        private void TopicInformHandler(IrcController controller, IrcMessage message)
        {
            if (_channels.TryGetValue(message.Parameters[1].ToIrcLower(), out IrcChannel channel)) channel.Topic = message.Parameters[2];
        }
        
        /// <summary>
        /// Process a TOPIC command
        /// </summary>
        private void TopicUpdateHandler(IrcController controller, IrcMessage message)
        {
            if (_channels.TryGetValue(message.Parameters[0].ToIrcLower(), out IrcChannel channel)) channel.Topic = message.Parameters[1] != "" ? message.Parameters[1] : null;
        }
        
        /// <summary>
        /// Process a 005 RPL_ISUPPORT command
        /// </summary>
        private void ServerISupportHandler(IrcController controller, IrcMessage message)
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
        
        /// <summary>
        /// Process a CAP command
        /// </summary>
        private void CapabilityHandler(IrcController controller, IrcMessage message)
        {
            switch (message.Parameters[1].ToIrcLower())
            {
                case "ls": //Receive the list of available capabilities
                    string[] capabilitiesGiven = message.Parameters[message.Parameters.Count - 1].Split(' ');

                    foreach (string cap in capabilitiesGiven)
                    {
                        string[] capabilityAndParameters = cap.Split('=');
                        List<string> parameters = capabilityAndParameters.Length > 1 ? capabilityAndParameters[1].Split(',').ToList() : new List<string>();

                        if (AvailableCapabilities.All(acap => acap.Key != capabilityAndParameters[0])) AvailableCapabilities.Add(new KeyValuePair<string, List<string>>(capabilityAndParameters[0], parameters));
                    }

                    if (message.Parameters[2] != "*") DoNextCapabilityCompletionStep();
                    break;

                case "ack": //Receive a list of capabilities which were both requested by us, and acknowledged by the server
                    string[] capabilitiesAcknowledged = message.Parameters[message.Parameters.Count - 1].Split(' ');

                    foreach (string cap in capabilitiesAcknowledged)
                    {
                        if (NegotiatedCapabilities.All(ncap => ncap != cap.ToIrcLower())) NegotiatedCapabilities.Add(cap.ToIrcLower());
                    }

                    DoNextCapabilityCompletionStep();
                    break;
            }
        }
        
        /// <summary>
        /// Process an AUTHENTICATE command
        /// </summary>
        private void SaslHandler(IrcController controller, IrcMessage message)
        {
            switch (message.Parameters[0].ToIrcLower())
            {
                case "+":
                    Connector.Transmit($"AUTHENTICATE {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Connector.Config.AuthUsername}\x00{Connector.Config.AuthUsername}\x00{Connector.Config.AuthPassword}"))}");
                    //On success, will lead to 903 RPL_SASLSUCCESS
                    break;
            }
        }
        
        /// <summary>
        /// Process a 903 RPL_SASLSUCCESS command
        /// </summary>
        private void SaslCompleteHandler(IrcController controller, IrcMessage message)
        {
            DoNextCapabilityCompletionStep();
        }
        
        /// <summary>
        /// Process one of these commands:
        ///     901 RPL_LOGGEDOUT
        ///     902 ERR_NICKLOCKED
        ///     904 ERR_SASLFAIL
        ///     905 ERR_SASLTOOLONG
        ///     906 ERR_SASLABORTED
        ///     907 ERR_SASLALREADY
        /// </summary>
        private void SaslFailureHandler(IrcController controller, IrcMessage message)
        {
            Quit("SASL authentication has failed");
        }
        
        /// <summary>
        /// Process an AWAY command
        /// </summary>
        private void AwayNotifyHandler(IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                Away = message.Parameters.Count == 0 ? null : message.Parameters[0];
            }
            else
            {
                if (!_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user)) return;
                
                user.Away = message.Parameters.Count == 0 ? null : message.Parameters[0];
            }
        }
        
        /// <summary>
        /// Process a CHGHOST command
        /// </summary>
        private void ChangeHostHandler(IrcController controller, IrcMessage message)
        {
            if (message.SourceNick.ToIrcLower() == Nick.ToIrcLower())
            {
                UserName = message.Parameters[0];
                Host = message.Parameters[1];
            }
            else
            {
                if (!_users.TryGetValue(message.SourceNick.ToIrcLower(), out IrcUser user)) return;
                
                user.UserName = message.Parameters[0];
                user.HostMask = message.Parameters[1];
            }
        }
    }
}