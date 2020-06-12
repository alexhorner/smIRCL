using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using smIRCL.Enums;

namespace smIRCL
{
    public class Client
    {
        private TcpClient _remote;
        private Stream _remoteStream;
        private StreamReader _remoteRx;
        private StreamWriter _remoteTx;

        /// <summary>
        /// Instantiate an IRCv3 Client for IRC clients and bots
        /// </summary>
        /// <param name="serverHostname">The DNS hostname or IP address of the server to connect to</param>
        /// <param name="nick">The nickname to connect as</param>
        /// <param name="port">The port to connect to</param>
        /// <param name="useSSL">Is the specified port SSL</param>
        /// <param name="password">Optional password to use when authMode is set</param>
        /// <param name="authMode">What AuthMode to use. Will default to None if password is empty</param>
        public Client(string serverHostname, string nick, int port = 6667, bool useSSL = false, string password = "", AuthMode authMode = AuthMode.None)
        {
            if (password == "")
            {
                authMode = AuthMode.None;
            }

            _remote = new TcpClient(serverHostname, port);

            if (useSSL)
            {
                _remoteStream = new SslStream(_remote.GetStream(), false);
            }
            else
            {
                _remoteStream = _remote.GetStream();
            }

            _remoteRx = new StreamReader(_remoteStream);
            _remoteTx = new StreamWriter(_remoteStream);
        }
    }
}
