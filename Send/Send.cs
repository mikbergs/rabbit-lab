
using Amqp;
using Amqp.Sasl;
using Amqp.Framing;
using Amqp.Types;

class Program
{
    private static string DEFAULT_BROKER_URI = "amqp://localhost:5672";
    private static string DEFAULT_CONTAINER_ID = "sender-1";

    static void Main(string[] args)
    {
        Connection connection = new Connection(new Address(DEFAULT_BROKER_URI),
                                       SaslProfile.Anonymous,
                                       new Open() { ContainerId = DEFAULT_CONTAINER_ID }, null);
        Session session = new Session(connection);
        
        Target target = new Target() { Address = "/exchange/broadcast" };
        SenderLink sender = new SenderLink(session, "tx-link", target, null);

        Message message = new Message($"Hello AMQP! Broadcast at {DateTime.Now:HH:mm:ss}");
        message.Properties = new Properties() { Subject = "account" }; // Set the subject property

        sender.Send(message);

        Console.WriteLine($"[x] Published to exchange: {message.GetBody<string>()}");

        sender.Close();
        session.Close();
        connection.Close();
    }
}
