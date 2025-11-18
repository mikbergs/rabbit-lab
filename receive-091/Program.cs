using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Program
{
    private static string DEFAULT_BROKER_HOST = "localhost";
    private static string DEFAULT_EXCHANGE_NAME = "broadcast";
    private static string DEFAULT_ROUTING_KEY = "account";

    static async Task Main(string[] args)
    {
        Console.WriteLine("RabbitMQ.Client (AMQP 0.9.1) Consumer");
        Console.WriteLine("=====================================");

        // Parse routing key from arguments
        string? routingKey = ParseRoutingKeyFromArgs(args);
        routingKey ??= DEFAULT_ROUTING_KEY;

        var factory = new ConnectionFactory()
        {
            HostName = DEFAULT_BROKER_HOST,
            UserName = "guest",
            Password = "guest",
            Port = 5672
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Declare the exchange (topic type for routing key support)
        await channel.ExchangeDeclareAsync(
            exchange: DEFAULT_EXCHANGE_NAME,
            type: ExchangeType.Direct,
            durable: false,
            autoDelete: false);

        // Declare a quorum queue with routing key in the name
        var queueName = $"broadcast.{routingKey}";
        var queueDeclareResult = await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum"
            });

        // Bind the queue to the exchange with routing key
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: DEFAULT_EXCHANGE_NAME,
            routingKey: routingKey);

        Console.WriteLine($" [*] Consumer (queue: {queueName}) waiting for messages with routing key '{routingKey}'");
        Console.WriteLine(" [*] To exit press CTRL+C");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Console.WriteLine($" [x] Received message: '{message}' (routing key: {routingKey})");

            // Acknowledge the message
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        // Start consuming messages
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false, // Manual acknowledgment
            consumer: consumer);

        // Setup cancellation handling
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // Keep the application running until cancelled
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        Console.WriteLine(" [*] Shutting down consumer...");
        Console.WriteLine(" [*] Exit");
    }
    private static string? ParseRoutingKeyFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-k" || args[i] == "--routing-key")
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
