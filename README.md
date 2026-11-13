# ai-lab
ai laboratory studies and more

# How to run example
`dotnet run Chat.cs --property:JsonSerializerIsReflectionEnabledByDefault=true` 
- use the property as true because OllamaClient deserialize the model responses using reflection, file based app this false by default.  