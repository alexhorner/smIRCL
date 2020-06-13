using System;
using System.Threading;
using Newtonsoft.Json;
using smIRCL;
using smIRCL.Config;

namespace smIRCLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IrcClient client = new IrcClient(new IrcClientConfig
            {
                ServerHostname = "irc.test.com",
                ServerPort = 6697,
                Nick = "MyNick",
                UserName = "MyUserName",
                RealName = "Me",
                UseSsl = true
            });

            Thread.Sleep(-1);
        }
    }
}
