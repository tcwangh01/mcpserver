using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

// 註冊 WeatherService Plugin
kernel.Plugins.AddFromType<WeatherService>();

// 取得聊天完成服務
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// 建立 ChatHistory - 這是本專案的核心功能
// ChatHistory 用於保存對話歷史，讓 AI 能夠記住之前的對話內容
var chatHistory = new ChatHistory();

// 設定系統提示詞，定義 AI 的角色和行為
chatHistory.AddSystemMessage(
    "你是一個友善的助手，可以協助使用者回答問題。" +
    "你會記住對話中的內容，並能夠根據之前的對話進行回應。" +
    "當使用者詢問天氣時，請使用可用的天氣服務工具來取得資訊。"
);

// 設定 OpenAI 執行設定，啟用自動函數調用
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

Console.WriteLine("=== Semantic Kernel Plugin + ChatHistory 示範 ===");
Console.WriteLine("這個應用程式會記住您的對話歷史，讓 AI 能夠理解上下文。");
Console.WriteLine("試試看問天氣，然後再問「那裡明天呢？」看 AI 是否記得您問的城市！");
Console.WriteLine("輸入 'exit' 或 'quit' 離開程式");
Console.WriteLine("輸入 'clear' 清除對話歷史");
Console.WriteLine("輸入 'history' 查看目前的對話歷史");
Console.WriteLine("================================================\n");

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
        chatHistory.AddSystemMessage(
            "你是一個友善的助手，可以協助使用者回答問題。" +
            "你會記住對話中的內容，並能夠根據之前的對話進行回應。" +
            "當使用者詢問天氣時，請使用可用的天氣服務工具來取得資訊。"
        );
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
