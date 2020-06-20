using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using smIRCL.Config;
using smIRCL.Exceptions;

namespace smIRCL
{
    public class IrcConnector : IDisposable
    {
        #region Public Properties

        public IrcConfig Config { get; internal set; }

        public bool IsConnected { get; internal set; }

        public bool IsDisposed { get; internal set; }

        #endregion

        #region Events
        
        public event ConnectionStateEventHandler Connected;
        public event ConnectionStateEventHandler Disconnected;
        public delegate void ConnectionStateEventHandler();


        public event MessageTransmitHandler MessageTransmitted;
        public delegate void MessageTransmitHandler(string rawMessage);


        public event MessageHandler MessageReceived;
        public delegate void MessageHandler(string rawMessage, IrcMessage message);


        public event CommunicationFailedHandler TransmitFailed;
        public event CommunicationFailedHandler ReceiveFailed;
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


        public IrcConnector(IrcConfig config)
        {
            config.IsValid(true);
            Config = config;

            if (config.ConnectOnInstantiation)
            {
                Connect();
            }
        }



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
    }
}
