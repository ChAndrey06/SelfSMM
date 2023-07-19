using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

var apiKey = "";

// Create an instance of the OpenAIService class
var gpt3 = new OpenAIService(new OpenAiOptions()
{
    ApiKey = apiKey
});

var completionResult = await gpt3.ChatCompletion.CreateCompletion
(new ChatCompletionCreateRequest
{
    Messages = new List<ChatMessage>(new[]
    {
        new ChatMessage("user", @"
            Language: ENG
            Context: Create a post for Instagram
            Topic: how to use chatgpt

            Query: Create text of the post on given topic, on selected language and return
        ")
        
    }),
    Model = Models.ChatGpt3_5Turbo,
    Temperature = 1F,
    MaxTokens = 2048
});

if (completionResult.Successful)
{
    foreach (var choice in completionResult.Choices)
    {
        Console.WriteLine(choice.Message.Content);
    }
}
else
{
    if (completionResult.Error == null)
    {
        throw new Exception("Unknown Error");
    }

    Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
}

Console.ReadLine();