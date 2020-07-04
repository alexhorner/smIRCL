using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using smIRCL.Config;
using smIRCL.Exceptions;
using smIRCL.ServerEntities;

namespace smIRCL.Core
{
    public class IrcConnector : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// The configuration for the IRC server connection and any attached controller
        /// </summary>
        public IrcConfig Config { get; internal set; }

        /// <summary>
        /// Whether there is a live IRC server connection
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Whether this connector has been disposed and can no longer be used
        /// </summary>
        public bool IsDisposed { get; internal set; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when a connection to an IRC server is established
        /// </summary>
        public event ConnectionStateEventHandler Connected;
        /// <summary>
        /// Fired when a connection to an IRC server is lost
        /// </summary>
        public event ConnectionStateEventHandler Disconnected;
        /// <summary>
        /// The handler for connect and disconnect events
        /// </summary>
        public delegate void ConnectionStateEventHandler();

        /// <summary>
        /// Fired when a message is sent to the IRC server
        /// </summary>
        public event MessageTransmitHandler MessageTransmitted;
        /// <summary>
        /// The handler for a message transmit
        /// </summary>
        /// <param name="rawMessage">The raw IRC message transmitted</param>
        public delegate void MessageTransmitHandler(string rawMessage);

        /// <summary>
        /// Fired when a message is received from the IRC server
        /// </summary>
        public event MessageHandler MessageReceived;
        /// <summary>
        /// The handler for a message receive
        /// </summary>
        /// <param name="rawMessage">The raw IRC message received</param>
        /// <param name="message">The parsed IRC message received</param>
        public delegate void MessageHandler(string rawMessage, IrcMessage message);

        /// <summary>
        /// Fired when a transmit fails to complete
        /// </summary>
        public event CommunicationFailedHandler TransmitFailed;
        /// <summary>
        /// Fired when a receive fails to complete
        /// </summary>
        public event CommunicationFailedHandler ReceiveFailed;
        /// <summary>
        /// The handler for a message transmit or receive failure
        /// </summary>
        /// <param name="exception">The exception describing the failure</param>
        public delegate void CommunicationFailedHandler(Exception exception);

        #endregion

        #region Private Properties

        private TcpClient _remote;

        private Stream _remoteStream;
        private StreamReader _remoteRx;
        private StreamWriter _remoteTx;

        private Thread _processingEngine;

        private int _reconnectAttempts;

        #endregion


        /// <summary>
        /// Instantiates a new IRC server connector
        /// </summary>
        /// <param name="config">The configuration for the IRC server connection and any controller attached</param>
        public IrcConnector(IrcConfig config)
        {
            config.IsValid(true);
            Config = config;

            if (config.ConnectOnInstantiation)
            {
                Connect();
            }
        }


        #region Public Methods

        /// <summary>
        /// Begin establishing a connection to the configured IRC server
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
                    _remote = new TcpClient(Config.ServerHostname, Config.ServerPort);

                    if (Config.UseSsl)
                    {
                        SslStream sslStream = new SslStream(_remote.GetStream(), false);
                        sslStream.AuthenticateAsClient(Config.ServerHostname);
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
            } while (!IsConnected && _reconnectAttempts >= Config.ReconnectAttempts);

            if (_reconnectAttempts >= Config.ReconnectAttempts)
            {
                Dispose();
                throw new ConnectionFailureException("Unable to connect after specified retries. Aborting", lastException);
            }
        }

        /// <summary>
        /// Disconnect from the IRC server if connected and dispose
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            IsConnected = false;
            Disconnected?.Invoke();
            //_processingEngine?.Abort(); //TODO not working as not supported?
            _remoteRx?.Dispose();
            _remoteTx?.Dispose();
            _remoteStream?.Dispose();
            _remote?.Dispose();
        }

        /// <summary>
        /// Transmit a raw message to the IRC server
        /// </summary>
        /// <param name="data">The raw IRC message to transmit</param>
        public void Transmit(string data)
        {
            CancelIfDisposed();

            try
            {
                _remoteTx.WriteLine(data);
                MessageTransmitted?.Invoke(data);
            }
            catch (Exception e)
            {
                Dispose();
                TransmitFailed?.Invoke(e);
            }
        }

        #endregion

        #region Private Methods

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
            while (IsConnected)
            {
                string received = Receive(); //TODO this isn't actually stopping when the reader is disposed
                IrcMessage message = IrcMessage.Parse(received);
                if (message != null) MessageReceived?.Invoke(received, message);
            }
        }

        #endregion
    }
}
