#:package Microsoft.Extensions.AI@10.11.0
#:package OllamaSharp@5.5.2

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient chatClient = new OllamaApiClient("http://localhost:11434", "phi4-mini:latest");

List<ChatMessage> history =
[
    new(ChatRole.System, "You are user finder")
];

Console.WriteLine("THE CHAT HAS STARTED, type 'vaza' to exit. \n");

while(true)
{
    Console.WriteLine("Prompt me the users and I'll extract the data: ");
    var userMessage = Console.ReadLine() ?? "";

    if(userMessage.Equals("vaza", StringComparison.InvariantCultureIgnoreCase))
        break;
    
    history.Add(new ChatMessage(ChatRole.User, userMessage));

    var responseUpdates = new List<ChatResponseUpdate>();
    var responseBuilder = new StringBuilder();

    Console.WriteLine("Processing... ");

    await foreach(var responseUpdate in chatClient.GetStreamingResponseAsync(history))
    {
        Console.Write(responseUpdate.Text);
        responseBuilder.Append(responseUpdate.Text);
    }

    var response = await chatClient.GetResponseAsync<List<User>>($"Extract all user information from this text: {responseBuilder}");
    var users = response.Result;
    
    if(users.Count == 0)
        Console.WriteLine("No users found!");

    foreach(var user in users)
    {
        Console.WriteLine("\n");
        Console.WriteLine($"Extracted user: {user.UserName} | {user.Password} | {user.Rate} \n");
    }

    history.AddMessages(responseUpdates);
}

Console.WriteLine("The chat has finished!!");

record User(string UserName, string Password, int Rate);