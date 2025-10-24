
using Amqp;
using Amqp.Sasl;
using Amqp.Framing;

class Program
{
    private static string DEFAULT_BROKER_URI = "amqp://localhost:5672";
    private static string DEFAULT_CONTAINER_ID = "sender-1";
    private static string DEFAULT_TOPIC_NAME = "hello-messages";

    static void Main(string[] args)
    {
        Connection connection = new Connection(new Address(DEFAULT_BROKER_URI),
                                       SaslProfile.Anonymous,
                                       new Open() { ContainerId = DEFAULT_CONTAINER_ID }, null);
        Session session = new Session(connection);

        // Publish to fanout exchange - publisher doesn't know about consumers
        Target target = new Target() { Address = "/exchange/amq.fanout" };
        SenderLink sender = new SenderLink(session, "publisher-link", target, null);

        Message message = new Message($"Hello AMQP! Broadcast at {DateTime.Now:HH:mm:ss}");

        // Publish once - fanout exchange handles distribution
        sender.Send(message);

        Console.WriteLine($"[x] Published to exchange: {message.GetBody<string>()}");

        sender.Close();
        session.Close();
        connection.Close();
    }
}
