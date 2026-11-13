#:package Microsoft.Extensions.AI@10.11.0
#:package OllamaSharp@5.5.2

using System.Text.Json;
using Microsoft.Extensions.AI;
using OllamaSharp;

var options = new ChatOptions
{
    MaxOutputTokens = 1000,
    Temperature = 2
};

IChatClient chatClient = new OllamaApiClient("http://localhost:11434", "phi4-mini:latest");

List<ChatMessage> history =
[
    new(ChatRole.System, 
    "You are smart and useful assistant for coding'")
];

Console.WriteLine("THE CHAT HAS STARTED, type 'vaza' to exit. \n");

while(true)
{
    Console.WriteLine("Ask me: ");
    var userMessage = Console.ReadLine() ?? "";

    if(userMessage.Equals("vaza", StringComparison.InvariantCultureIgnoreCase))
        break;
    
    history.Add(new ChatMessage(ChatRole.User, userMessage));

    var responseUpdates = new List<ChatResponseUpdate>();
    
    await foreach(var responseUpdate in chatClient.GetStreamingResponseAsync(history, options))
    {
        Console.Write(responseUpdate.Text);
        responseUpdates.Add(responseUpdate);
    }

    history.AddMessages(responseUpdates);
    Console.WriteLine("\n");
}

Console.WriteLine("The chat has finished!!");