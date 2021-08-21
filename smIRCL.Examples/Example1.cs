using System;
using System.Linq;
using smIRCL.Config;
using smIRCL.Core;
using smIRCL.Extensions;

namespace smIRCL.Examples
{
    public static class Example1
    {
        public static void Run()
        {
            IrcConfig config = new IrcConfig
            {
                ServerHostname = "irc.libera.chat",
                UserName = "smIRCL Example",
                RealName = "smIRCL Example 1",
                Nick = $"smIRCL-{new Random().Next(99999).ToString()}"
            };
            IrcConnector connector = new IrcConnector(config);

            connector.MessageReceived += (message, ircMessage) =>
            {
                Console.WriteLine(">>> " + message);
            };
            
            connector.MessageTransmitted += (message) =>
            {
                Console.WriteLine("<<< " + message);
            };
            
            IrcController controller = new IrcController(connector);

            controller.PrivMsg += (ircController, message) =>
            {
                if (message.Parameters[1].ToIrcLower() == "hello")
                {
                    Console.WriteLine($"PRIVMSG>>> [{DateTime.Now}] {message.SourceNick}({message.SourceHostMask}) {controller.Users.FirstOrDefault(u => u.Nick.ToIrcLower() == message.SourceNick.ToIrcLower())?.RealName}: {message.Parameters[1]}");
                    controller.SendPrivMsg(message.Parameters[0], "Hello, " + message.SourceNick);
                }
            };
            
            Console.Write("Connecting... ");
            
            connector.Connect();

            while (!connector.IsConnected || controller.SupportedChannelTypes.Count < 1)
            {
                //wait
            }
            
            Console.WriteLine("Done");
            
            Console.Write("Waiting for ready... ");

            while (!controller.IsReady)
            {
                //wait
            }
            
            Console.WriteLine("Done");
            
            Console.Write("Joining channel... ");
            controller.Join("##smIRCL");
            Console.WriteLine("Done");

            Console.WriteLine("Press enter to disconnect");
            Console.ReadLine();
            
            Console.Write("Disconnecting... ");
            
            controller.Quit();

            while (connector.IsConnected)
            {
                //wait
            }
            
            Console.WriteLine("Done");
        }
    }
}