#:package Microsoft.Extensions.AI@10.11.0
#:package OllamaSharp@5.5.2

using Microsoft.Extensions.AI;
using OllamaSharp;

using IEmbeddingGenerator<string, Embedding<float>> generator = 
    new OllamaApiClient("http://localhost:11434", "all-minilm:latest");

var prompt = "What's is Alan'2 password?";
var promptEmbedding = await generator.GenerateVectorAsync(prompt);

string[] trainingData = [
    "user: alan pwd: 123456",
    "user: marco pwd: blah",
    "user: sophia pwd: sophi@at",
    "user: alan'2 pwd: haha"
];

var dataWithEmbeddings = await generator.GenerateAndZipAsync(trainingData);

var similarities = dataWithEmbeddings.Select(_ => new
{
    Account = _.Value,
    Rate = CosineSimilarity(promptEmbedding, _.Embedding.Vector)
})
.OrderByDescending(_ => _.Rate)
.ToList();

Console.WriteLine($"Prompt: {prompt}");

similarities.ForEach(s =>
{
    Console.WriteLine($"Account - {s.Account} | Rate - {s.Rate}");
});

static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
{
    var spanA = a.Span;
    var spanB = b.Span;
    
    float dotProduct = 0, normA = 0, normB = 0;
    
    for (int i = 0; i < spanA.Length; i++)
    {
        dotProduct += spanA[i] * spanB[i];
        normA += spanA[i] * spanA[i];
        normB += spanB[i] * spanB[i];
    }
    
    return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
}