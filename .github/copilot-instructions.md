# Copilot Instructions for RabbitMQ Lab

## Project Overview
This is a RabbitMQ learning project demonstrating message queuing with C# and .NET 8. The solution contains two console applications: a message sender and receiver that communicate through RabbitMQ using AMQP 1.0 protocol.

## Architecture & Components
- **Send/**: Message producer console app (implemented)
- **Recieve/**: Message consumer console app (implemented)
- Both projects target .NET 8 with AmqpNetLite 2.5.1

## Key Patterns & Conventions

### AMQP Connection Pattern
Follow the established pattern using AMQP.Net Lite:
```csharp
var address = new Address("amqp://guest:guest@localhost:5672");
var connection = new Connection(address);
var session = new Session(connection);
```

### Message Sending Pattern
```csharp
var message = new Message("Hello AMQP!");
var sender = new SenderLink(session, "sender-link", "hello");
sender.Send(message);
```

### Message Receiving Pattern
```csharp
var receiver = new ReceiverLink(session, "receiver-link", "hello");
var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(1));
if (message != null) {
    var messageBody = message.GetBody<string>();
    receiver.Accept(message);
}
```

## Development Workflow

### Building & Running
```bash
# Build entire solution
dotnet build rabbit-lab.sln

# Run sender
cd Send && dotnet run

# Run receiver (after implementation)
cd Recieve && dotnet run
```

### Project Structure
- Each console app is a separate .csproj with identical dependencies
- Both reference AmqpNetLite version 2.5.1
- ImplicitUsings and Nullable are enabled for modern C# features
- Both apps use the "hello" queue for consistent messaging

## External Dependencies
- **RabbitMQ Server**: Must be running on localhost:5672 with AMQP 1.0 plugin enabled
- Start with: `docker run -d --hostname my-rabbit --name some-rabbit -p 15672:15672 -p 5672:5672 rabbitmq:3-management`
- Enable AMQP 1.0: `docker exec some-rabbit rabbitmq-plugins enable rabbitmq_amqp1_0`

## Key Implementation Details
- **Continuous Listening**: Receiver runs in a Task.Run loop with timeout handling
- **Cancellation Support**: Uses CancellationTokenSource with Console.CancelKeyPress for graceful shutdown
- **Error Handling**: TimeoutExceptions are expected and handled during message waiting
- **Resource Cleanup**: Proper disposal of receiver, session, and connection on exit

## Testing
Verify message flow by:
1. Starting RabbitMQ server
2. Running receiver first (to establish queue consumer)
3. Running sender to publish messages
4. Confirming message receipt in receiver console output
