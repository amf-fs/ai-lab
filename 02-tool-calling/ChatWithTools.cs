#:package Microsoft.Extensions.AI@10.11.0
#:package OllamaSharp@5.5.2

using System.ComponentModel;
using Microsoft.Extensions.AI;
using OllamaSharp;

const string exitWord = "enough";
const string model = "llama3.2:latest";
int toolCalls = 0;

IChatClient chatClient = 
    new OllamaApiClient("http://localhost:11434", model);

chatClient = chatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

List<ChatMessage> history = [];


var options = new ChatOptions()
{
    Tools = [
        AIFunctionFactory.Create(GetPassword),
        AIFunctionFactory.Create(GetLatestNews),
        AIFunctionFactory.Create(GetAssetPrice),
        AIFunctionFactory.Create(GetTheWeather)],
    MaxOutputTokens = 100,
    Temperature = 0
};


Console.WriteLine($"THE CHAT HAS STARTED, type {exitWord} to exit. \n");

while(true)
{
    Console.WriteLine($"Model {model}: ");
    var userMessage = Console.ReadLine() ?? "";

    if(userMessage.Equals(exitWord, StringComparison.InvariantCultureIgnoreCase))
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

Console.WriteLine($"Tool calls: {toolCalls}!!");

[Description("Get user passwords by username returning random pwd for now")]
string GetPassword(string username)
{
    toolCalls++;
    Console.WriteLine($"Querying password for {username}");
    return Random.Shared.GetString("ewuiqruwqhi", 10);
}

[Description("Get the latest bombastic news")]
string GetLatestNews()
{
    toolCalls++;
    Console.WriteLine("Querying news");
    return "The world has collapsed!!";
}

[Description("Get the most recent asset price")]
string GetAssetPrice(
    [Description("asset name can be Dollar, Microsoft stock, Real, gold, any commodity")]
    string assetName, 
    [Description("This is period can be daily price monthly or by year only")]
    TimeFrame frame)
{
    toolCalls++;
    Console.WriteLine($"Querying asset: {assetName} {frame}");
    return "123456";
}


[Description("Get the current demo weather. This is fixed sample data, not a live forecast.")]
string GetTheWeather()
{
    toolCalls++;
    const string weather = "The demo weather is 12 degrees C and rainy. An umbrella is recommended.";
    Console.WriteLine($"GetTheWeather function invoked: {weather}");
    return weather;
}

enum TimeFrame
{
    Day,
    Month,
    Year
}