using RabbitMQ.Client;
using System.Text;

class Program
{
    private static string DEFAULT_BROKER_HOST = "localhost";
    private static string DEFAULT_EXCHANGE_NAME = "broadcast";
    private static string DEFAULT_ROUTING_KEY = "mikael";

    static async Task Main(string[] args)
    {
        Console.WriteLine("RabbitMQ.Client (AMQP 0.9.1) Sender");
        Console.WriteLine("===================================");

        // Parse command-line arguments for message and routing key
        string? messageText = ParseMessageFromArgs(args);
        string? routingKey = ParseRoutingKeyFromArgs(args);

        // If no message provided via args, prompt user
        if (string.IsNullOrEmpty(messageText))
        {
            Console.Write("Enter message to send: ");
            messageText = Console.ReadLine() ?? string.Empty;
        }

        // If still no message, use default
        if (string.IsNullOrEmpty(messageText))
        {
            messageText = "Hello AMQP 0.9.1!";
        }

        // Use provided routing key or default
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

        // Prepare the message
        var fullMessage = $"{messageText} - Broadcast at {DateTime.Now:HH:mm:ss}";
        var body = Encoding.UTF8.GetBytes(fullMessage);

        // Publish the message
        await channel.BasicPublishAsync(
            exchange: DEFAULT_EXCHANGE_NAME,
            routingKey: routingKey,
            body: body);

        Console.WriteLine($" [x] Published to exchange '{DEFAULT_EXCHANGE_NAME}' with routing key '{routingKey}': {fullMessage}");
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
