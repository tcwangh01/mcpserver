# Semantic Kernel MCP Server Console Client 訂單查詢 AI 助手示範

這是一個展示 **Semantic Kernel** 結合 **Model Context Protocol（MCP）** 的 .NET Console 應用程式，實現一個能夠查詢真實訂單資料的 AI 客服助手。ChatCompletionAgent 透過 MCP 協定自動呼叫 `semantic-mcp-order-server` 的訂單查詢工具，完成使用者的自然語言查詢需求。

## 為什麼使用 MCP Client + Semantic Kernel Agent？

### 問題：AI Agent 與業務工具的整合障礙

傳統整合 AI Agent 與業務系統的方式，需要手動為每個業務功能撰寫 Kernel Plugin，並緊密耦合業務邏輯與 AI 框架：

```csharp
// 傳統方式 — 需手動撰寫、維護每個 Plugin，與業務系統緊耦合
kernel.Plugins.AddFromObject(new OrderPlugin());  // 需維護 Plugin 類別
// 若業務系統新增工具，Client 端需同步修改並重新部署
```

### 解決方案：MCP 標準化工具整合

透過 MCP 協定，AI Agent 可自動探索並使用 MCP Server 提供的任何工具，無需手動維護 Plugin 定義：

```csharp
// MCP 方式 — 自動探索工具，Client 無需知道工具細節
var tools = await mcpClient.ListToolsAsync();
kernel.Plugins.AddFromFunctions("McpTools", tools.Select(t => t.AsKernelFunction()));
// MCP Server 新增工具時，Client 自動取得，無需修改任何程式碼 ✅
```

## MCP Client + Agent 的核心優勢

| 優勢 | 說明 |
|------|------|
| **自動工具探索** | `ListToolsAsync()` 自動取得 MCP Server 所有工具，無需手動定義 |
| **Server 自動啟動** | `StdioClientTransport` 在執行時自動以子程序啟動 MCP Server，**無需手動預先啟動** |
| **Function Calling** | Agent 根據使用者意圖自動判斷並呼叫對應工具，無需 if/else 分流邏輯 |
| **串流輸出** | `InvokeStreamingAsync()` 逐字輸出 Agent 回覆，提升使用者體驗 |
| **對話記憶** | `ChatHistoryAgentThread` 保存完整對話歷程，Agent 可理解上下文 |
| **函數呼叫追蹤** | 即時顯示 Agent 呼叫了哪個工具，便於除錯與理解 AI 行為 |

## 與其他 Agent 整合模式的差異

| 項目 | Kernel Plugin | Azure AI Agent | MCP Client（本專案） |
|------|--------------|----------------|-------------------|
| **工具定義方式** | 手動撰寫 C# 類別 | 在 Azure Portal 設定 | MCP Server 自動提供 |
| **工具探索** | 靜態（編譯時） | 靜態（設定時） | 動態（`ListToolsAsync()`） |
| **Server 啟動** | 不適用 | 需部署 Azure 服務 | Client 自動啟動子程序 |
| **跨語言支援** | 僅限 .NET | 限 Azure 生態 | 任何支援 MCP 的語言 |
| **部署複雜度** | 低 | 高（需 Azure） | 低（隨 Client 啟動） |
| **適用場景** | 本地函數整合 | 企業雲端場景 | 標準化工具整合 |

## 功能特性

- **MCP Client**：透過 `StdioClientTransport` 自動啟動 MCP Server 並建立連線
- **自動工具載入**：執行時動態取得 MCP Server 工具清單，自動轉換為 Kernel Function
- **ChatCompletionAgent**：具備訂單查詢指令的 AI 助手，自動判斷何時呼叫工具
- **串流對話**：逐字輸出 AI 回覆，函數呼叫過程即時顯示
- **多輪對話**：完整保存對話歷程，Agent 可理解前後文脈絡
- **工具呼叫追蹤**：顯示 Agent 呼叫的函數名稱，及對話 Thread ID

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│               使用者介面（Console 互動輸入）                  │
│               User > 請查詢訂單 1001                         │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│          semantic-mcp-server-console-client（本專案）         │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ChatCompletionAgent（SupportAgent）                  │  │
│  │  Instructions: 專業訂單查詢助手規則                   │  │
│  │  FunctionChoiceBehavior: Auto（自動函數呼叫）         │  │
│  └───────────────────────────────────────────────────────┘  │
│                         ↕                                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Semantic Kernel（GPT-4o）                            │  │
│  │  Plugins: McpTools（從 MCP Server 動態載入）          │  │
│  └───────────────────────────────────────────────────────┘  │
│                         ↕                                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  MCP Client（McpClientFactory）                       │  │
│  │  StdioClientTransport                                 │  │
│  │  → 自動啟動子程序：dotnet run --project              │  │
│  │    ../semantic-mcp-order-server                       │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                    ↕ JSON-RPC over Stdio
┌─────────────────────────────────────────────────────────────┐
│     semantic-mcp-order-server（自動啟動的子程序）            │
│     GetOrderById / SearchOrdersByCustomer                   │
└─────────────────────────────────────────────────────────────┘
                             ↕
┌─────────────────────────────────────────────────────────────┐
│                    OpenAI GPT-4o API                        │
└─────────────────────────────────────────────────────────────┘
```

## 執行流程

```
使用者執行 dotnet run
        │
        ▼
┌───────────────────────────────────────────┐
│  建立 Semantic Kernel（連接 GPT-4o）      │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  StdioClientTransport 自動啟動子程序      │
│  dotnet run --project                     │
│  ../semantic-mcp-order-server             │
│  （無需手動啟動 MCP Server）              │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  ListToolsAsync() 取得工具清單            │
│  顯示：GetOrderById                       │
│        SearchOrdersByCustomer             │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  將工具載入 Kernel Plugins（McpTools）    │
│  建立 ChatCompletionAgent                 │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  進入互動對話迴圈                         │
│  User > 輸入訂單查詢問題                  │
│         ↓                                 │
│  Agent 分析意圖，判斷呼叫哪個 MCP 工具   │
│         ↓                                 │
│  # trace FUNCTION CALL - GetOrderById     │
│         ↓                                 │
│  Agent 串流輸出查詢結果                   │
│  # trace threadId: xxx                   │
└───────────────────────────────────────────┘
```

## 重要：無需手動啟動 MCP Server

執行本專案時，**不需要事先手動啟動 `semantic-mcp-order-server`**。

`StdioClientTransport` 會在 `McpClientFactory.CreateAsync()` 時自動以子程序方式啟動 MCP Server：

```csharp
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "../semantic-mcp-order-server"],
        Name = "OrderServer"
    }));
// ↑ 此行執行後，MCP Server 子程序自動啟動
//   Server 隨本程式啟動，也隨本程式結束而停止
```

直接執行 `dotnet run` 啟動 Console Client 即可，MCP Server 會自動被帶起。

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-mcp-server-console-client/
├── Program.cs                                      # 主程式（Kernel、MCP Client、Agent 建立與對話迴圈）
├── semantic-mcp-server-console-client.csproj       # 專案設定
└── README.md                                       # 本文件

（相依的 MCP Server 專案，位於同層資料夾）
../semantic-mcp-order-server/                       # 由本程式自動啟動
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-mcp-server-console-client
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.70.0
dotnet add package ModelContextProtocol --version 0.2.0-preview.3
```

### 3. 抑制實驗性 API 警告

由於 `ChatCompletionAgent` 等 API 目前標記為實驗性，需在 `.csproj` 中加入：

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);SKEXP0001;SKEXP0101;SKEXP0110</NoWarn>
</PropertyGroup>
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
# 切換到 Console Client 專案資料夾
cd semantic-mcp-server-console-client

# 編譯專案
dotnet build

# 執行專案（MCP Server 會自動隨之啟動）
dotnet run
```

> **提醒**：首次執行時，`dotnet run` 需要編譯 `semantic-mcp-order-server`，可能需要稍等片刻。

## 執行範例

```
Hello, Agent+MCP !

MCP Server 工具清單:
GetOrderById: Query order data by order ID
SearchOrdersByCustomer: Query order list by customer name keyword

User > 請查詢訂單編號 1001 的狀態

# trace assistant - SupportAgent: FUNCTION CALL - McpTools-GetOrderById

assistant - SupportAgent > 您好！訂單 1001 的資訊如下：

- **訂單編號**：1001
- **客戶姓名**：王小美
- **下單日期**：2025 年 6 月 1 日
- **訂單金額**：1,299 元
- **訂單狀態**：已出貨

請問還有其他需要協助的地方嗎？

# trace chat thread with agent: SupportAgent - 一個可以回答訂單資訊的助手,threadId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

User > 幫我查一下陳錢錢有沒有訂單

# trace assistant - SupportAgent: FUNCTION CALL - McpTools-SearchOrdersByCustomer

assistant - SupportAgent > 找到陳錢錢的訂單如下：

- **訂單編號**：1002
- **客戶姓名**：陳錢錢
- **下單日期**：2025 年 6 月 3 日
- **訂單金額**：2,599 元
- **訂單狀態**：處理中

目前訂單正在處理中，請耐心等候。如有其他問題，歡迎繼續詢問！

# trace chat thread with agent: SupportAgent - 一個可以回答訂單資訊的助手,threadId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

User > exit
```

## 核心程式碼說明

### 1. 建立 Semantic Kernel

```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId: "gpt-4o",
        apiKey: apiKey)
    .Build();
```

### 2. 啟動 MCP Client（自動帶起 Server）

`StdioClientTransport` 在建立時自動啟動 MCP Server 子程序，無需手動啟動：

```csharp
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "../semantic-mcp-order-server"],
        Name = "OrderServer"
    }));
```

### 3. 動態載入 MCP 工具

自動取得 MCP Server 提供的工具清單，轉換為 Semantic Kernel 可用的 KernelFunction：

```csharp
var tools = await mcpClient.ListToolsAsync();
kernel.Plugins.AddFromFunctions("McpTools", tools.Select(t => t.AsKernelFunction()));
```

### 4. 建立 ChatCompletionAgent

設定 `FunctionChoiceBehavior.Auto()`，讓 Agent 自動判斷何時呼叫工具：

```csharp
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Description = "一個可以回答訂單資訊的助手",
    Instructions = @"你是一位專業且有禮貌的助手，負責協助顧客查詢訂單資訊...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
```

### 5. 多輪串流對話迴圈

保存對話歷程，支援跨輪次的上下文理解：

```csharp
ChatHistoryAgentThread agentThread = new();

while ((userInput = Console.ReadLine()) is not null)
{
    ChatMessageContent message = new(AuthorRole.User, userInput);

    await foreach (StreamingChatMessageContent response in
        agent.InvokeStreamingAsync(message, agentThread))
    {
        // 追蹤函數呼叫
        StreamingFunctionCallUpdateContent? functionCall =
            response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
        if (!string.IsNullOrEmpty(functionCall?.Name))
            Console.WriteLine($"\n# trace FUNCTION CALL - {functionCall.Name}");

        // 串流輸出 Agent 回覆
        Console.Write(response.Content);
    }
}
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.70.0 | 核心框架，管理 AI 模型與 Plugin |
| Microsoft.SemanticKernel.Agents.Core | 1.70.0 | ChatCompletionAgent、ChatHistoryAgentThread |
| ModelContextProtocol | 0.2.0-preview.3 | MCP Client，連線至 MCP Server |

## 關鍵概念說明

### StdioClientTransport 與子程序啟動

`StdioClientTransport` 透過執行指定的命令（`dotnet run`）啟動 MCP Server 子程序，並使用 Stdin/Stdout 作為通訊通道。Server 的生命週期由 Client 管理，Client 程式結束時 Server 也會隨之停止。

### AsKernelFunction()

MCP SDK 提供的擴充方法，將 MCP 工具自動轉換為 Semantic Kernel 可用的 `KernelFunction`，包含工具名稱、說明與參數定義，無需手動撰寫 Plugin 類別。

### FunctionChoiceBehavior.Auto()

讓 AI 模型（GPT-4o）自動判斷使用者的意圖，決定是否呼叫工具、呼叫哪個工具、以及傳入什麼參數。開發者不需要撰寫任何 if/else 分流邏輯。

### ChatHistoryAgentThread

保存完整的對話歷程，讓 Agent 在多輪對話中能理解前後文。例如使用者說「幫我查上一個訂單的客戶名稱」時，Agent 能從歷史中找到先前查詢的訂單編號。

## 常見問題

### Q: 執行時出現「請設定 OPENAI_API_KEY 環境變數」怎麼辦？

**A: 請先設定環境變數。** 執行前在終端機執行：
```bash
export OPENAI_API_KEY="your-api-key-here"   # macOS/Linux
$env:OPENAI_API_KEY="your-api-key-here"     # Windows PowerShell
```

### Q: 需要先啟動 MCP Server 嗎？

**A: 不需要。** 本程式使用 `StdioClientTransport`，執行 `dotnet run` 後會自動以子程序方式啟動 `semantic-mcp-order-server`，無需手動操作。

### Q: 啟動很慢，需要等很久？

**A: 首次執行需要編譯兩個專案（Console Client + MCP Server），需要較長時間。** 後續執行若程式碼未修改，編譯時間會大幅縮短。可先在 MCP Server 專案資料夾執行 `dotnet build` 預先編譯。

### Q: 如何讓 Agent 查詢我自己的業務資料？

**A: 修改 `semantic-mcp-order-server` 的工具實作。** 將靜態模擬資料替換為真實資料庫查詢，Console Client 端無需任何修改，Agent 會自動使用更新後的工具。

### Q: # trace 輸出的 threadId 有什麼用途？

**A: `threadId` 是 `ChatHistoryAgentThread` 的唯一識別碼，代表當前的對話 Session。** 可用於除錯、日誌追蹤，或在多 Agent 場景中識別是哪個對話 Thread 產生的輸出。

### Q: SKEXP0001、SKEXP0101、SKEXP0110 警告是什麼？

**A: 這些是 Semantic Kernel 實驗性 API 的警告代碼，已在 `.csproj` 中以 `<NoWarn>` 抑制。** 這些 API 仍在積極開發中，功能可能會在未來版本中調整。

## 注意事項

1. **API Key 安全**：請勿將 API Key 硬編碼在程式碼中，務必使用環境變數
2. **API 費用**：GPT-4o 的 API 呼叫有費用，每次對話及工具呼叫均會計費
3. **相對路徑**：`../semantic-mcp-order-server` 是相對於 Console Client 的執行路徑，兩個專案需位於同一層資料夾
4. **Preview 套件**：`ModelContextProtocol` 及部分 Semantic Kernel Agents API 目前為預覽版本

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-kernel-multi-function-app** - 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent
6. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent
7. **semantic-mcp-order-server** - 學習 MCP Server
8. **semantic-mcp-server-console-client** ← **本專案** - 學習 MCP Client + Agent 整合

## 參考資源

- [Model Context Protocol 官方文檔](https://modelcontextprotocol.io/)
- [ModelContextProtocol .NET SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
