# Semantic Kernel MCP Server Console Client 訂單查詢 AI 助手示範（Streamable HTTP 版）

這是一個展示 **Semantic Kernel** 結合 **Model Context Protocol（MCP）** 的 .NET Console 應用程式，透過 **Streamable HTTP** 連線到 `semantic-mcp-order-shttp-server`，取得訂單查詢工具，並由 AI Agent 根據使用者自然語言對話自動決定是否呼叫工具。

本專案是 `semantic-mcp-stdio-server-console-client` 的 **HTTP 版本**，AI 互動邏輯與 Agent 設定完全相同，差別在於連線 MCP Server 的方式由 stdio 子程序改為 HTTP 連線。

---

## ★ Stdio vs Streamable HTTP：Client 端完整比較

> 這是本專案與 stdio 版本最核心的差異，務必理解兩種模式的機制。

### Client 端程式碼差異

```csharp
// ── stdio 版本（semantic-mcp-stdio-server-console-client）────────────────────
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "../semantic-mcp-order-stdio-server"],
        Name = "OrderServer"
        // ↑ 指定要執行的命令，SDK 自動啟動子程序
        // ↑ 不需要 Server 預先啟動
    }));

// ── Streamable HTTP 版本（本專案）────────────────────────────────────────────
var mcpClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new SseClientTransportOptions
    {
        Endpoint = new Uri("http://localhost:5121/"),    // ← 指定 HTTP URL（非路徑）
        Name = "OrderServer",
        TransportMode = HttpTransportMode.StreamableHttp // ← 明確指定協定
        // ↑ Server 必須事先獨立啟動，否則連線失敗
    }));
```

### Client 端屬性對照表

| 比較項目 | stdio 版本 | Streamable HTTP 版本（本專案） |
|---------|-----------|-------------------------------|
| **Transport 類別** | `StdioClientTransport` | `SseClientTransport` |
| **Options 類別** | `StdioClientTransportOptions` | `SseClientTransportOptions` |
| **連線目標設定** | `Command` + `Arguments`（執行命令） | `Endpoint`（HTTP URL） |
| **Server 啟動** | Client 自動以子程序啟動 Server | **需事先手動啟動 Server** |
| **TransportMode** | 無此設定 | `HttpTransportMode.StreamableHttp` |
| **連線失敗原因** | Server 路徑錯誤、dotnet 未安裝 | Server 未啟動、Port 錯誤、URL 路徑錯誤 |
| **操作步驟** | 只需執行 `dotnet run`（一個動作） | 需先啟動 Server，再執行 Client（兩個動作） |
| **額外套件** | 不需要 | 不需要（`SseClientTransport` 在 `ModelContextProtocol` 內） |

### MCP 連線握手流程（兩版本相同）

無論 stdio 或 Streamable HTTP，`McpClientFactory.CreateAsync()` 完成後的 MCP 協定握手流程完全一致：

```
1. 建立連線（stdio: 啟動子程序 / HTTP: 建立 TCP 連線）
2. Client 發送 initialize 請求（含 Client 資訊與 capabilities）
3. Server 回傳 initialize 回應（含 Server 資訊與支援功能）
4. 握手完成，IMcpClient 可用
          ↓
5. ListToolsAsync() → 取得工具清單（兩版本相同）
6. AsKernelFunction() → 轉換為 Kernel Function（兩版本相同）
7. Agent 自動呼叫工具（兩版本相同）
```

### HttpTransportMode 三種模式說明

```csharp
// 明確使用 MCP 2025-03-26 規範的 Streamable HTTP（本專案設定）
TransportMode = HttpTransportMode.StreamableHttp
// → Client 發送 HTTP POST 請求，Server 以串流方式回應

// 自動偵測（預設值，若不指定 TransportMode 則使用此設定）
TransportMode = HttpTransportMode.AutoDetect
// → 先嘗試 Streamable HTTP，若 Server 不支援則自動退回 SSE
// → 相容性最佳，適合連接不確定版本的 Server

// 明確使用舊版 MCP 2024-11-05 的 SSE 協定
TransportMode = HttpTransportMode.Sse
// → Client 先發 GET /sse 建立長連線，再發 POST /message 送請求
// → 適合連接只支援舊版協定的 Server
```

---

## 為什麼需要 Streamable HTTP Client？

### stdio 模式的限制

```
每次執行 Client，都會啟動一個新的 Server 子程序：
  Client 啟動 → 子程序 Server 啟動 → 對話 → Client 結束 → Server 自動停止
  ↑ 無法連線到已在運行的共享 Server
```

### Streamable HTTP 模式的優勢

```
Server 獨立長期運行（可預先載入大量資料、快取、維持資料庫連線）：
  Server 啟動（一次）
    ↑
  Client A 連線 → 查詢 → 斷線     （資源共享）
  Client B 連線 → 查詢 → 斷線
  Client C 連線 → 查詢 → 斷線
```

## 功能特性

- **MCP Client（Streamable HTTP）**：透過 `SseClientTransport` 連線到獨立運行的 MCP Server
- **自動工具載入**：執行時動態取得 MCP Server 工具清單，自動轉換為 Kernel Function
- **ChatCompletionAgent**：具備訂單查詢指令的 AI 助手，自動判斷何時呼叫工具
- **串流對話**：逐字輸出 AI 回覆，函數呼叫過程即時顯示
- **多輪對話**：完整保存對話歷程，Agent 可理解前後文脈絡
- **工具呼叫追蹤**：顯示 Agent 呼叫的函數名稱及對話 Thread ID

## 系統架構

```
┌──────────────────────────────────────────────────────────────┐
│               使用者介面（Console 互動輸入）                   │
│               User > 請查詢訂單 1001                          │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│     semantic-mcp-shttp-server-console-client（本專案）        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  ChatCompletionAgent（SupportAgent）                 │    │
│  │  Instructions: 專業訂單查詢助手規則                  │    │
│  │  FunctionChoiceBehavior: Auto（自動函數呼叫）        │    │
│  └──────────────────────────────────────────────────────┘    │
│                          ↕                                   │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  Semantic Kernel（GPT-4o）                           │    │
│  │  Plugins: McpTools（從 MCP Server 動態載入）         │    │
│  └──────────────────────────────────────────────────────┘    │
│                          ↕                                   │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  MCP Client（McpClientFactory）                      │    │
│  │  SseClientTransport                                  │    │
│  │  Endpoint: http://localhost:5121/                    │    │
│  │  TransportMode: StreamableHttp                       │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
                  ↕ JSON-RPC over HTTP POST
┌──────────────────────────────────────────────────────────────┐
│   semantic-mcp-order-shttp-server（需事先獨立啟動）           │
│   http://localhost:5121/                                     │
│   GetOrderById / SearchOrdersByCustomer                      │
└──────────────────────────────────────────────────────────────┘
                              ↕
┌──────────────────────────────────────────────────────────────┐
│                     OpenAI GPT-4o API                        │
└──────────────────────────────────────────────────────────────┘
```

## 執行流程

```
步驟 0（必要前置）：在另一個終端機啟動 MCP Server
  cd semantic-mcp-order-shttp-server && dotnet run
        │
        ▼
使用者執行 dotnet run（本專案）
        │
        ▼
┌───────────────────────────────────────────┐
│  建立 Semantic Kernel（連接 GPT-4o）      │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  SseClientTransport 連線至                │
│  http://localhost:5121/                   │
│  執行 MCP 握手（initialize）              │
│  ← stdio 版本：此步驟改為啟動子程序 →    │
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
│  建立 ChatCompletionAgent（SupportAgent） │
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
│  HTTP POST → MCP Server → 查詢工具執行   │
│         ↓                                 │
│  Agent 串流輸出查詢結果                   │
│  # trace threadId: xxx                   │
└───────────────────────────────────────────┘
```

## 重要：需事先手動啟動 MCP Server

**與 stdio 版本最大的操作差異**：本 Client 無法自動啟動 Server，必須先手動啟動。

```bash
# ✅ 正確操作流程（兩個終端機）

# 終端機 1：啟動 MCP Server（保持運行）
cd semantic-mcp-order-shttp-server
dotnet run
# 看到 "Now listening on: http://localhost:5121" 後，Server 準備好了

# 終端機 2：啟動 Console Client
cd semantic-mcp-shttp-server-console-client
dotnet run
```

```bash
# ❌ 常見錯誤：未啟動 Server 直接執行 Client
dotnet run
# 結果：HttpRequestException: Response status code does not indicate success: 404 (Not Found)
# 原因：Server 未啟動，或 Endpoint URL 不正確
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key
- `semantic-mcp-order-shttp-server` 已在 `http://localhost:5121` 運行

## 專案結構

```
semantic-mcp-shttp-server-console-client/
├── Program.cs                                              # 主程式（Kernel、MCP Client、Agent 建立與對話迴圈）
├── semantic-mcp-shttp-server-console-client.csproj        # 專案設定
└── README.md                                              # 本文件

（相依的 MCP Server 專案，需獨立啟動）
../semantic-mcp-order-shttp-server/                        # 需手動啟動，監聽 http://localhost:5121
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-mcp-shttp-server-console-client
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.72.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.72.0
dotnet add package ModelContextProtocol --version 0.2.0-preview.3
```

> stdio 版本與 HTTP 版本所需的套件完全相同。`SseClientTransport` 已包含在 `ModelContextProtocol` 套件中，無需額外安裝 `ModelContextProtocol.AspNetCore`（那是 Server 端才需要的）。

### 3. 抑制實驗性 API 警告

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
# 步驟 1：確認 MCP Server 已啟動（另一個終端機）
# cd semantic-mcp-order-shttp-server && dotnet run

# 步驟 2：編譯與執行本 Client
dotnet build
dotnet run
```

## 執行範例

```
Hello, Agent+MCP (Streamable HTTP) !

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

### 2. 連線 MCP Server（stdio vs HTTP 差異點）

```csharp
// ── stdio 版本（自動啟動子程序）──────────────────────────────────────────────
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "../semantic-mcp-order-stdio-server"],
        Name = "OrderServer"
    }));

// ── Streamable HTTP 版本（連線已運行的 Server）（本專案）─────────────────────
var mcpClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new SseClientTransportOptions
    {
        Endpoint = new Uri("http://localhost:5121/"),  // Server 的根路徑端點
        Name = "OrderServer",
        TransportMode = HttpTransportMode.StreamableHttp
    }));
```

### 3. 動態載入 MCP 工具（兩版本相同）

```csharp
var tools = await mcpClient.ListToolsAsync();
kernel.Plugins.AddFromFunctions("McpTools", tools.Select(t => t.AsKernelFunction()));
```

### 4. 建立 ChatCompletionAgent（兩版本相同）

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

### 5. 多輪串流對話迴圈（兩版本相同）

```csharp
ChatHistoryAgentThread agentThread = new();

while ((userInput = Console.ReadLine()) is not null)
{
    ChatMessageContent message = new(AuthorRole.User, userInput);

    await foreach (StreamingChatMessageContent response in
        agent.InvokeStreamingAsync(message, agentThread))
    {
        // 追蹤工具呼叫
        StreamingFunctionCallUpdateContent? functionCall =
            response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
        if (!string.IsNullOrEmpty(functionCall?.Name))
            Console.WriteLine($"\n# trace FUNCTION CALL - {functionCall.Name}");

        // 串流輸出
        Console.Write(response.Content);
    }
}
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.72.0 | 核心框架，管理 AI 模型與 Plugin |
| Microsoft.SemanticKernel.Agents.Core | 1.72.0 | ChatCompletionAgent、ChatHistoryAgentThread |
| ModelContextProtocol | 0.2.0-preview.3 | MCP Client，含 SseClientTransport（HTTP 連線） |

## 關鍵概念說明

### SseClientTransport — 名為 SSE，支援 Streamable HTTP

`SseClientTransport` 雖然名稱含 "Sse"，但它支援三種模式（由 `TransportMode` 控制）：
- `StreamableHttp`：MCP 2025-03-26 新版協定
- `Sse`：MCP 2024-11-05 舊版協定
- `AutoDetect`（預設）：自動偵測 Server 支援的協定

### Endpoint URL 的正確設定

`app.MapMcp()` 的預設 `pattern` 為空字串，端點掛載於根路徑 `/`，因此 `Endpoint` 應設為：

```csharp
Endpoint = new Uri("http://localhost:5121/")    // ✅ 正確（根路徑）
Endpoint = new Uri("http://localhost:5121/mcp") // ❌ 錯誤（除非 Server 改為 MapMcp("/mcp")）
```

### AsKernelFunction() — 工具自動橋接

MCP SDK 的擴充方法，將 MCP 工具自動轉換為 Semantic Kernel 的 `KernelFunction`，包含工具名稱、說明與參數定義，無需手動撰寫 Plugin 類別。實際呼叫時，仍透過 MCP 協定（HTTP）向 Server 送出請求。

### FunctionChoiceBehavior.Auto()

讓 AI 模型（GPT-4o）自動判斷使用者意圖，決定是否呼叫工具、呼叫哪個工具、傳入什麼參數，對應 OpenAI API 的 `tool_choice: "auto"`。

### ChatHistoryAgentThread

保存完整的對話歷程，讓 Agent 在多輪對話中理解前後文。`agentThread.Id` 是本次對話的唯一識別碼，可用於除錯或日誌追蹤。

## 常見問題

### Q: 執行時出現 "404 Not Found" 怎麼辦？

**A: 最常見的兩個原因：**

1. **MCP Server 尚未啟動**：請先在另一個終端機執行 `dotnet run --project ../semantic-mcp-order-shttp-server`
2. **Endpoint URL 路徑錯誤**：`MapMcp()` 預設掛載於根路徑 `/`，請確認 `Endpoint = new Uri("http://localhost:5121/")` 而非 `/mcp`

### Q: 與 stdio 版本相比，何時應選擇 Streamable HTTP？

| 場景 | 建議版本 |
|------|---------|
| 快速原型開發、本機單一用戶 | **stdio** |
| 多個 Client 共享 Server | **Streamable HTTP** |
| Server 需跨主機部署 | **Streamable HTTP** |
| 需用 curl / Postman 直接測試 MCP | **Streamable HTTP** |
| 不想管理 Server 啟動 | **stdio** |

### Q: 執行時出現「請設定 OPENAI_API_KEY 環境變數」怎麼辦？

**A: 請先設定環境變數再執行：**
```bash
export OPENAI_API_KEY="your-api-key-here"   # macOS/Linux
$env:OPENAI_API_KEY="your-api-key-here"     # Windows PowerShell
```

### Q: SKEXP0001、SKEXP0101、SKEXP0110 警告是什麼？

**A: 這些是 Semantic Kernel 實驗性 API 的警告代碼，已在 `.csproj` 中以 `<NoWarn>` 抑制。** 目前使用的 `ChatCompletionAgent`、`ChatHistoryAgentThread` 等 API 仍在積極開發中，功能可能在未來版本調整。

### Q: # trace 輸出的 threadId 有什麼用途？

**A: 是 `ChatHistoryAgentThread` 的唯一識別碼**，代表當前的對話 Session，可用於除錯、日誌追蹤，或在多 Agent 場景中識別對話來源。

### Q: 如何讓 Agent 查詢不同的業務資料？

**A: 修改 `semantic-mcp-order-shttp-server` 的工具實作即可，Client 端無需任何修改**，Agent 會自動探索並使用更新後的工具。

## 注意事項

1. **API Key 安全**：請勿將 API Key 硬編碼在程式碼中，務必使用環境變數
2. **啟動順序**：必須先啟動 Server，再執行 Client
3. **API 費用**：GPT-4o 的 API 呼叫有費用，每次對話及工具呼叫均會計費
4. **Preview 套件**：`ModelContextProtocol` 及部分 Semantic Kernel Agents API 目前為預覽版本，API 可能變更

## 學習路徑建議

1. **hello-semantic-kernel-app** — 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** — 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** — 學習 ChatHistory
4. **semantic-kernel-multi-function-app** — 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** — 學習 ChatCompletionAgent
6. **semantic-mcp-order-stdio-server** — 學習 MCP Server（stdio）
7. **semantic-mcp-stdio-server-console-client** — 學習 MCP Client（stdio）
8. **semantic-mcp-order-shttp-server** — 學習 MCP Server（Streamable HTTP）
9. **semantic-mcp-shttp-server-console-client** ← **本專案** — 學習 MCP Client（Streamable HTTP）

## 參考資源

- [Model Context Protocol 官方文檔](https://modelcontextprotocol.io/)
- [MCP Streamable HTTP 規範（2025-03-26）](https://modelcontextprotocol.io/specification/2025-03-26/basic/transports#streamable-http)
- [ModelContextProtocol .NET SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
