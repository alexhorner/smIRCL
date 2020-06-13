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
        private readonly TcpClient _remote = new TcpClient();

        private IrcClientConfig _clientConfig;

        private readonly Stream _remoteStream;
        private readonly StreamReader _remoteRx;
        private readonly StreamWriter _remoteTx;

        private readonly Thread TxThread;
        private readonly Thread RxThread;

        /// <summary>
        /// Instantiate an IRCv3 Client for IRC clients and bots
        /// </summary>
        /// <param name="config">The configuration for this client</param>
        public IrcClient(IrcClientConfig config)
        {
            config.IsValid(true);

            _clientConfig = config;

            _remote.Connect(config.ServerHostname, config.ServerPort);

            if (config.UseSsl)
            {
                SslStream sslStream = new SslStream(_remote.GetStream(), false);
                sslStream.AuthenticateAsClient(config.ServerHostname);
                _remoteStream = sslStream;
            }
            else
            {
                _remoteStream = _remote.GetStream();
            }

            _remoteRx = new StreamReader(_remoteStream);
            _remoteTx = new StreamWriter(_remoteStream);
            _remoteTx.NewLine = "\r\n";
            _remoteTx.AutoFlush = true;

            RxThread = new Thread(RunOutput);
            RxThread.Start();


            _remoteTx.WriteLine($"USER {config.UserName} 0 * :{config.RealName}");
            _remoteTx.WriteLine($"NICK {config.Nick}");

            /*switch (config.AuthMode)
            {
                case AuthMode.NickServ:
                    Thread.Sleep(3000);
                    _remoteTx.WriteLine($"PRIVMSG NickServ : IDENTIFY {config.AuthPassword}");
                    break;

                default: break;
            }*/


            TxThread = new Thread(RunInput);
            TxThread.Start();
        }

        public void Dispose()
        {
            _remoteRx?.Dispose();
            _remoteTx?.Dispose();
            _remoteStream?.Dispose();
            _remote?.Dispose();
        }

        private void RunOutput()
        {
            while (_remote.Connected)
            {
                string res = _remoteRx.ReadLine();
                if (res != null)
                {
                    Console.WriteLine(res);
                }
            }
        }

        private void RunInput()
        {
            while (_remote.Connected)
            {
                _remoteTx.WriteLine(Console.ReadLine());
            }
        }
    }
}
