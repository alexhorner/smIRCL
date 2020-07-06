# smIRCL Demo Programs
These demo programs show example implementations of the library for making IRC Bots

---

## Demo 1 - Hello Bot
This demo implements a very basic hello call-response, fired when any PRIVMSG received contains both the client's Nick and the word `hello`, `hi` or `hey`. It extends on the **Getting Started** code, so you will see all input and output, and will also be able to send raw messages to the server:

```csharp
static void Main(string[] args)
{
    IrcConnector connector = new IrcConnector(new IrcConfig
    {
        ServerHostname = "irc.myservice.net",
        ServerPort = 6697,
        Nick = "smIRCLDemo1"
        UserName = "smIRCL",
        RealName = "The .NET IRCv3 Library - Demo 1 - Hello Bot",
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

private static void OnPrivMsg(IrcController controller, IrcMessage message)
{
    string messageLower = message.Parameters[1].ToIrcLower();

    if (messageLower.Contains(controller.Nick.ToIrcLower()) && (messageLower.Contains("hello") || messageLower.Contains("hi") || messageLower.Contains("hey")))
    {
        if (controller.IsValidChannelName(message.Parameters[0]))
        {
            controller.SendPrivMsg(message.Parameters[0], $"Hello there {message.SourceNick}!");
        }
        else
        {
            controller.SendPrivMsg(message.SourceNick, "Hello there!");
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

The demo first determines if the message was received from a channel, or directly from a user, then responds to the correct place in the correct manner based on this.