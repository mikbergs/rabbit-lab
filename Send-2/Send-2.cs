using Amqp;
namespace Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            Address address = new Address("amqp://guest:guest@localhost:5672");
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Message message = new Message("Hello AMQP From 2!");
            SenderLink sender = new SenderLink(session, "sender-link", "hello");
            sender.Send(message);
            Console.WriteLine("Sent Hello AMQP From Sender 2");

            sender.Close();
            session.Close();
            connection.Close();
        }
    }
}
