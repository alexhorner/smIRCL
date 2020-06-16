using System;
using smIRCL;
using smIRCL.Config;

namespace smIRCLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IrcConnector connector = new IrcConnector(new IrcConfig
            {
                ServerHostname = "irc.server.net",
                ServerPort = 6697,
                Nick = "smIRCL",
                UserName = "smIRCL",
                RealName = "smIRCL Demo",
                UseSsl = true
            });

            IrcController controller = new IrcController(connector);



            connector.MessageReceived += ConnectorMessageReceived;
            connector.MessageTransmitted += ConnectorOnMessageTransmitted;

            connector.Connect();

            while (!connector.IsDisposed)
            {
                string msg = Console.ReadLine();
                if (msg == "break")
                {

                }
                else
                {
                    connector.Transmit(msg);
                }
            }
        }

        private static void ConnectorOnMessageTransmitted(string rawMessage)
        {
            Console.WriteLine("<<<    " + rawMessage);
        }

        private static void ConnectorMessageReceived(string rawMessage, IrcMessage message)
        {
            Console.WriteLine(">>>    " + rawMessage);
        }
    }
}
