using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using smIRCL.Config;
using smIRCL.Exceptions;

namespace smIRCL
{
    /// <summary>
    /// An IRCv3 Client for IRC clients and bots
    /// </summary>
    public class IrcClient : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// The IrcClientConfig used to configure this IrcClient
        /// </summary>
        public IrcClientConfig ClientConfig { get; internal set; }

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

        #region Events
        
        /// <summary>
        /// Fired when the client connection has been established
        /// </summary>
        public event ConnectedEventHandler Connected;

        /// <summary>
        /// The delegate representing a handler for Connected
        /// </summary>
        public delegate void ConnectedEventHandler();




        /// <summary>
        /// Fired when any message istransmitted to the server
        /// </summary>
        public event RawMessageTransmitHandler RawMessageTransmitted;

        /// <summary>
        /// The delegate representing a handler for RawMessageTransmitted
        /// </summary>
        /// <param name="rawMessage">The string which was parsed into and IrcMessage for message</param>
        public delegate void RawMessageTransmitHandler(string rawMessage);




        /// <summary>
        /// Fired when any message is received from the server
        /// </summary>
        public event RawMessageHandler RawMessageReceived;

        /// <summary>
        /// The delegate representing a handler for RawMessageReceived
        /// </summary>
        /// <param name="rawMessage">The string which was parsed into and IrcMessage for message</param>
        /// <param name="message">The IrcMessage parsed from rawMessage</param>
        public delegate void RawMessageHandler(string rawMessage, IrcMessage message);




        /// <summary>
        /// Fired when a message fails to be transmitted
        /// </summary>
        public event TransmitReceivedFailedHandler TransmitFailed;

        /// <summary>
        /// Fired when a message fails to be received
        /// </summary>
        public event TransmitReceivedFailedHandler ReceiveFailed;

        /// <summary>
        /// The delegate representing a handler for TransmitFailedHandler
        /// </summary>
        /// <param name="exception">The exception which caused the transmission to fail</param>
        public delegate void TransmitReceivedFailedHandler(Exception exception);

        #endregion

        #region Private Properties

        private int _reconnectAttempts = 0;

        private TcpClient _remote;

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
            ClientConfig = config;

            if (config.ConnectOnInstantiation)
            {
                Connect();
            }
        }

        /// <summary>
        /// Connects the IrcClient if it is currently disconnected
        /// </summary>
        public void Connect()
        {
            CancelIfDisposed();

            Exception lastException = null;

            if (IsConnected) throw new InvalidOperationException("Cannot double connect");

            do
            {
                try
                {
                    _remote = new TcpClient(ClientConfig.ServerHostname, ClientConfig.ServerPort);

                    if (ClientConfig.UseSsl)
                    {
                        SslStream sslStream = new SslStream(_remote.GetStream(), false);
                        sslStream.AuthenticateAsClient(ClientConfig.ServerHostname);
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

                    Connected?.Invoke();
                }
                catch (Exception e)
                {
                    _reconnectAttempts++;
                    lastException = e;

                    try
                    {
                        _remoteRx.Dispose();
                    }
                    catch (Exception)
                    {
                        //ignored
                    }

                    try
                    {
                        _remoteTx.Dispose();
                    }
                    catch (Exception)
                    {
                        //ignored
                    }

                    try
                    {
                        _remoteStream.Dispose();
                    }
                    catch (Exception)
                    {
                        //ignored
                    }

                    try
                    {
                        _remote.Dispose();
                    }
                    catch (Exception)
                    {
                        //ignored
                    }

                    _remoteRx = null;
                    _remoteTx = null;
                    _remoteStream = null;
                    _remote = null;
                }
            } while (!IsConnected && _reconnectAttempts >= ClientConfig.ReconnectAttempts);

            if (_reconnectAttempts >= ClientConfig.ReconnectAttempts)
            {
                Dispose();
                throw new ConnectionFailureException("Unable to connect after specified retries. Aborting", lastException);
            }
        }

        /// <summary>
        /// Disposes of the IrcClient
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            IsConnected = false;
            _processingEngine?.Abort();
            _remoteRx?.Dispose();
            _remoteTx?.Dispose();
            _remoteStream?.Dispose();
            _remote?.Dispose();
        }

        /// <summary>
        /// Disposes of the IrcClient with an optional message
        /// </summary>
        public void Quit(string quitMessage = "Client quit")
        {
            try
            { 
                Transmit($"QUIT :{quitMessage}"); 
                Dispose();
            }
            catch (Exception)
            {
                //ignored
            }
        }

        /// <summary>
        /// Transmit a raw message to the server
        /// </summary>
        /// <param name="data">Message to transmit</param>
        public void Transmit(string data)
        {
            CancelIfDisposed();

            try
            {
                _remoteTx.WriteLine(data);
                RawMessageTransmitted?.Invoke(data);
            }
            catch (Exception e)
            {
                Dispose();
                TransmitFailed?.Invoke(e);
            }
        }

        private void CancelIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException("Operation cannot be completed on a disposed IrcClient");
        }

        private string Receive()
        {
            try
            {
                return _remoteRx.ReadLine();
            }
            catch (Exception e)
            {
                Dispose();
                ReceiveFailed?.Invoke(e);
            }

            return null;
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
                if(message != null) RawMessageReceived?.Invoke(received, message);

                /*switch (message.Command)
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
                }*/
            }
        }
    }
}
