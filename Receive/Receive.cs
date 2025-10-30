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

        await BindQueueToExchange(queueName, "broadcast", "account");
        var connection = new Connection(address);
        var session = new Session(connection);

        var source = new Source
        {
            Address = queueName,
            ExpiryPolicy = new Symbol("link-detach"),
            Durable = 0,
            DistributionMode = new Symbol("copy")
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
    private static async Task BindQueueToExchange(string queueName, string exchangeName, string routingKey = "")
    {
        using var httpClient = new HttpClient();
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

        // Create the queue
        var queueContent = new StringContent(
            JsonSerializer.Serialize(new { durable = false, auto_delete = true }),
            Encoding.UTF8,
            "application/json");

        await httpClient.PutAsync($"http://localhost:15672/api/queues/%2f/{queueName}", queueContent);

        // Bind queue to exchange with routing key
        var bindContent = new StringContent(
            JsonSerializer.Serialize(new { routing_key = routingKey }),
            Encoding.UTF8,
            "application/json");
        await httpClient.PostAsync(
            $"http://localhost:15672/api/bindings/%2f/e/{exchangeName}/q/{queueName}",
            bindContent);
    }
}
