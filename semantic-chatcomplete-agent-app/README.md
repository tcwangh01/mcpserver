# Semantic Kernel ChatCompletionAgent 客服系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **ChatCompletionAgent** 和 **Plugin** 功能的 .NET 應用程式，實現一個具備訂單查詢與產品資訊查詢功能的 AI 客服機器人。

## 為什麼使用 ChatCompletionAgent？

### 問題：傳統方式的複雜性

在傳統的 Semantic Kernel 使用方式中，您需要手動管理：
- ChatHistory 的建立與維護
- 訊息的添加與格式化
- ChatCompletionService 的調用

```csharp
// 傳統方式 - 需要手動管理很多細節
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage("系統提示詞...");
chatHistory.AddUserMessage(input);
var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);
chatHistory.AddMessage(response.Role, response.Content);
```

### 解決方案：ChatCompletionAgent

ChatCompletionAgent 封裝了這些複雜性，提供更高層次的抽象：

```csharp
// 使用 Agent - 簡潔且易於管理
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "系統提示詞...",
    Kernel = kernel
};

// 自動管理對話歷史
await foreach(var response in agent.InvokeStreamingAsync(message, chatHistoryAgentThread))
{
    Console.Write(response.Content);
}
```

## ChatCompletionAgent 的核心優勢

| 優勢 | 說明 |
|------|------|
| **封裝複雜性** | 自動管理 ChatHistory、訊息格式化等細節 |
| **統一介面** | 提供一致的 Agent 呼叫方式 |
| **串流支援** | 內建 `InvokeStreamingAsync` 即時回應 |
| **執行緒管理** | 透過 `ChatHistoryAgentThread` 自動追蹤對話 |
| **函數呼叫** | 與 Plugin 無縫整合，自動呼叫函數 |

## 與 plugin-history-app 的差異

| 項目 | plugin-history-app | chatcomplete-agent-app |
|------|-------------------|------------------------|
| **架構層級** | 低層級 API | 高層級 Agent 封裝 |
| **對話管理** | 手動管理 ChatHistory | 自動透過 AgentThread |
| **系統提示詞** | `chatHistory.AddSystemMessage()` | `agent.Instructions` |
| **API 調用** | `GetChatMessageContentAsync` | `InvokeStreamingAsync` |
| **程式碼複雜度** | 較複雜 | 較簡潔 |
| **擴展性** | 手動處理 | 支援多 Agent 協作 |

### 程式碼差異對比

**傳統方式（plugin-history-app）：**
```csharp
// 建立對話歷史
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage("你是一個友善的助手...");

// 手動管理訊息
chatHistory.AddUserMessage(input);
var response = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory, settings, kernel);
chatHistory.AddMessage(response.Role, response.Content);
```

**Agent 方式（chatcomplete-agent-app）：**
```csharp
// 建立 Agent（包含系統提示詞）
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "你是一個專業的客服...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

// 建立對話執行緒
ChatHistoryAgentThread thread = new();

// 簡潔的呼叫方式
ChatMessageContent message = new(AuthorRole.User, input);
await foreach(var response in agent.InvokeStreamingAsync(message, thread))
{
    Console.Write(response.Content);
}
```

## 功能特性

- **ChatCompletionAgent**：使用 Semantic Kernel Agents 框架
- **Plugin 自動函數調用**：AI 自動判斷何時需要調用客服函數
- **ChatHistoryAgentThread**：自動管理對話歷史
- **串流回應**：即時顯示 AI 回應
- **函數追蹤**：顯示 AI 呼叫的函數名稱
- **客服功能**：
  - 訂單狀態查詢
  - 產品資訊查詢

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                   ChatCompletionAgent                       │
│  ┌─────────────────┐  ┌─────────────────────────────────┐  │
│  │   Instructions  │  │  FunctionChoiceBehavior.Auto()  │  │
│  │   (系統提示詞)   │  │     (自動選擇呼叫函數)           │  │
│  └─────────────────┘  └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                      Semantic Kernel                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              CustomerSupportService Plugin           │   │
│  │  ┌───────────────────┐  ┌───────────────────────┐   │   │
│  │  │  GetOrderStatus   │  │   GetProductInfo      │   │   │
│  │  │  (查詢訂單狀態)    │  │   (查詢產品資訊)       │   │   │
│  │  └───────────────────┘  └───────────────────────┘   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    OpenAI GPT-4o API                        │
└─────────────────────────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-chatcomplete-agent-app
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI --version 1.70.0
```

### 3. 設定環境變數

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

## 執行範例

```
=== Semantic Kernel Plugin + CustomerSupport 查詢訂單示範 ===
這個應用程式會記住您的對話歷史，讓 AI 能夠理解上下文。
這個應用程式可查詢訂單狀態與產品資訊．也可查詢對話歷史
試試看問「我想查訂單？」，問某項「產品資訊？」以及問問「我剛問了哪些問題？」
輸入 'exit' 或 'quit' 離開程式
================================================

您: 我想查詢訂單
# trace Assistant - *: FUNCTION CALL - CustomerSupportService-GetOrderStatus
Assistant - SupportAgent > 好的！請問您的訂單編號是什麼呢？

# trace chat thread with agent: SupportAgent - 一個可以回答訂單狀態與產品資訊的AI客服,threadId: xxx

您: 12345
# trace Assistant - *: FUNCTION CALL - CustomerSupportService-GetOrderStatus
Assistant - SupportAgent > 您查詢的訂單 12345 的狀態為：已出貨。請問還有其他需要幫忙的嗎？

# trace chat thread with agent: SupportAgent - 一個可以回答訂單狀態與產品資訊的AI客服,threadId: xxx

您: iPhone 的價格是多少？
# trace Assistant - *: FUNCTION CALL - CustomerSupportService-GetProductInfo
Assistant - SupportAgent > iPhone 是我們長年熱銷商品，價格為 NT $599。

# trace chat thread with agent: SupportAgent - 一個可以回答訂單狀態與產品資訊的AI客服,threadId: xxx

您: 我剛剛問了哪些問題？
Assistant - SupportAgent > 您剛剛問了以下問題：
1. 查詢訂單（訂單編號 12345 的狀態）
2. iPhone 的價格

請問還有什麼需要幫忙的嗎？
```

## 核心程式碼說明

### 1. 建立 Kernel 與註冊 Plugin

```csharp
// 建立 Kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();

// 註冊客服 Plugin
kernel.Plugins.AddFromType<CustomerSupportService>();
```

### 2. 建立 ChatCompletionAgent

```csharp
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Description = "一個可以回答訂單狀態與產品資訊的AI客服",
    Instructions = @"你是一位專業且有禮貌的 AI 客服專員...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
```

### 3. 使用 AgentThread 管理對話

```csharp
// 建立對話執行緒
ChatHistoryAgentThread chatHistoryAgentThread = new();

// 串流呼叫 Agent
await foreach(StreamingChatMessageContent response in
    agent.InvokeStreamingAsync(message, chatHistoryAgentThread))
{
    Console.Write(response.Content);
}
```

### 4. Plugin 函數定義

```csharp
public class CustomerSupportService
{
    [KernelFunction]
    [Description("Get the status of an order.")]
    public static string GetOrderStatus(
        [Description("Order ID")] string orderId)
    {
        return $"您查詢的訂單 {orderId} 的狀態為 : 已出貨";
    }

    [KernelFunction]
    [Description("Get the product information")]
    public static string GetProductInfo(
        [Description("Product Name")] string productName)
    {
        return $"{productName} 是我們長年熱銷商品，價格為 NT $599";
    }
}
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.70.0 | 核心框架 |
| Microsoft.SemanticKernel.Agents.Core | 1.70.0-preview | Agent 框架 |
| Microsoft.SemanticKernel.Connectors.OpenAI | 1.70.0 | OpenAI 連接器 |

## FunctionChoiceBehavior 選項說明

| 選項 | 說明 |
|------|------|
| `Auto()` | AI 自動決定是否呼叫函數（推薦） |
| `Required()` | 強制 AI 必須呼叫函數 |
| `None()` | 禁止 AI 呼叫函數 |

## 進階用法

### 1. 多 Agent 協作

```csharp
// 建立多個專門的 Agent
ChatCompletionAgent orderAgent = new()
{
    Name = "OrderAgent",
    Instructions = "專門處理訂單相關問題...",
    Kernel = kernel
};

ChatCompletionAgent productAgent = new()
{
    Name = "ProductAgent",
    Instructions = "專門處理產品相關問題...",
    Kernel = kernel
};

// 根據問題類型選擇合適的 Agent
```

### 2. 自訂 AgentThread

```csharp
// 取得對話歷史
var messages = await chatHistoryAgentThread.GetMessagesAsync().ToListAsync();

// 清除對話歷史
await chatHistoryAgentThread.DeleteAsync();
```

### 3. 非串流呼叫

```csharp
// 如果不需要串流，可以使用 InvokeAsync
await foreach(ChatMessageContent response in agent.InvokeAsync(message, thread))
{
    Console.WriteLine(response.Content);
}
```

## 常見問與答

### Q: 解析使用者問題的是 ChatCompletionAgent 還是 Semantic Kernel？

**A: 都不是！真正解析問題的是 OpenAI GPT-4o 模型。**

ChatCompletionAgent 和 Semantic Kernel 都是**協調層**，負責串接和管理，而非理解語意。

#### 元件職責分工

```
┌─────────────────────────────────────────────────────────────────┐
│                     您的輸入: "我想查訂單"                        │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  ChatCompletionAgent                                            │
│  職責：                                                          │
│  • 封裝 Instructions（系統提示詞）                                │
│  • 管理對話執行緒 (ChatHistoryAgentThread)                       │
│  • 提供統一的呼叫介面 (InvokeStreamingAsync)                     │
│  • ❌ 不解析問題                                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  Semantic Kernel                                                │
│  職責：                                                          │
│  • 管理 OpenAI 連接                                              │
│  • 註冊 Plugin（CustomerSupportService）                         │
│  • 當 AI 決定呼叫函數時，執行對應的 Plugin 方法                    │
│  • ❌ 不解析問題                                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  OpenAI GPT-4o 模型    ← ✅ 真正解析問題的地方                    │
│  職責：                                                          │
│  • 理解使用者意圖（自然語言理解 NLU）                              │
│  • 根據 Instructions 決定如何回應                                 │
│  • 判斷是否需要呼叫函數（Function Calling）                       │
│  • 生成回應文字                                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### 實際流程範例

當您輸入「我想查訂單 12345」：

| 步驟 | 元件 | 動作 |
|-----|------|-----|
| 1 | ChatCompletionAgent | 將訊息 + Instructions + 對話歷史打包 |
| 2 | Semantic Kernel | 透過 OpenAI Connector 發送 API 請求 |
| 3 | **GPT-4o** | **解析問題，判斷意圖是「查詢訂單」** |
| 4 | **GPT-4o** | **決定呼叫 `GetOrderStatus("12345")`** |
| 5 | Semantic Kernel | 執行 `CustomerSupportService.GetOrderStatus("12345")` |
| 6 | Semantic Kernel | 將函數結果回傳給 GPT-4o |
| 7 | **GPT-4o** | **根據結果生成友善的回應文字** |
| 8 | ChatCompletionAgent | 串流回傳結果給使用者 |

#### 程式碼對應

```csharp
// ChatCompletionAgent - 只是包裝層
ChatCompletionAgent agent = new()
{
    Instructions = "...",  // 傳給 GPT-4o 的系統提示詞
    Kernel = kernel,       // 連接 Semantic Kernel
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()  // 告訴 GPT-4o 可以自動呼叫函數
    })
};

// 這行會將訊息送到 GPT-4o，由 GPT-4o 解析並回應
await foreach(var response in agent.InvokeStreamingAsync(message, thread))
```

#### 總結

| 元件 | 角色 |
|-----|------|
| ChatCompletionAgent | 協調器、封裝層 |
| Semantic Kernel | 框架、Plugin 管理、API 連接 |
| **OpenAI GPT-4o** | **真正的大腦，解析問題、決策、生成回應** |

---

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **Token 限制**：對話歷史會累積，注意不要超過模型的 Token 限制
3. **成本考量**：GPT-4o 的 API 調用有費用，請注意使用量
4. **Preview 套件**：`Microsoft.SemanticKernel.Agents.Core` 目前為預覽版本

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-chatcomplete-agent-app** - 學習 Agent 架構（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [ChatCompletionAgent 類別文檔](https://learn.microsoft.com/dotnet/api/microsoft.semantickernel.agents.chatcompletionagent)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
