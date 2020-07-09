# Getting Started
smIRCL is designed to be extremely simple to hook up and get going with minimal configuration and minimal additional code.

At its core, smIRCL handles the current state of your IRC connection without any intervention, and when not hooked into will just hold a connection for you.

Here is an example of holding such a connection open without any useful functionality other than logging all input and output to the console and allowing you to send raw messages:

```csharp
static void Main(string[] args)
{
    IrcConnector connector = new IrcConnector(new IrcConfig
    {
        ServerHostname = "irc.myservice.net",
        ServerPort = 6697,
        Nick = "smIRCL",
        UserName = "smIRCL",
        RealName = "The .NET IRCv3 Library",
        UseSsl = true
    });

    connector.MessageReceived += ConnectorMessageReceived;
    connector.MessageTransmitted += ConnectorOnMessageTransmitted;

    IrcController controller = new IrcController(connector);

    connector.Connect();

    while (!connector.IsDisposed)
    {
        string msg = Console.ReadLine();
        try
        {
            connector.Transmit(msg);
        }
        catch (Exception)
        {
            //ignore
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
```

When this demo is started (you'll need to configure a proper server) you'll be able to see the connection to the server take place, and you'll also see the communication smIRCL does internally in order to properly register a Nick and negotiate IRCv3 capabilities.

You will also be able to issue IRC commands from the console to join and speak in channels (and anything else you can do on an IRC server) making this demo a very basic IRC client.