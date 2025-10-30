
using Amqp;
using Amqp.Sasl;
using Amqp.Framing;

class Program
{
    private static string DEFAULT_BROKER_URI = "amqp://localhost:5672";
    private static string DEFAULT_CONTAINER_ID = "sender-1";

    static void Main(string[] args)
    {
        // Parse command-line arguments for message
        string? messageText = ParseMessageFromArgs(args);

        // If no message provided via args, prompt user
        if (string.IsNullOrEmpty(messageText))
        {
            Console.Write("Enter message to send: ");
            messageText = Console.ReadLine() ?? string.Empty;
        }

        // If still no message, use default
        if (string.IsNullOrEmpty(messageText))
        {
            messageText = "Hello AMQP!";
        }

        Connection connection = new Connection(new Address(DEFAULT_BROKER_URI),
                                       SaslProfile.Anonymous,
                                       new Open() { ContainerId = DEFAULT_CONTAINER_ID }, null);
        Session session = new Session(connection);

        Target target = new Target() { Address = "/exchange/broadcast" };
        SenderLink sender = new SenderLink(session, "tx-link", target, null);

        Message message = new Message($"{messageText} - Broadcast at {DateTime.Now:HH:mm:ss}");
        message.Properties = new Properties() { Subject = "account" }; // Set the subject property

        sender.Send(message);
        Console.WriteLine($"[x] Published to exchange: {message.GetBody<string>()}");

        sender.Close();
        session.Close();
        connection.Close();
    }

    private static string? ParseMessageFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-m" || args[i] == "--message")
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
