#:package Microsoft.Extensions.AI@10.11.0
#:package OllamaSharp@5.5.2
#:package Microsoft.Extensions.Caching.Memory@10.0.0
#:package Microsoft.Extensions.DependencyInjection@10.0.0
#:package Microsoft.Extensions.Hosting@10.0.0
#:package Microsoft.Extensions.Logging@10.0.0
#:package Microsoft.Extensions.Options@10.0.0
#:package OpenTelemetry@1.12.0
#:package OpenTelemetry.Extensions.Hosting@1.12.0
#:package OpenTelemetry.Exporter.Console@1.12.0

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OllamaSharp;
using Microsoft.Extensions.Caching.Distributed;


var builder = Host.CreateApplicationBuilder();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(_ => _.AddConsole());

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("ProdChat")
            .AddConsoleExporter();
    });

builder.Services.AddChatClient(services =>
{
    var cache = services.GetRequiredService<IDistributedCache>();
    var log = services.GetRequiredService<ILoggerFactory>();

    var builder = ((IChatClient)new OllamaApiClient("http://localhost:11434", "llama3.2:latest"))
        .AsBuilder();

    return builder
        .ConfigureOptions(_ => _.Temperature = 0.5f)
        .UseDistributedCache(cache)
        .UseFunctionInvocation()
        .UseLogging(log)
        .UseOpenTelemetry(sourceName: "ProdChat")
        .Use(innerClient => new RetryChatClient(innerClient))
        .Build(services);
});

var host = builder.Build();
await host.StartAsync();

try
{
    var chatClient = host.Services.GetRequiredService<IChatClient>();

    List<ChatMessage> history =
    [
        new(ChatRole.System,
    "You are smart and useful assistant for coding'")
    ];

    Console.WriteLine("I am a more robust chat equipped with logging, caching and retry. Type 'vaza' to exit. \n");

    while (true)
    {
        Console.WriteLine("Ask me: ");
        var userMessage = Console.ReadLine() ?? "";

        if (userMessage.Equals("vaza", StringComparison.InvariantCultureIgnoreCase))
            break;

        history.Add(new ChatMessage(ChatRole.User, userMessage));

        var modelInstruction = await chatClient.GetResponseAsync(history);
        Console.WriteLine(modelInstruction.Text);

        history.Add(new ChatMessage(ChatRole.Assistant, modelInstruction.Text));

        Console.WriteLine("\n");
    }
}
finally
{
    await host.StopAsync();
}

Console.WriteLine("The chat has finished!!");


public sealed class RetryChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {

        var maxAttempts = 4;
        var attempt = 1;

        while(attempt <= maxAttempts)
        {
            try
            {
                return await base.GetResponseAsync(messages, options, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Console.WriteLine($"Attempt: {attempt}: {ex.Message}");
                await Task.Delay(3000);
            }

            attempt++;            
        }

        throw new InvalidOperationException("Model is unavailable!!");
    }
}