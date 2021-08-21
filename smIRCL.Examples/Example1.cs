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

            controller.OnChannelMessage += (ircController, args) =>
            {
                if (args.Content.ToIrcLower() != "hello") return;
                
                Console.WriteLine($"{args.Channel.Name} >>> [{DateTime.Now}] {args.Author.Nick}({args.Author.HostMask}) {args.Author.RealName}: {args.Content}");
                ircController.SendPrivMsg(args.Channel.Name, "Hello, " + args.Author.Nick);
            };
            
            controller.OnPrivateMessage += (ircController, args) =>
            {
                if (args.Content.ToIrcLower() != "hello") return;
                
                Console.WriteLine($"Private >>> [{DateTime.Now}] {args.Author.Nick}({args.Author.HostMask}) {args.Author.RealName}: {args.Content}");
                ircController.SendPrivMsg(args.Author.Nick, "Hello, " + args.Author.Nick);
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