using Microsoft.SemanticKernel;
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

kernel.Plugins.AddFromType<WeatherService>();

// 設定 OpenAI 執行設定
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

Console.WriteLine("=== Semantic Kernel 天氣助手 ===");
Console.WriteLine("您可以詢問任何天氣相關的問題");
Console.WriteLine("輸入 'exit' 或 'quit' 離開程式\n");

// 持續接收使用者輸入
while (true)
{
    Console.Write("您: ");
    var input = Console.ReadLine();

    // 檢查是否要退出
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再見！");
        break;
    }

    try
    {
        // 呼叫 AI 並取得回應
        var response = await kernel.InvokePromptAsync(input, new(settings));
        Console.WriteLine($"AI: {response}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"錯誤: {ex.Message}\n");
    }
}

