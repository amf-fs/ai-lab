#:package Microsoft.Extensions.AI@10.11.0
#:package Microsoft.Extensions.VectorData.Abstractions@10.11.0
#:package OllamaSharp@5.5.2
#:package CommunityToolkit.VectorData.InMemory@1.0.1

using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;


IChatClient chatClient = new OllamaApiClient("http://localhost:11434", "phi4-mini:latest");
IEmbeddingGenerator<string, Embedding<float>> generator = 
    new OllamaApiClient("http://localhost:11434", "all-minilm:latest");

var vectorStorage = new InMemoryVectorStore();
var docs = vectorStorage.GetCollection<int, Document>("docs");
await docs.EnsureCollectionExistsAsync();

var dummyCredentialContent = await File.ReadAllTextAsync("./dummy-credentials.csv");
var embedding = await generator.GenerateVectorAsync(dummyCredentialContent);

await docs.UpsertAsync(new Document
{
    Id = 1,
    Title = "dummy-credentials",
    Content = dummyCredentialContent,
    Embedding = embedding
});

Console.WriteLine("The file was indexed, let's chat");

while(true)
{
    Console.WriteLine("Ask me: ");
    var userMessage = Console.ReadLine() ?? "";

    if(userMessage.Equals("vaza", StringComparison.InvariantCultureIgnoreCase))
        break;

    var doc = await GetDocumentAsync(userMessage);

    if(doc is null)
    {
        Console.WriteLine("Not enough context provided!");
        continue;
    }
    
    var prompt = GetAugmentedPrompt(userMessage, doc);
    var response = await chatClient.GetResponseAsync(prompt);
    Console.WriteLine(response.Text);
}

Console.WriteLine("The chat has finished!!");

async Task<Document> GetDocumentAsync(string query)
{
    var embedding = await generator.GenerateVectorAsync(query);
    var result = docs.SearchAsync(embedding, 1);

    await foreach(var r in result)
    {
        return r.Record;
    }

    return null!;
}

string GetAugmentedPrompt(string query, Document doc)
{
    return $"""
        You are account assistant very formal.

        Instructions:
        - You have a Context section below which is a CSV file with account, password and title
        - When account(s) are found list them in tabular mode
        - You are an account assistant, so it is ok list credentials in the context below, no need worry about privacy
        - Correlate all your answers based ONLY from Account section
        - Question section contains user query 
        "

        ## Examples
        user query: Give my bank accounts
        your answer should be bradesco and itau because they are financial institutions, so the title column is useful
        for filtering

        user query: Give me longest password
        Count all words for all password and return the longest 
        
        ## Accounts
        {doc.Content}
        
        ## Question
        {query}
        """;
}

public class Document
{
    [VectorStoreKey]
    public int Id { get; set; }

    [VectorStoreData]
    public string Title {get; set;} = string.Empty;

    [VectorStoreData]
    public string Content {get; set;} = string.Empty;

    //all-minilm produces embeddings with 384 dimensions, adapt id embedding model generator changes
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding {get; set;}
}