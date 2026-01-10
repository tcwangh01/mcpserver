#pragma warning disable SKEXP0070 // Google AI connector is experimental

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

// 建立 Kernel Builder
var builder = Kernel.CreateBuilder();

// 從環境變數讀取 OpenAI API Key
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數");
}

// 設定 OpenAI 服務
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4",
    apiKey: apiKey
);

var kernel = builder.Build();

// 取得聊天完成服務
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// 簡單的對話範例
var response = await chatCompletionService.GetChatMessageContentAsync(
    "什麼是 Semantic Kernel？"
);

Console.WriteLine("=== OpenAI 回應 ===");
Console.WriteLine(response.Content);

// 使用 InvokePromptAsync 的範例
Console.WriteLine("\n=== 使用 InvokePromptAsync ===");
var promptResult = await kernel.InvokePromptAsync("義大利推薦美食?");
Console.WriteLine(promptResult);

// ========== 使用 Gemini ==========
Console.WriteLine("\n=== Gemini 回應 ===");

// 從環境變數讀取 Gemini API Key
var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrEmpty(geminiApiKey))
{
    throw new InvalidOperationException("請設定 GEMINI_API_KEY 環境變數");
}

// 建立 Gemini Kernel
var geminiBuilder = Kernel.CreateBuilder();
geminiBuilder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-2.0-flash",
    apiKey: geminiApiKey
);

var geminiKernel = geminiBuilder.Build();

// 取得 Gemini 聊天完成服務
var geminiChatService = geminiKernel.GetRequiredService<IChatCompletionService>();

// 詢問 Gemini
var geminiResponse = await geminiChatService.GetChatMessageContentAsync(
    "你認識台灣嗎"
);

Console.WriteLine(geminiResponse.Content);
