# Hello Semantic Kernel Plugin + ChatHistory App

這是一個展示 **Semantic Kernel** 結合 **Plugin** 和 **ChatHistory** 功能的 .NET 應用程式，讓 AI 不僅能夠調用外部函數，還能記住對話歷史，實現真正的多輪對話體驗。

## 為什麼需要 ChatHistory？

### 問題：沒有記憶的 AI

在沒有 ChatHistory 的情況下，每次與 AI 對話都是**獨立的**。AI 無法記住之前說過什麼：

```
您: 台北今天天氣如何？
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 那裡明天呢？
AI: 請問您指的是哪個城市？  ← AI 不記得您剛才問的是台北！
```

### 解決方案：ChatHistory 對話歷史

加入 ChatHistory 後，AI 能夠記住整個對話脈絡：

```
您: 台北今天天氣如何？
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 那裡明天呢？
AI: 台北明天的天氣預報是多雲，溫度 23-28°C  ← AI 記得您問的是台北！
```

## ChatHistory 的核心目的

| 目的 | 說明 |
|------|------|
| **保持對話連貫性** | AI 能理解代名詞（如「那裡」、「它」）指的是什麼 |
| **理解上下文** | AI 能根據之前的對話內容做出更準確的回應 |
| **多輪對話能力** | 支援複雜的多步驟對話，如討論、澄清、追問 |
| **個人化體驗** | AI 能記住使用者的偏好和之前提供的資訊 |
| **System Prompt** | 可以設定系統提示詞，定義 AI 的角色和行為 |

## 與 hello-semantic-kernel-plugin-app 的差異

| 項目 | plugin-app | plugin-history-app |
|------|------------|-------------------|
| **對話記憶** | ❌ 無（每次對話獨立） | ✅ 有（記住所有對話） |
| **上下文理解** | ❌ 無法理解代名詞 | ✅ 能理解「那裡」「它」等 |
| **System Prompt** | ❌ 無 | ✅ 有（定義 AI 角色） |
| **API 調用方式** | `InvokePromptAsync` | `GetChatMessageContentAsync` |
| **對話管理** | 無 | 支援清除/查看歷史 |

### 程式碼差異對比

**原本（plugin-app）：**
```csharp
// 每次對話都是獨立的，AI 不記得之前說過什麼
var response = await kernel.InvokePromptAsync(input, new(settings));
```

**現在（plugin-history-app）：**
```csharp
// 建立對話歷史
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage("你是一個友善的助手...");

// 加入使用者訊息到歷史
chatHistory.AddUserMessage(input);

// AI 會考慮整個對話歷史來回應
var response = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,        // 傳入完整對話歷史
    executionSettings: settings,
    kernel: kernel
);

// 將 AI 回應也加入歷史
chatHistory.AddMessage(response.Role, response.Content);
```

## 功能特性

- **Plugin 自動函數調用**：AI 自動判斷何時需要調用天氣服務
- **ChatHistory 對話歷史**：記住整個對話過程
- **System Prompt**：定義 AI 的角色和行為
- **對話管理命令**：
  - `clear`：清除對話歷史，重新開始
  - `history`：查看目前的對話歷史
  - `exit`/`quit`：離開程式

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案建立步驟

### 1. 建立專案目錄

```bash
mkdir hello-semantic-kernel-plugin-history-app
cd hello-semantic-kernel-plugin-history-app
```

### 2. 建立專案

```bash
dotnet new console -n hello-semantic-kernel-plugin-history-app
```

### 3. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.68.0
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI --version 1.68.0
```

### 4. 設定環境變數

**macOS/Linux：**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

**Windows PowerShell：**
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
```

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## ChatHistory 運作原理

```
┌─────────────────────────────────────────────────────────┐
│                    ChatHistory                          │
├─────────────────────────────────────────────────────────┤
│ [System]: 你是一個友善的助手，可以幫助使用者回答問題...       │
│ [User]: 台北今天天氣如何？                              │
│ [Assistant]: 台北今天晴朗，溫度 25°C，濕度 60%          │
│ [User]: 那裡明天呢？  ← AI 可以從歷史得知「那裡」=台北  │
│ [Assistant]: 台北明天多雲，溫度 23-28°C                 │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
              ┌───────────────────────┐
              │   AI 模型 (GPT-4)     │
              │ 分析完整對話歷史       │
              │ 理解上下文和意圖       │
              └───────────────────────┘
```

## 程式碼說明

### ChatHistory 初始化

```csharp
// 建立 ChatHistory 實例
var chatHistory = new ChatHistory();

// 設定 System Prompt - 定義 AI 的角色和行為
chatHistory.AddSystemMessage(
    "你是一個友善的助手，可以協助使用者回答問題。" +
    "你會記住對話中的內容，並能夠根據之前的對話進行回應。"
);
```

### 對話流程

```csharp
// 1. 使用者輸入
var input = Console.ReadLine();

// 2. 將使用者訊息加入歷史
chatHistory.AddUserMessage(input);

// 3. AI 根據完整歷史生成回應
var response = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,
    executionSettings: settings,
    kernel: kernel
);

// 4. 將 AI 回應加入歷史（下次對話會用到）
chatHistory.AddMessage(response.Role, response.Content);
```

## 執行範例

```
=== Semantic Kernel Plugin + ChatHistory 示範 ===
這個應用程式會記住您的對話歷史，讓 AI 能夠理解上下文。
試試看問天氣，然後再問「那裡明天呢？」看 AI 是否記得您問的城市！
輸入 'exit' 或 'quit' 離開程式
輸入 'clear' 清除對話歷史
輸入 'history' 查看目前的對話歷史
================================================

您: 台北今天天氣如何？
[WeatherService.GetWeather] 被調用，參數 city = 台北
[WeatherService.GetWeather] 回傳結果：台北今天晴朗，溫度 25°C，濕度 60%
AI: 台北今天晴朗，溫度 25°C，濕度 60%

您: 那裡明天呢？
[WeatherService.GetWeatherForecast] 被調用，參數 city = 台北, days = 1
AI: 台北明天的天氣預報是晴朗，溫度 25-30°C

您: 高雄呢？
[WeatherService.GetWeather] 被調用，參數 city = 高雄
AI: 高雄今天晴朗，溫度 31°C，濕度 70%

您: history
[對話歷史]
-----------------------------------
[System]: 你是一個友善的助手...
[User]: 台北今天天氣如何？
[Assistant]: 台北今天晴朗，溫度 25°C，濕度 60%
[User]: 那裡明天呢？
[Assistant]: 台北明天的天氣預報是晴朗，溫度 25-30°C
[User]: 高雄呢？
[Assistant]: 高雄今天晴朗，溫度 31°C，濕度 70%
-----------------------------------
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.68.0 | 核心框架 |
| Microsoft.SemanticKernel.Connectors.OpenAI | 1.68.0 | OpenAI 連接器 |

## ChatHistory 的進階用法

### 1. 限制歷史長度

避免 Token 超過限制：

```csharp
// 只保留最近 10 則對話
if (chatHistory.Count > 20)  // System + 10 組對話
{
    // 保留 System Message 和最近的對話
    var systemMessage = chatHistory[0];
    var recentMessages = chatHistory.Skip(chatHistory.Count - 10).ToList();
    chatHistory.Clear();
    chatHistory.Add(systemMessage);
    foreach (var msg in recentMessages)
    {
        chatHistory.Add(msg);
    }
}
```

### 2. 儲存和載入對話

```csharp
// 將對話歷史序列化儲存
var json = JsonSerializer.Serialize(chatHistory);
File.WriteAllText("chat_history.json", json);

// 載入對話歷史
var loaded = JsonSerializer.Deserialize<ChatHistory>(
    File.ReadAllText("chat_history.json")
);
```

### 3. 動態修改 System Prompt

```csharp
// 根據情況調整 AI 行為
chatHistory[0] = new ChatMessageContent(
    AuthorRole.System,
    "新的系統提示詞..."
);
```

## 注意事項

1. **Token 限制**：ChatHistory 會累積，注意不要超過模型的 Token 限制
2. **敏感資訊**：對話歷史可能包含敏感資訊，注意安全性
3. **API Key 安全**：請勿將 API Key 提交到版本控制系統
4. **成本考量**：較長的對話歷史會增加 API 調用成本

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **hello-semantic-kernel-plugin-history-app** - 學習 ChatHistory（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [ChatHistory 類別文檔](https://learn.microsoft.com/dotnet/api/microsoft.semantickernel.chatcompletion.chathistory)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
