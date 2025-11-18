using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Amqp.Sasl;
using System.Text;
using System.Text.Json;

class Program
{

    static async Task Main(string[] args)
    {
        var url = "amqp://guest:guest@localhost:5672";
        var queueName = "/queue/accountapi";
        var receiverLinkName = "receiver-link-2";

        var source = new Source
        {
            Address = queueName,
            // ExpiryPolicy = new Symbol("link-detach"),
            
            Durable = 2
            // DistributionMode = new Symbol("copy"),
        };

        var open = new Open
        {
            ContainerId = Guid.NewGuid().ToString()
        };
        var connection = await Connection.Factory.CreateAsync(new Address(url), open);
        var session = new Session(connection);
        // var receiver = new ReceiverLink(session, receiverLinkName, queueName);
        var receiver = new ReceiverLink(session, receiverLinkName, source, null);



        // var connection = new Connection(address);
        // var session = new Session(connection);

        // var source = new Source
        // {
        //     Address = "/queues/accountapi", // Broker implementation handles the implicit queue/binding here.
        //     ExpiryPolicy = new Symbol("link-detach"),
        //     Durable = 0,
        //     DistributionMode = new Symbol("copy"),
        // };
        // var receiver = new ReceiverLink(session, "receiver-link-2", source, null);
        // var receiver = new ReceiverLink(session, "receiver-link-2", queueName);
        // Console.WriteLine($"-u:  {address.User}, -p: {address.Password}");
        Console.WriteLine($"Waiting for messages on {queueName}");
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

        // Keep receiving messages until cancelled
        var receiveTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(1));
                    if (message != null)
                    {
                        var messageBody = message.GetBody<string>();
                        Console.WriteLine($" [x] Receiver 2 got: {messageBody}");
                        receiver.Accept(message);
                    }
                }
                catch (TimeoutException)
                {
                    // Timeout is expected, continue listening
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        });

        try
        {
            await receiveTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        Console.WriteLine(" [*] Shutting down receiver...");
        receiver.Close();
        session.Close();
        connection.Close();
        Console.WriteLine(" [*] Exit");
    }
}
