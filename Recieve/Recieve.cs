using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Amqp.Sasl;
using System.Text;
using System.Text.Json;
class Program
{
    private static string DEFAULT_BROKER_URI = "amqp://localhost:5672";
    private static string DEFAULT_CONTAINER_ID = "client-1";
    private static string DEFAULT_SUBSCRIPTION_NAME = "test-subscription";
    private static string DEFAULT_TOPIC_NAME = "hello-messages";

    static async Task Main(string[] args)
    {
        /*
        var address = new Address("amqp://guest:guest@localhost:5672");
        var connection = new Connection(address);
        var session = new Session(connection);
        // var source = new Source() { Address = "/queue/#", Durable = 0 };
        var source = CreateBasicSource();
        var receiver = new ReceiverLink(session, "receiver-link", source, null);
        */
        Connection connection = new Connection(new Address(DEFAULT_BROKER_URI),
                                       SaslProfile.Anonymous,
                                       new Open() { ContainerId = DEFAULT_CONTAINER_ID }, null);
        Session session = new Session(connection);
        Source source = CreateBasicSource();

        // Create temporary queue with unique name for this consumer
        var tempQueueName = $"temp-{Guid.NewGuid().ToString("N")[..8]}";
        source.Address = tempQueueName;
        source.ExpiryPolicy = new Symbol("link-detach");
        source.Durable = 0;
        source.DistributionMode = new Symbol("copy");

        ReceiverLink receiver = new ReceiverLink(session, DEFAULT_SUBSCRIPTION_NAME, source, null);

        // Auto-bind this temp queue to the broadcast exchange
        await BindQueueToExchange(tempQueueName, "amq.fanout");

        Console.WriteLine($" [*] Receiver 1 (queue: {tempQueueName}) waiting for messages. To exit press CTRL+C");

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
                        Console.WriteLine($" [x] Receiver 1 got: {messageBody}");
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
    // private static void CreateDurableSubscription()
    // {
    //     Connection connection = new Connection(new Address(DEFAULT_BROKER_URI),
    //                                            SaslProfile.Anonymous,
    //                                            new Open() { ContainerId = DEFAULT_CONTAINER_ID }, null);

    //     try
    //     {
    //         Session session = new Session(connection);

    //         Source source = CreateBasicSource();

    //         // Create a Durable Consumer Source.
    //         source.Address = DEFAULT_TOPIC_NAME;
    //         source.ExpiryPolicy = new Symbol("never");
    //         source.Durable = 2;
    //         source.DistributionMode = new Symbol("copy");

    //         ReceiverLink receiver = new ReceiverLink(session, DEFAULT_SUBSCRIPTION_NAME, source, null);

    //         session.Close();
    //     }
    //     finally
    //     {
    //         connection.Close();
    //     }
    // }
    private static Source CreateBasicSource()
    {
        Source source = new Source();

        // These are the outcomes this link will accept.
        Symbol[] outcomes = new Symbol[] {new Symbol("amqp:accepted:list"),
                                          new Symbol("amqp:rejected:list"),
                                          new Symbol("amqp:released:list"),
                                          new Symbol("amqp:modified:list") };

        // Default Outcome for deliveries not settled on this link
        Modified defaultOutcome = new Modified();
        defaultOutcome.DeliveryFailed = true;
        defaultOutcome.UndeliverableHere = false;

        // Configure Source.
        source.DefaultOutcome = defaultOutcome;
        source.Outcomes = outcomes;

        return source;
    }

    private static async Task BindQueueToExchange(string queueName, string exchangeName)
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

        // Bind queue to exchange
        var bindContent = new StringContent("{}", Encoding.UTF8, "application/json");
        await httpClient.PostAsync($"http://localhost:15672/api/bindings/%2f/e/{exchangeName}/q/{queueName}", bindContent);
    }
}

