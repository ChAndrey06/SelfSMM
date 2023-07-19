using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using PL.Telegram.Bot.Models;

namespace PL.Telegram.Bot.Services;

public class ChatGptService
{
    private readonly OpenAIService _openAiService;
    private readonly GptConfig _gptConfig;

    public ChatGptService(IOptions<GptConfig> gptConfigOptions)
    {
        _gptConfig = gptConfigOptions.Value;
        _openAiService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = _gptConfig.ApiKey
        });
    }

    public async Task<string> Request(List<ChatMessage> messages)
    {
        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = messages,
            Model = _gptConfig.Model,
            Temperature = _gptConfig.Temperature,
            MaxTokens = _gptConfig.MaxTokens
        });

        if (completionResult.Successful)
        {
            return string.Concat(completionResult.Choices.Select(choice => choice.Message.Content)).Trim('"');
        }

        if (completionResult.Error == null)
        {
            throw new Exception("Unknown Error");
        }

        return $"{completionResult.Error.Code}: {completionResult.Error.Message}";
    }

    public Task<string> Request(string userMessage)
    {
        return Request(new List<ChatMessage>(new[]
        {
            new ChatMessage("user", userMessage)
        }));
    }
}