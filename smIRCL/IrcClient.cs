using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using smIRCL.Config;

namespace smIRCL
{
    public class IrcClient : IDisposable
    {
        #region Public State Properties

        /// <summary>
        /// Indicates whether the IrcClient has an active connection to a server
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Indicates that the clieIrcClientnt has been disposed and is no longer usable
        /// </summary>
        public bool IsDisposed { get; internal set; }

        /// <summary>
        /// The Nick of the IrcClient after the server has confirmed and registered it, otherwise null
        /// </summary>
        public string Nick { get; internal set; } = null;

        /// <summary>
        /// The Username of the IrcClient after the server has confirmed and registered it, otherwise null
        /// </summary>
        public string Username { get; internal set; } = null;

        /// <summary>
        /// The RealName of the IrcClient after the server has confirmed and registered it, otherwise null
        /// </summary>
        public string RealName { get; set; } = null;

        #endregion

        #region Private  Properties

        private int _reconnectAttempts = 0;

        private TcpClient _remote;

        private IrcClientConfig _clientConfig;

        private Stream _remoteStream;
        private StreamReader _remoteRx;
        private StreamWriter _remoteTx;

        private Thread _processingEngine;

        //private Thread TxThread;
        //private Thread RxThread;

        #endregion

        /// <summary>
        /// Instantiate an IRCv3 Client for IRC clients and bots
        /// </summary>
        /// <param name="config">The configuration for this client</param>
        public IrcClient(IrcClientConfig config)
        {
            config.IsValid(true);
            _clientConfig = config;

            if (config.ConnectOnInstantiation)
            {
                Connect();
            }

            /*RxThread = new Thread(RunOutput);
            RxThread.Start();*/

            /*switch (config.AuthMode)
            {
                case AuthMode.NickServ:
                    Thread.Sleep(3000);
                    _remoteTx.WriteLine($"PRIVMSG NickServ : IDENTIFY {config.AuthPassword}");
                    break;

                default: break;
            }*/


            /*TxThread = new Thread(RunInput);
            TxThread.Start();*/
        }

        /// <summary>
        /// Connects the IrcClient if it is currently disconnected
        /// </summary>
        public void Connect()
        {
            CancelIfDisposed();

            while (!IsConnected && _reconnectAttempts >= _clientConfig.ReconnectAttempts)
            {
                _reconnectAttempts++;

                try
                {
                    _remote = new TcpClient(_clientConfig.ServerHostname, _clientConfig.ServerPort);

                    if (_clientConfig.UseSsl)
                    {
                        SslStream sslStream = new SslStream(_remote.GetStream(), false);
                        sslStream.AuthenticateAsClient(_clientConfig.ServerHostname);
                        _remoteStream = sslStream;
                    }
                    else
                    {
                        _remoteStream = _remote.GetStream();
                    }

                    _remoteRx = new StreamReader(_remoteStream);
                    _remoteTx = new StreamWriter(_remoteStream)
                    {
                        NewLine = "\r\n",
                        AutoFlush = true
                    };

                    _processingEngine = new Thread(RunProcessingEngine);
                    _processingEngine.Start();

                    IsConnected = true;

                    Register();
                }
                catch (Exception)
                {
                    try
                    {
                        _remoteRx.Dispose();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    try
                    {
                        _remoteTx.Dispose();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    try
                    {
                        _remoteStream.Dispose();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    try
                    {
                        _remote.Dispose();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    _remoteRx = null;
                    _remoteTx = null;
                    _remoteStream = null;
                    _remote = null;
                }
            }
        }

        /// <summary>
        /// Disposes of the IrcClient
        /// </summary>
        public void Dispose()
        {
            _remoteRx?.Dispose();
            _remoteTx?.Dispose();
            _remoteStream?.Dispose();
            _remote?.Dispose();
        }

        private void CancelIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException("Operation cannot be completed on a disposed IrcClient");
        }

        private void Transmit(string data)
        {
            try
            {
                _remoteTx.WriteLine(data);
            }
            catch (Exception e)
            {

            }
        }

        private string Receive()
        {
            return "";
        }

        private void RunProcessingEngine()
        {
            /*while (_remote.Connected)
            {
                string res = _remoteRx.ReadLine();
                if (res != null)
                {
                    Console.WriteLine(res);
                }
            }*/

            while (IsConnected)
            {
                string received = Receive();
                IrcMessage message = IrcMessage.Parse(received);

                switch (message.Command)
                {
                    case Numerics.ERR_NICKCOLLISION:
                    case Numerics.ERR_NICKNAMEINUSE:
                    case Numerics.ERR_ERRONEUSNICKNAME:
                        if (Nick == null)
                        {
                            CycleNick();
                        }
                        break;

                    case "NICK":
                        Nick = message.Parameters[0];
                        break;
                }
            }
        }

        private void Register()
        {
            Transmit($"NICK {_clientConfig.Nick}");
            Transmit($"USER {_clientConfig.UserName} 0 * :{_clientConfig.RealName}");
        }

        private void CycleNick()
        {
            if (_clientConfig.AlternativeNicks.Count > 0)
            {
                Transmit($"NICK {_clientConfig.AlternativeNicks.Dequeue()}");
            }
            else
            {
                Transmit("QUIT");
            }
        }

        /*private void RunInput()
        {
            while (_remote.Connected)
            {
                _remoteTx.WriteLine(Console.ReadLine());
            }
        }*/
    }
}
