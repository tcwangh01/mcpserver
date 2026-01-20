# Semantic Kernel Multi-Function App

這是一個展示 **Semantic Kernel** 結合**多個 Plugin Functions** 和 **Auto Function Invocation Filter** 的 .NET 應用程式。本專案在 `semantic-kernel-plugin-history-app` 的基礎上，進一步展示如何：

1. 同時註冊多種類型的 Plugin Functions
2. 使用 Filter 監控和攔截函數調用

## 與 semantic-kernel-plugin-history-app 的主要差異

| 項目 | plugin-history-app | multi-function-app (本專案) |
|------|-------------------|----------------------------|
| **Plugin 數量** | 1 個 (WeatherService) | 2 個 (WeatherService + Writer) |
| **Plugin 類型** | 僅 Class-based | Class-based + Prompt-based |
| **函數調用過濾器** | ❌ 無 | ✅ 有 (AuditFilter) |
| **Prompt Function** | ❌ 無 | ✅ 有 (村上春樹風格寫作) |
| **System Prompt** | ✅ 有 | ❌ 無（可自行加入） |

## 核心功能亮點

### 1. 多個 Plugin Functions

本專案展示兩種不同的 Plugin 註冊方式：

#### Class-based Plugin (WeatherService)
```csharp
// 從類別註冊 Plugin
kernel.Plugins.AddFromType<WeatherService>();
```

WeatherService 提供兩個函數：
- `GetWeather(city)` - 取得指定城市的天氣
- `GetWeatherForecast(city, days)` - 取得未來幾天的天氣預報

#### Prompt-based Function (Writer)
```csharp
// 從 Prompt 建立函數
var writerPrompt = @"採用村上春樹風格，為主題 ```{{$title}}``` 使用繁體中文創作一篇短文...";

var writerFunction = kernel.CreateFunctionFromPrompt(
    writerPrompt,
    new OpenAIPromptExecutionSettings{
        Temperature = 0.7f,
        MaxTokens = 2000,
    },
    functionName: "WriteMurakamiStyleEssay",
    description: "Write a short essay, with a title provided by the user."
);

// 註冊到 kernel
kernel.Plugins.AddFromFunctions("Writer", [writerFunction]);
```

### 2. Auto Function Invocation Filter (過濾器)

本專案新增了 `AuditFilter`，用於監控所有自動函數調用：

```csharp
// AuditFilter.cs
namespace test
{
    public sealed class AuditFilter() : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(
            AutoFunctionInvocationContext context,
            Func<AutoFunctionInvocationContext, Task> next)
        {
            // 在函數執行前進行攔截/記錄
            Console.WriteLine($"FILTER INVOKED : {context.Function.Name}");

            // 執行原本的函數
            await next(context);

            // 可在此處理函數執行後的邏輯
        }
    }
}
```

#### 註冊 Filter
```csharp
// 在 Program.cs 中註冊
kernel.AutoFunctionInvocationFilters.Add(new AuditFilter());
```

#### Filter 的用途

| 用途 | 說明 |
|------|------|
| **日誌記錄** | 記錄所有函數調用，便於除錯和審計 |
| **權限控制** | 在執行前檢查使用者權限 |
| **參數驗證** | 驗證傳入參數的合法性 |
| **效能監控** | 測量函數執行時間 |
| **錯誤處理** | 統一處理函數執行異常 |
| **結果修改** | 在函數執行後修改回傳結果 |

## 架構圖

```
┌─────────────────────────────────────────────────────────────┐
│                         Kernel                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐    ┌─────────────────────────────────┐ │
│  │   Plugins       │    │   AutoFunctionInvocationFilters │ │
│  ├─────────────────┤    ├─────────────────────────────────┤ │
│  │ WeatherService  │    │         AuditFilter             │ │
│  │  - GetWeather   │    │  ┌───────────────────────────┐  │ │
│  │  - GetForecast  │    │  │ 1. 記錄函數名稱           │  │ │
│  ├─────────────────┤    │  │ 2. 執行 next(context)     │  │ │
│  │ Writer          │    │  │ 3. (可處理執行後邏輯)     │  │ │
│  │  - WriteMuraka- │    │  └───────────────────────────┘  │ │
│  │    miStyleEssay │    │                                 │ │
│  └─────────────────┘    └─────────────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────┐
              │   AI 模型 (GPT-4)         │
              │   根據使用者輸入          │
              │   自動選擇要調用的函數    │
              └───────────────────────────┘
```

## 執行流程

```
使用者輸入 → AI 判斷需要調用哪個函數
                    │
                    ▼
            ┌───────────────┐
            │  AuditFilter  │ ← 攔截並記錄
            └───────────────┘
                    │
                    ▼
    ┌───────────────────────────────┐
    │ 執行對應的 Plugin Function    │
    │ - WeatherService.GetWeather   │
    │ - WeatherService.GetForecast  │
    │ - Writer.WriteMurakamiStyle   │
    └───────────────────────────────┘
                    │
                    ▼
            AI 整合結果回應使用者
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-kernel-multi-function-app/
├── Program.cs                                 # 主程式
├── WeatherService.cs                          # 天氣服務 Plugin
├── AuditFilter.cs                             # 函數調用過濾器
├── semantic-kernel-multi-function-app.csproj  # 專案設定
└── README.md                                  # 本文件
```

## 快速開始

### 1. 設定環境變數

**macOS/Linux：**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

**Windows PowerShell：**
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
```

### 2. 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 執行範例

```
您: 台北今天天氣如何？
FILTER INVOKED : GetWeather                    ← Filter 攔截記錄
[WeatherService.GetWeather] 被調用，參數 city = 台北
[WeatherService.GetWeather] 回傳結果：台北今天晴朗，溫度 25°C，濕度 60%
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 幫我寫一篇關於「雨天的咖啡館」的短文
FILTER INVOKED : WriteMurakamiStyleEssay       ← Filter 攔截記錄
AI: 窗外的雨聲彷彿一首無盡的旋律，咖啡館裡瀰漫著烘焙豆子的香氣...

您: history
[對話歷史]
-----------------------------------
[User]: 台北今天天氣如何？
[Assistant]: 台北今天晴朗，溫度 25°C，濕度 60%
[User]: 幫我寫一篇關於「雨天的咖啡館」的短文
[Assistant]: 窗外的雨聲彷彿一首無盡的旋律...
-----------------------------------
```

## 對話命令

| 命令 | 說明 |
|------|------|
| `exit` / `quit` | 離開程式 |
| `clear` | 清除對話歷史 |
| `history` | 查看目前的對話歷史 |

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.69.0 | 核心框架 |
| Microsoft.SemanticKernel.Connectors.OpenAI | 1.69.0 | OpenAI 連接器 |

## 進階：自訂 Filter

你可以擴展 AuditFilter 來實現更多功能：

```csharp
public sealed class AdvancedFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        // 1. 執行前：記錄開始時間
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"[{startTime}] 開始執行: {context.Function.Name}");

        // 2. 執行前：檢查權限（範例）
        if (context.Function.Name == "SensitiveFunction")
        {
            // 可以在此拒絕執行
            throw new UnauthorizedAccessException("無權限執行此函數");
        }

        // 3. 執行函數
        await next(context);

        // 4. 執行後：記錄執行時間
        var duration = DateTime.UtcNow - startTime;
        Console.WriteLine($"[執行完成] {context.Function.Name} 耗時: {duration.TotalMilliseconds}ms");

        // 5. 執行後：可以修改結果（透過 context.Result）
    }
}
```

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-kernel-multi-function-app** - 多 Plugin + Filter（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Function Filters 文檔](https://learn.microsoft.com/semantic-kernel/concepts/enterprise-readiness/filters)
- [Creating Functions from Prompts](https://learn.microsoft.com/semantic-kernel/concepts/prompts/your-first-prompt)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
