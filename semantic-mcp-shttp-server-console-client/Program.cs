// ============================================================================
// Program.cs — MCP Console Client (Streamable HTTP) 進入點
// ============================================================================
//
// ┌─────────────────────────────────────────────────────────────────────────┐
// │                         整體架構說明                                    │
// ├─────────────────────────────────────────────────────────────────────────┤
// │  本程式是一個結合 Semantic Kernel Agent 與 MCP Client 的對話應用，        │
// │  透過 Streamable HTTP 連線到 semantic-mcp-order-shttp-server，           │
// │  取得訂單查詢工具，並由 AI Agent 根據使用者對話自動決定是否呼叫工具。      │
// │                                                                         │
// │  請求流程：                                                              │
// │    使用者輸入自然語言                                                     │
// │      → ChatCompletionAgent（GPT-4o）判斷是否需要查詢訂單                  │
// │      → 透過 MCP 協定呼叫 MCP Server 上的工具（HTTP POST）                 │
// │      → 取得工具回傳的 JSON 資料                                           │
// │      → AI 整理後以自然語言回覆使用者                                      │
// └─────────────────────────────────────────────────────────────────────────┘
//
// ============================================================================
// ★ Streamable HTTP vs stdio Client 端的核心差異 ★
// ============================================================================
//
//  ┌─────────────────────┬──────────────────────────────┬───────────────────────────────────┐
//  │ 比較項目             │ stdio 版本                    │ Streamable HTTP 版本（本專案）     │
//  ├─────────────────────┼──────────────────────────────┼───────────────────────────────────┤
//  │ Transport 類別       │ StdioClientTransport          │ SseClientTransport                │
//  │ Options 類別         │ StdioClientTransportOptions   │ SseClientTransportOptions         │
//  │ Server 啟動方式      │ Client 自動以子程序啟動 Server │ 需手動事先啟動 Server（獨立程序）  │
//  │ 連線目標             │ 指定可執行檔路徑（dotnet run） │ 指定 HTTP URL（Endpoint）         │
//  │ 通訊機制             │ Stdin / Stdout（JSON-RPC）    │ HTTP POST + SSE（JSON-RPC）       │
//  │ Server 生命週期      │ 跟隨 Client 子程序            │ 獨立於 Client，可長期運行          │
//  │ 多 Client 支援       │ 每個 Client 各自啟動一個 Server│ 多個 Client 可同時連線同一 Server │
//  └─────────────────────┴──────────────────────────────┴───────────────────────────────────┘
//
// 【使用前置條件】
//   執行本 Client 前，請先在另一個終端機啟動 MCP Server：
//     dotnet run --project ../semantic-mcp-order-shttp-server
//   Server 監聽於 http://localhost:5121/（由 launchSettings.json 設定）
//
// ============================================================================
//
// 【using 引用說明】
//
//   using Microsoft.SemanticKernel;
//     ↑ 提供 Kernel、Kernel.CreateBuilder()、PromptExecutionSettings、
//       FunctionChoiceBehavior 等 Semantic Kernel 核心型別。
//
//   using Microsoft.SemanticKernel.Agents;
//     ↑ 提供 ChatCompletionAgent、ChatHistoryAgentThread 等 Agent 框架型別。
//
//   using Microsoft.SemanticKernel.ChatCompletion;
//     ↑ 提供 ChatMessageContent、AuthorRole、
//       StreamingChatMessageContent、StreamingFunctionCallUpdateContent 等
//       Chat Completion 相關型別，用於建構對話訊息與處理串流回應。
//
//   using ModelContextProtocol.Client;
//     ↑ 提供 McpClientFactory、SseClientTransport、SseClientTransportOptions、
//       HttpTransportMode 等 MCP Client 核心型別。
//       ★ stdio 版本也引用此命名空間，但使用 StdioClientTransport。
//
// ============================================================================

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;

Console.WriteLine("Hello, Agent+MCP (Streamable HTTP) ! \n\n");

// ============================================================================
// 第一部分：環境設定與 API Key 驗證
// ============================================================================
// 從環境變數讀取 OpenAI API Key。
// 這是安全的做法，避免將敏感資訊硬編碼在程式碼中。
//
// 設定方式（macOS / Linux）：
//   export OPENAI_API_KEY="sk-..."
// 設定方式（Windows PowerShell）：
//   $env:OPENAI_API_KEY="sk-..."
// ============================================================================
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數");
}

// ============================================================================
// 第二部分：建立 Semantic Kernel
// ============================================================================
//
// Kernel 是 Semantic Kernel 的核心元件，負責：
//   1. 管理 AI 模型連接（這裡使用 OpenAI GPT-4o）
//   2. 管理 Plugin（MCP 工具會以 Plugin 形式注入）
//   3. 協調 AI 推理與函數呼叫（Function Calling / Tool Use）
//
// AddOpenAIChatCompletion()：
//   向 DI 容器註冊 OpenAI Chat Completion 服務，
//   指定模型 ID 與 API Key，Kernel 會在需要時使用此服務呼叫 GPT。
//
// ============================================================================
var kernel = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(
                        modelId: "gpt-4o",  // 使用 GPT-4o 模型，支援 Function Calling 與串流輸出
                        apiKey: apiKey)
                    .Build();

// ============================================================================
// 第三部分：連線到 MCP Server（Streamable HTTP 模式）
// ============================================================================
//
// McpClientFactory.CreateAsync()
// ─────────────────────────────────────────────────────────────────────────
// 建立 MCP Client 並執行 MCP 初始化握手（initialize / initialized）：
//   1. 建立 HTTP 連線到指定的 Server Endpoint
//   2. 傳送 initialize 請求（含 Client 資訊與 capabilities）
//   3. 接收 Server 回應（含 Server 資訊與支援的功能）
//   4. 完成握手後回傳可用的 IMcpClient 實例
//
// SseClientTransport
// ─────────────────────────────────────────────────────────────────────────
// ★ 與 stdio 版本的關鍵差異點 ★
//
//   stdio 版本使用 StdioClientTransport：
//     new StdioClientTransport(new() {
//         Command = "dotnet",
//         Arguments = ["run", "--project", "../semantic-mcp-order-stdio-server"],
//         Name = "OrderServer"
//     })
//     → 啟動子程序（dotnet run），透過 Stdin/Stdout 通訊
//     → Server 尚未執行時由 Client 自動啟動
//
//   HTTP 版本使用 SseClientTransport（本方法）：
//     → 連線到已獨立運行的 HTTP Server，不啟動任何子程序
//     → 名稱含 "Sse" 但實際上支援 SSE 與 Streamable HTTP 兩種協定
//     → 透過 TransportMode 控制使用哪種協定
//
// SseClientTransportOptions 屬性說明
// ─────────────────────────────────────────────────────────────────────────
//
//   Endpoint（必填）
//     MCP Server 的 HTTP 端點 URL。
//     app.MapMcp() 預設 pattern="" 掛載於根路徑，
//     因此 Endpoint 應設為 http://localhost:5121/（根路徑），
//     而非 http://localhost:5121/mcp。
//
//   Name（選填）
//     傳輸層識別名稱，用於日誌輸出，不影響功能。
//
//   TransportMode（選填，預設 AutoDetect）
//     控制 Client 使用的 HTTP 傳輸協定：
//
//     HttpTransportMode.StreamableHttp（本設定）
//       → 明確使用 MCP 2025-03-26 規範的 Streamable HTTP 協定
//       → Client 發送 HTTP POST 請求，Server 以串流方式回應
//
//     HttpTransportMode.AutoDetect（預設值）
//       → 先嘗試 Streamable HTTP，若 Server 不支援則自動退回 SSE 協定
//       → 相容性最佳，但有額外的偵測開銷
//
//     HttpTransportMode.Sse
//       → 明確使用舊版 MCP 2024-11-05 的 SSE 協定
//       → Client 先發 GET /sse 建立長連線，再發 POST /message 送請求
//
// ─────────────────────────────────────────────────────────────────────────
var mcpClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new SseClientTransportOptions
    {
        // app.MapMcp() 預設 pattern="" 掛載於根路徑，端點為 http://localhost:5121/
        Endpoint = new Uri("http://localhost:5121/"),
        Name = "OrderServer",
        TransportMode = HttpTransportMode.StreamableHttp
    }));

// ============================================================================
// 第四部分：取得 MCP Server 工具清單
// ============================================================================
//
// ListToolsAsync()
//   向 MCP Server 發送 tools/list 請求，
//   取得所有標記了 [McpServerTool] 的方法清單，
//   每個工具包含：Name、Description、InputSchema（參數結構）。
//
// ============================================================================
var tools = await mcpClient.ListToolsAsync();

// debug：顯示從 MCP Server 取得的工具清單，確認連線成功
Console.WriteLine("MCP Server 工具清單:");
foreach (var tool in tools)
{
    Console.WriteLine($"  {tool.Name}: {tool.Description}");
}
Console.WriteLine();

// ============================================================================
// 第五部分：將 MCP 工具注入 Semantic Kernel
// ============================================================================
//
// tools.Select(t => t.AsKernelFunction())
//   將每個 McpClientTool 包裝成 KernelFunction，
//   讓 Semantic Kernel 可以像呼叫本地函數一樣呼叫 MCP 工具。
//   實際呼叫時，Kernel 仍透過 MCP 協定（HTTP）向 Server 送出請求。
//
// kernel.Plugins.AddFromFunctions("McpTools", ...)
//   將所有工具以「McpTools」為 Plugin 名稱注入 Kernel 的 Plugin 集合。
//   Agent 的 FunctionChoiceBehavior.Auto() 會自動從此 Plugin 集合選用工具。
//
// ============================================================================
kernel.Plugins.AddFromFunctions("McpTools", tools.Select(t => t.AsKernelFunction()));

// ============================================================================
// 第六部分：建立 AI Agent（ChatCompletionAgent）
// ============================================================================
//
// ChatCompletionAgent：
//   Semantic Kernel 提供的 Agent 實作，結合 Chat Completion 模型與工具使用。
//   根據 Instructions（系統提示）、使用者輸入和對話歷史進行推理，
//   並在必要時自動呼叫已注入的 MCP 工具取得資料。
//
// FunctionChoiceBehavior.Auto()
//   允許 AI 模型自主決定是否呼叫工具及呼叫哪個工具（對應 GPT 的 tool_choice: "auto"）。
//   AI 會根據 [Description] 的說明文字判斷工具的適用場景。
//
// ============================================================================
ChatCompletionAgent agent =
    new()
    {
        Name = "SupportAgent",
        Description = "一個可以回答訂單資訊的助手",
        Instructions = @"你是一位專業且有禮貌的助手，負責協助顧客查詢訂單資訊。請依照以下規則提供回覆：
                        1. 如果詢問訂單狀態，請引導提供訂單編號或是顧客姓名，並使用已提供的查詢工具查詢對應資料。
                        2. 回覆時要友善、清楚，避免使用過於技術化的語言。
                        3. 若問題無法直接回答，請告知「我會轉交給專人協助」，並避免捏造答案。
                        4. 僅限提供查詢訂單狀態，若超出職責範圍，請禮貌回覆並引導聯繫客服專線0800-888-888。",
        Kernel = kernel,
        Arguments = new(new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
    };

// ============================================================================
// 第七部分：建立對話歷史 Thread
// ============================================================================
//
// ChatHistoryAgentThread：
//   維護與 Agent 的完整對話歷史（含使用者訊息、AI 回應、工具呼叫結果）。
//   每一輪對話都以同一個 agentThread 傳入，讓 AI 可以追蹤上下文。
//   agentThread.Id 是本次對話的唯一識別碼，可用於追蹤或持久化。
//
// ============================================================================
ChatHistoryAgentThread agentThread = new();

// ============================================================================
// 第八部分：互動對話迴圈
// ============================================================================
Console.Write("User > ");
string? userInput;
while ((userInput = Console.ReadLine()) is not null)
{
    // 輸入 exit 或空白行結束程式
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    // 包裝使用者輸入為 ChatMessageContent（MCP 協定傳遞的訊息格式）
    ChatMessageContent message = new(AuthorRole.User, userInput);

    // ────────────────────────────────────────────────────────────────────
    // InvokeStreamingAsync()
    // ────────────────────────────────────────────────────────────────────
    // 以串流方式呼叫 Agent，逐字接收回應（類似 ChatGPT 的打字效果）。
    //
    // 每個 StreamingChatMessageContent 可能包含：
    //   A. 文字內容（response.Content 有值）  → 直接輸出到 Console
    //   B. 工具呼叫事件（Content 為空，Items 含 StreamingFunctionCallUpdateContent）
    //      → AI 正在決定或執行工具呼叫，此時只印出 trace 追蹤訊息
    //
    // 整個流程（包含工具呼叫）在這個 await foreach 迴圈中自動完成：
    //   1. AI 判斷需要工具 → 產生 Function Call 事件（trace 輸出）
    //   2. SDK 透過 MCP 協定呼叫 Server 上的工具（HTTP POST）
    //   3. 取得工具回傳結果後，AI 繼續生成回應文字（逐字串流輸出）
    // ────────────────────────────────────────────────────────────────────
    bool isFirst = false;
    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread))
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            // 嘗試從 Items 中取出 Function Call 事件
            // SingleOrDefault：一個 chunk 最多只有一個 Function Call 事件
            StreamingFunctionCallUpdateContent? functionCall =
                response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();

            // 追蹤工具呼叫（僅當 Name 有值時表示是新的 Function Call 開始）
            if (!string.IsNullOrEmpty(functionCall?.Name))
            {
                Console.WriteLine($"\n# trace {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
            }
            continue;
        }

        // 第一個文字 chunk 時，先輸出角色標頭
        if (!isFirst)
        {
            Console.Write($"{response.Role} - {response.AuthorName ?? "*"} > ");
            isFirst = true;
        }

        // 逐字輸出 AI 回應（串流效果）
        Console.Write($"{response.Content}");
    }
    Console.WriteLine();

    // 輸出本輪對話的 Thread 資訊（用於追蹤對話狀態）
    Console.WriteLine($"\n# trace chat thread with agent: {agent.Name} - {agent.Description},threadId: {agentThread.Id} \n");
    Console.Write("User > ");
}
