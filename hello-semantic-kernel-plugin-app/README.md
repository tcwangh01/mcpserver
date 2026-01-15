# Semantic Kernel Plugin 整合範例

這是一個使用 Microsoft Semantic Kernel 框架展示 **Plugin（外掛）功能**的示範專案，透過自定義 Plugin 讓 AI 能夠**自動調用函數**來完成特定任務。

## 與 hello-semantic-kernel-app 的差異

本專案是 [hello-semantic-kernel-app](../hello-semantic-kernel-app) 的**進階延伸**，兩個專案的主要差異如下：

| 特性 | hello-semantic-kernel-app | hello-semantic-kernel-plugin-app |
|------|---------------------------|----------------------------------|
| **主要功能** | 展示基本 AI 對話 | 展示 Plugin 與函數調用 |
| **執行模式** | 單次執行，固定問答 | 互動式聊天介面，持續對話 |
| **API 使用** | `GetChatMessageContentAsync` + `InvokePromptAsync` | `InvokePromptAsync` + **自動函數調用** |
| **Plugin 功能** | 無 | WeatherService 自定義 Plugin |
| **函數調用** | 無 | `FunctionChoiceBehavior.Auto()` |
| **AI 模型** | OpenAI + Google Gemini | OpenAI |
| **應用場景** | 學習基本 AI 對話 | 學習如何擴展 AI 能力 |

### 核心差異說明

#### 1. Plugin 架構
本專案引入 **Plugin（外掛）** 概念，讓您可以為 AI 添加自定義功能：

```csharp
// 註冊自定義 Plugin
kernel.Plugins.AddFromType<WeatherService>();
```

#### 2. 自動函數調用 (Function Calling)
透過 `FunctionChoiceBehavior.Auto()` 設定，AI 會**自動判斷**何時需要調用 Plugin 中的函數：

```csharp
var settings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
```

當用戶詢問「台北今天天氣如何？」時，AI 會：
1. 理解用戶需要天氣資訊
2. 自動調用 `WeatherService.GetWeather("台北")`
3. 將結果整合到回應中

#### 3. 互動式對話
不同於基礎專案的單次執行，本專案提供**持續對話**的互動體驗：

```
您: 台北今天天氣如何？
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 那高雄呢？
AI: 高雄今天晴朗，溫度 31°C，濕度 70%
```

## 功能特性

- 自定義 **WeatherService Plugin** 提供天氣查詢功能
- 使用 **FunctionChoiceBehavior.Auto()** 啟用自動函數調用
- **互動式聊天介面**，支援持續對話
- 展示 **KernelFunction** 屬性的使用方式
- 函數調用時的**日誌追蹤**，方便理解執行流程

## 環境需求

- .NET 10.0 或更高版本
- OpenAI API Key

## 專案建立步驟

### 1. 建立 .NET C# Console 應用程式

開始開發前要先完成 .NET C# 的開發環境．建立方式可參考這篇 [在 MacBook 上建立 .NET/C# 開發環境](https://github.com/tcwangh01/ai-learning-notes/blob/main/llm/semantic-kernel/sk-dotnet-mac-setup.md)。

C# 開發環境建立後，開啟終端機，導航至您想要建立專案的目錄：

```bash
# 建立新的控制台應用程式
dotnet new console -n hello-semantic-kernel-plugin-app

# 進入專案目錄
cd hello-semantic-kernel-plugin-app
```

### 2. 安裝相關套件

```bash
# 安裝 Semantic Kernel 核心套件
dotnet add package Microsoft.SemanticKernel

# 安裝 OpenAI 連接器
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI
```

### 3. 設定環境變數

在 `~/.zshrc` (Mac/Linux) 或系統環境變數中加入：

```bash
export OPENAI_API_KEY="your-openai-api-key-here"
```

重新載入環境變數：

```bash
source ~/.zshrc
```

### 4. 驗證環境變數

```bash
echo $OPENAI_API_KEY
```

## 取得 API Key

### OpenAI API Key
1. 前往 [OpenAI Platform](https://platform.openai.com/api-keys)
2. 登入或註冊帳號
3. 在 API Keys 頁面創建新的 API Key

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 專案結構

```
hello-semantic-kernel-plugin-app/
├── Program.cs                              # 主程式
├── WeatherService.cs                       # 天氣服務 Plugin
├── README.md                               # 專案說明文件
└── hello-semantic-kernel-plugin-app.csproj # 專案設定檔
```

## 程式說明

### WeatherService Plugin

這是一個自定義的 Plugin，提供天氣相關功能：

```csharp
public class WeatherService
{
    [KernelFunction, Description("取得指定城市的天氣資訊")]
    public string GetWeather(
        [Description("城市名稱，例如：台北、台中、高雄")] string city)
    {
        // 模擬天氣資訊
        var weather = city switch
        {
            "台北" => "台北今天晴朗，溫度 25°C，濕度 60%",
            "台中" => "台中今天多雲，溫度 28°C，濕度 55%",
            "高雄" => "高雄今天晴朗，溫度 31°C，濕度 70%",
            _ => $"{city}今天天氣晴朗，溫度 26°C"
        };
        return weather;
    }

    [KernelFunction, Description("取得指定城市未來幾天的天氣預報")]
    public string GetWeatherForecast(
        [Description("城市名稱")] string city,
        [Description("預報天數")] int days = 3)
    {
        // 模擬天氣預報
    }
}
```

### Plugin 關鍵元素

| 元素 | 說明 |
|------|------|
| `[KernelFunction]` | 標記方法為可被 AI 調用的函數 |
| `[Description]` | 提供函數/參數的描述，幫助 AI 理解用途 |
| `FunctionChoiceBehavior.Auto()` | 讓 AI 自動決定何時調用函數 |

### 執行流程

```
1. 用戶輸入：「台北天氣如何？」
          ↓
2. AI 分析用戶意圖
          ↓
3. AI 決定調用 WeatherService.GetWeather("台北")
          ↓
4. [WeatherService.GetWeather] 被調用，參數 city = 台北
          ↓
5. 函數回傳天氣資訊
          ↓
6. AI 整合資訊並回應用戶
```

### 輸出範例

```
=== Semantic Kernel 天氣助手 ===
您可以詢問任何天氣相關的問題
輸入 'exit' 或 'quit' 離開程式

您: 台北今天天氣如何？
[WeatherService.GetWeather] 被調用，參數 city = 台北
[WeatherService.GetWeather] 回傳結果：台北今天晴朗，溫度 25°C，濕度 60%
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 可以給我高雄未來5天的天氣預報嗎？
AI: 高雄未來5天的天氣預報：
- 第1天：晴朗，25-30°C
- 第2天：多雲，23-28°C
- 第3天：陰天，22-26°C

您: exit
再見！
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.68.0 | Semantic Kernel 核心框架 |
| Microsoft.SemanticKernel.Connectors.OpenAI | 1.68.0 | OpenAI 連接器 |

## 進階應用

### 擴展 Plugin 功能

您可以輕鬆擴展 WeatherService 或創建新的 Plugin：

```csharp
// 創建新的 Plugin
public class NewsService
{
    [KernelFunction, Description("取得最新新聞")]
    public string GetLatestNews([Description("新聞類別")] string category)
    {
        // 實作新聞查詢邏輯
    }
}

// 註冊多個 Plugin
kernel.Plugins.AddFromType<WeatherService>();
kernel.Plugins.AddFromType<NewsService>();
```

### 連接真實 API

目前的天氣服務是模擬資料，您可以替換為真實的天氣 API：

```csharp
[KernelFunction, Description("取得指定城市的天氣資訊")]
public async Task<string> GetWeather(string city)
{
    // 呼叫真實的天氣 API
    var response = await httpClient.GetAsync($"https://api.weather.com/{city}");
    // 處理回應...
}
```

## 注意事項

1. **API Key 安全**：絕對不要將 API Keys 硬編碼在程式碼中或提交到版本控制系統
2. **函數描述**：為 Plugin 函數提供清晰的 `[Description]`，這會影響 AI 調用函數的準確性
3. **API 費用**：使用 OpenAI API 會產生費用，請注意使用量
4. **函數調用限制**：某些模型可能不支援函數調用功能

## 疑難排解

### 錯誤：請設定 OPENAI_API_KEY 環境變數
確認環境變數已正確設定：
```bash
echo $OPENAI_API_KEY
```

### AI 沒有調用 Plugin 函數
1. 確認 `FunctionChoiceBehavior.Auto()` 已正確設定
2. 檢查函數的 `[Description]` 是否清晰描述功能
3. 確認使用的模型支援函數調用

### 套件版本問題
如果遇到套件相容性問題：
```bash
dotnet list package
dotnet add package Microsoft.SemanticKernel --prerelease
```

## 學習路徑建議

1. **先學習 hello-semantic-kernel-app**：了解基本的 AI 對話方式
2. **再學習本專案**：了解如何透過 Plugin 擴展 AI 能力
3. **進階應用**：整合更多 Plugin，建立複雜的 AI 工作流

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Semantic Kernel Plugins 指南](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)
