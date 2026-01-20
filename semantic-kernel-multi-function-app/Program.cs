using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using test;

// 從環境變數讀取 OpenAI API Key
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數");
}
// 建立 Kernel Builder
var builder = Kernel.CreateBuilder();
// 設定 OpenAI 服務
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4",
    apiKey: apiKey
);
var kernel = builder.Build();

// 註冊 WeatherService Plugin
kernel.Plugins.AddFromType<WeatherService>();

var writerPrompt = @"採用村上春樹風格，為主題 ```{{$title}}``` 使用繁體中文創作一篇短文，全長約 500 字。
        請使用村上春樹的獨特風格，包含隱喻、象徵和富有詩意的語言。
        請注意，這篇短文應該是完整的敘事，並且能夠引起讀者的共鳴。";

var writerFunction = kernel.CreateFunctionFromPrompt(
    writerPrompt, 
    new OpenAIPromptExecutionSettings{
        Temperature = 0.7f,
        MaxTokens = 2000,
    },
    functionName: "WriteMurakamiStyleEssay",
    description: "Write a short essay , with a title provided by the user."
);
// 將 writerFunction 註冊到 kernel
kernel.Plugins.AddFromFunctions("Writer", [writerFunction]);
// 從 kernel 取得聊天服務
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
// 註冊過濾器，自定義的 function invocation filter
kernel.AutoFunctionInvocationFilters.Add(new AuditFilter());

// 建立對話歷史
var chatHistory = new ChatHistory();
// 設定 OpenAI 執行設定，啟用自動函數調用
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
// 持續接收使用者輸入
while (true)
{
    Console.Write("您: ");
    var input = Console.ReadLine();
    // 處理退出命令
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再見！");
        break;
    }
    // 處理清除歷史命令
    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        chatHistory.Clear();
        //chatHistory.AddSystemMessage(
        //    "你是一個友善的助手，可以協助使用者回答問題。" +
        //    "你會記住對話中的內容，並能夠根據之前的對話進行回應。" +
        //    "當使用者詢問天氣時，請使用可用的天氣服務工具來取得資訊。"
        //);
        Console.WriteLine("[系統] 對話歷史已清除\n");
        continue;
    }
    // 處理查看歷史命令
    if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n[對話歷史]");
        Console.WriteLine("-----------------------------------");
        foreach (var message in chatHistory)
        {
            var role = message.Role.ToString();
            var content = message.Content ?? "(函數調用)";
            Console.WriteLine($"[{role}]: {content}");
        }
        Console.WriteLine("-----------------------------------\n");
        continue;
    }
    try
    {
        // 將使用者訊息加入對話歷史
        chatHistory.AddUserMessage(input);

        // 使用 ChatCompletionService 取得回應
        // 這會考慮整個對話歷史，而不只是當前的輸入
        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: settings,
            kernel: kernel
        );

        // 將 AI 回應加入對話歷史
        chatHistory.AddMessage(response.Role, response.Content ?? string.Empty);

        Console.WriteLine($"AI: {response.Content}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"錯誤: {ex.Message}\n");
    }
}


        




// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
