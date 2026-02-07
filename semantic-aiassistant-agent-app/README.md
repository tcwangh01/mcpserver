# Semantic Kernel OpenAIAssistantAgent 客服系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **OpenAIAssistantAgent** 和 **Plugin** 功能的 .NET 應用程式，實現一個具備訂單查詢與產品資訊查詢功能的 AI 客服機器人。

## 為什麼使用 OpenAIAssistantAgent？

### 問題：ChatCompletionAgent 的限制

在 `ChatCompletionAgent` 中，對話歷史儲存在本地（記憶體中），每次 API 請求需要傳送完整的對話歷史：

```csharp
// ChatCompletionAgent - 對話歷史在本地管理
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "系統提示詞...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

// 本地對話執行緒
ChatHistoryAgentThread thread = new();
```

### 解決方案：OpenAIAssistantAgent

OpenAIAssistantAgent 利用 OpenAI Assistants API，將對話歷史管理交給 OpenAI 伺服器端：

```csharp
// OpenAIAssistantAgent - 對話歷史由 OpenAI 伺服器管理
AssistantClient assistantClient = new(new ApiKeyCredential(apiKey));
Assistant assistant = await assistantClient.CreateAssistantAsync("gpt-4o", ...);
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);

// 伺服器端對話執行緒
OpenAIAssistantAgentThread agentThread = new(assistantClient);
```

## OpenAIAssistantAgent 的核心優勢

| 優勢 | 說明 |
|------|------|
| **伺服器端對話管理** | 對話歷史儲存在 OpenAI 伺服器，不佔本地記憶體 |
| **Thread 自動追蹤** | 透過 Thread ID 自動維護上下文，無需手動管理 |
| **串流支援** | 內建 `InvokeStreamingAsync` 即時回應 |
| **進階功能** | 支援 Code Interpreter、File Search 等 Assistant 專屬功能 |
| **函數呼叫** | 與 Plugin 無縫整合，自動呼叫函數 |
| **持久化對話** | Thread 可跨 Session 重用，實現持久對話 |

## 與 ChatCompletionAgent 的差異

| 項目 | chatcomplete-agent-app | aiassistant-agent-app |
|------|------------------------|------------------------|
| **Agent 類型** | ChatCompletionAgent | OpenAIAssistantAgent |
| **對話歷史儲存** | 本地記憶體（ChatHistory） | OpenAI 伺服器端（Thread） |
| **對話執行緒** | ChatHistoryAgentThread | OpenAIAssistantAgentThread |
| **Kernel 需求** | 需要建立 Kernel 物件 | 不需要 Kernel，直接使用 AssistantClient |
| **Plugin 註冊** | 透過 `kernel.Plugins.AddFromType<T>()` | 透過建構函式傳入 `KernelPlugin` |
| **函數呼叫設定** | 需設定 `FunctionChoiceBehavior.Auto()` | 自動支援，無需額外設定 |
| **進階功能** | 僅支援聊天完成 | 支援 Code Interpreter、File Search |
| **跨 Session** | 需自行序列化對話歷史 | 透過 Thread ID 天然支援 |
| **API 層級** | Chat Completions API | Assistants API |

### 程式碼差異對比

**ChatCompletionAgent 方式：**
```csharp
// 1. 建立 Kernel（需手動配置模型和連接）
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();
kernel.Plugins.AddFromType<CustomerSupportService>();

// 2. 建立 Agent（透過 Kernel）
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "系統提示詞...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

// 3. 本地對話執行緒
ChatHistoryAgentThread thread = new();
```

**OpenAIAssistantAgent 方式：**
```csharp
// 1. 建立 AssistantClient（直接使用 OpenAI SDK）
AssistantClient assistantClient = new(new ApiKeyCredential(apiKey));

// 2. 在 OpenAI 伺服器端建立 Assistant（包含模型和指令）
Assistant assistant = await assistantClient.CreateAssistantAsync(
    "gpt-4o",
    name: "Support Agent",
    instructions: "系統提示詞...");

// 3. 註冊 Plugin
KernelPlugin plugin = KernelPluginFactory.CreateFromType<CustomerSupportService>();

// 4. 組裝 Agent（Assistant + Client + Plugin）
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);

// 5. 伺服器端對話執行緒
OpenAIAssistantAgentThread agentThread = new(assistantClient);
```

## 功能特性

- **OpenAIAssistantAgent**：使用 OpenAI Assistants API 的 Agent 封裝
- **Plugin 自動函數調用**：AI 自動判斷何時需要調用客服函數
- **OpenAIAssistantAgentThread**：伺服器端對話歷史管理
- **串流回應**：即時顯示 AI 回應（打字機效果）
- **函數追蹤**：顯示 AI 呼叫的函數名稱（Trace）
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
│                   OpenAIAssistantAgent                       │
│  ┌─────────────────┐  ┌─────────────────────────────────┐  │
│  │    Assistant     │  │    KernelPlugin                 │  │
│  │  (模型 + 指令)   │  │  (CustomerSupportService)      │  │
│  └─────────────────┘  └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                      AssistantClient                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         OpenAIAssistantAgentThread                   │   │
│  │   (伺服器端對話歷史 — 透過 Thread ID 追蹤)           │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│              OpenAI Assistants API (GPT-4o)                 │
│  ┌────────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │  Chat / 推論    │  │ Thread 管理   │  │ Function Call │  │
│  └────────────────┘  └──────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-aiassistant-agent-app
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Agents.OpenAI --version 1.70.0-preview
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
# trace assistant - *: FUNCTION CALL - CustomerSupportService-GetOrderStatus
assistant - Support Agent > 好的！請問您的訂單編號是什麼呢？

# trace chat thread with agent: Support Agent - ,threadId: thread_abc123

您: 12345
# trace assistant - *: FUNCTION CALL - CustomerSupportService-GetOrderStatus
assistant - Support Agent > 您查詢的訂單 12345 的狀態為：已出貨。請問還有其他需要幫忙的嗎？

# trace chat thread with agent: Support Agent - ,threadId: thread_abc123

您: iPhone 的價格是多少？
# trace assistant - *: FUNCTION CALL - CustomerSupportService-GetProductInfo
assistant - Support Agent > iPhone 是我們長年熱銷商品，價格為 NT $599。

# trace chat thread with agent: Support Agent - ,threadId: thread_abc123

您: 我剛剛問了哪些問題？
assistant - Support Agent > 您剛剛問了以下問題：
1. 查詢訂單（訂單編號 12345 的狀態）
2. iPhone 的價格

請問還有什麼需要幫忙的嗎？
```

## 核心程式碼說明

### 1. 建立 AssistantClient 與 Assistant

```csharp
// 建立 OpenAI Assistants API 客戶端
AssistantClient assistantClient = new AssistantClient(new ApiKeyCredential(apiKey));

// 在 OpenAI 伺服器端建立 Assistant（定義模型、名稱、系統指令）
Assistant assistant = await assistantClient.CreateAssistantAsync(
    "gpt-4o",
    name: "Support Agent",
    instructions: @"你是一位專業且有禮貌的 AI 客服專員...");
```

### 2. 註冊 Plugin 並組裝 Agent

```csharp
// 掃描 CustomerSupportService 類別，將 [KernelFunction] 方法註冊為工具
KernelPlugin plugin = KernelPluginFactory.CreateFromType<CustomerSupportService>();

// 組裝 Agent：Assistant + Client + Plugin
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);
```

### 3. 使用 AgentThread 管理對話

```csharp
// 建立伺服器端對話執行緒
OpenAIAssistantAgentThread agentThread = new(assistantClient);

// 串流呼叫 Agent
ChatMessageContent message = new(AuthorRole.User, input);
await foreach (StreamingChatMessageContent response in
    agent.InvokeStreamingAsync(message, agentThread))
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
| Microsoft.SemanticKernel | 1.70.0 | 核心框架（KernelPlugin 等） |
| Microsoft.SemanticKernel.Agents.Abstractions | 1.70.0-preview | Agent 抽象層 |
| Microsoft.SemanticKernel.Agents.Core | 1.70.0-preview | Agent 核心框架 |
| Microsoft.SemanticKernel.Agents.OpenAI | 1.70.0-preview | OpenAI Assistant Agent 支援 |
| Microsoft.SemanticKernel.Connectors.OpenAI | 1.70.0 | OpenAI 連接器 |

## 常見問與答

### Q: OpenAIAssistantAgent 和 ChatCompletionAgent 該怎麼選？

**A: 取決於你的需求場景。**

| 需求 | 推薦方案 |
|------|---------|
| 簡單問答、不需要持久化 | ChatCompletionAgent |
| 需要跨 Session 記住對話 | OpenAIAssistantAgent |
| 需要 Code Interpreter | OpenAIAssistantAgent |
| 需要 File Search | OpenAIAssistantAgent |
| 需要自行管理對話歷史 | ChatCompletionAgent |
| 需要最低延遲 | ChatCompletionAgent |
| 離線或私有部署 | ChatCompletionAgent |

### Q: 每次程式執行都會建立新的 Assistant 嗎？

**A: 是的，目前範例程式每次執行都會在 OpenAI 伺服器建立一個新的 Assistant。** 正式環境應儲存 Assistant ID，並在後續執行中重用：

```csharp
// 重用已建立的 Assistant（正式環境建議做法）
var existingAssistant = await assistantClient.GetAssistantAsync("asst_abc123");
var agent = new OpenAIAssistantAgent(existingAssistant, assistantClient, [plugin]);
```

### Q: Thread 可以跨 Session 重用嗎？

**A: 可以！** 這是 OpenAIAssistantAgent 的一大優勢。只要儲存 Thread ID，就能在不同 Session 中繼續對話：

```csharp
// 儲存 Thread ID
var threadId = agentThread.Id;

// 在新的 Session 中恢復對話
OpenAIAssistantAgentThread resumedThread = new(assistantClient, threadId);
```

### Q: 解析使用者問題的是 OpenAIAssistantAgent 還是 Semantic Kernel？

**A: 都不是！真正解析問題的是 OpenAI GPT-4o 模型。**

```
┌─────────────────────────────────────────────────────────────────┐
│                     您的輸入: "我想查訂單"                        │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  OpenAIAssistantAgent                                           │
│  職責：                                                          │
│  • 組裝 Assistant + Client + Plugin                              │
│  • 管理對話執行緒 (OpenAIAssistantAgentThread)                   │
│  • 提供統一的呼叫介面 (InvokeStreamingAsync)                     │
│  • ❌ 不解析問題                                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  OpenAI Assistants API + GPT-4o    ← ✅ 真正解析問題的地方       │
│  職責：                                                          │
│  • 管理 Thread（伺服器端對話歷史）                                │
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
| 1 | OpenAIAssistantAgent | 將訊息加入 Thread |
| 2 | AssistantClient | 透過 Assistants API 發送請求 |
| 3 | **GPT-4o** | **解析問題，判斷意圖是「查詢訂單」** |
| 4 | **GPT-4o** | **決定呼叫 `GetOrderStatus("12345")`** |
| 5 | Semantic Kernel | 在本地執行 `CustomerSupportService.GetOrderStatus("12345")` |
| 6 | AssistantClient | 將函數結果回傳給 Assistants API |
| 7 | **GPT-4o** | **根據結果生成友善的回應文字** |
| 8 | OpenAIAssistantAgent | 串流回傳結果給使用者 |

---

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **Assistant 管理**：每次執行會建立新 Assistant，正式環境應重用已建立的 Assistant
3. **成本考量**：GPT-4o 的 API 調用有費用，Assistants API 的 Thread 儲存也可能產生費用
4. **Preview 套件**：`Microsoft.SemanticKernel.Agents.OpenAI` 目前為預覽版本，API 未來可能變更
5. **OPENAI001 警告**：Assistants API 標記為實驗性，需以 `#pragma warning disable OPENAI001` 抑制

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent 架構
5. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent 架構（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [OpenAI Assistants API 文檔](https://platform.openai.com/docs/assistants/overview)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
