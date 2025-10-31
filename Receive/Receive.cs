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
        var address = new Address("amqp://guest:guest@localhost:5672");
        var queueName = $"broadcast_sub_{Guid.NewGuid()}";

        var connection = new Connection(address);
        var session = new Session(connection);

        var source = new Source
        {
            Address = "/exchange/broadcast/account",
            ExpiryPolicy = new Symbol("link-detach"),
            Durable = 0,
            DistributionMode = new Symbol("copy"),
            FilterSet = new Map
            {
                {
                    new Symbol("apache.org:legacy-amqp-direct-binding:string"),
                    new DescribedValue(
                        new Symbol("apache.org:legacy-amqp-direct-binding:string"),
                        "account")
                }
            }
        };
        var receiver = new ReceiverLink(session, "receiver-link-2", source, null);
        Console.WriteLine($" [*] Receiver 2 (queue: {queueName}) waiting for messages. To exit press CTRL+C");

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
