/*
 * ============================================================================
 * Semantic Kernel ChatCompletionAgent 客服系統示範
 * ============================================================================
 *
 * 程式概述：
 * 這是一個使用 Microsoft Semantic Kernel 框架建立的 AI 客服聊天機器人。
 * 透過 ChatCompletionAgent 結合自訂 Plugin，實現訂單查詢與產品資訊查詢功能。
 *
 * 主要功能：
 * 1. 訂單狀態查詢 - 透過 GetOrderStatus 函數查詢訂單狀態
 * 2. 產品資訊查詢 - 透過 GetProductInfo 函數查詢產品詳情
 * 3. 對話歷史記憶 - 使用 ChatHistoryAgentThread 保持對話上下文
 * 4. 串流回應 - 即時顯示 AI 回應，提供更好的使用者體驗
 *
 * 技術架構：
 * ┌─────────────────────────────────────────────────────────────┐
 * │                      使用者介面 (Console)                    │
 * └─────────────────────────────────────────────────────────────┘
 *                              ↓↑
 * ┌─────────────────────────────────────────────────────────────┐
 * │                   ChatCompletionAgent                       │
 * │  ┌─────────────────┐  ┌─────────────────────────────────┐  │
 * │  │   Instructions  │  │  FunctionChoiceBehavior.Auto()  │  │
 * │  │   (系統提示詞)   │  │     (自動選擇呼叫函數)           │  │
 * │  └─────────────────┘  └─────────────────────────────────┘  │
 * └─────────────────────────────────────────────────────────────┘
 *                              ↓↑
 * ┌─────────────────────────────────────────────────────────────┐
 * │                      Semantic Kernel                        │
 * │  ┌─────────────────────────────────────────────────────┐   │
 * │  │              CustomerSupportService Plugin           │   │
 * │  │  ┌───────────────────┐  ┌───────────────────────┐   │   │
 * │  │  │  GetOrderStatus   │  │   GetProductInfo      │   │   │
 * │  │  │  (查詢訂單狀態)    │  │   (查詢產品資訊)       │   │   │
 * │  │  └───────────────────┘  └───────────────────────┘   │   │
 * │  └─────────────────────────────────────────────────────┘   │
 * └─────────────────────────────────────────────────────────────┘
 *                              ↓↑
 * ┌─────────────────────────────────────────────────────────────┐
 * │                    OpenAI GPT-4o API                        │
 * └─────────────────────────────────────────────────────────────┘
 *
 * 核心元件說明：
 * - Kernel: Semantic Kernel 的核心，負責協調 AI 模型與 Plugin
 * - ChatCompletionAgent: 封裝聊天完成功能的 Agent，具有名稱、描述和指令
 * - ChatHistoryAgentThread: 管理對話歷史的執行緒，讓 AI 記住之前的對話
 * - CustomerSupportService: 自訂 Plugin，提供業務邏輯函數給 AI 呼叫
 *
 * 使用前準備：
 * 1. 設定環境變數 OPENAI_API_KEY
 * 2. 確保已安裝必要的 NuGet 套件
 *
 * ============================================================================
 */

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

// ============================================================================
// 第一部分：環境設定與 API Key 驗證
// ============================================================================
// 從環境變數讀取 OpenAI API Key
// 這是安全的做法，避免將敏感資訊硬編碼在程式碼中
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數");
}

// ============================================================================
// 第二部分：建立 Semantic Kernel
// ============================================================================
// Kernel 是 Semantic Kernel 的核心元件，負責：
// 1. 管理 AI 模型連接（這裡使用 OpenAI GPT-4o）
// 2. 註冊和管理 Plugin（自訂函數）
// 3. 協調 AI 推理與函數呼叫
var kernel = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(
                        modelId: "gpt-4o",  // 使用 GPT-4o 模型，支援函數呼叫與串流
                        apiKey: apiKey)
                    .Build();

// ============================================================================
// 第三部分：註冊 Plugin（外掛程式）
// ============================================================================
// Plugin 是一組可被 AI 呼叫的函數
// CustomerSupportService 包含：
// - GetOrderStatus: 查詢訂單狀態
// - GetProductInfo: 查詢產品資訊
//
// AddFromType<T>() 會自動掃描類別中標記 [KernelFunction] 的方法
// 並將其註冊為可供 AI 使用的函數
kernel.Plugins.AddFromType<CustomerSupportService>();

// ============================================================================
// 第四部分：建立 ChatCompletionAgent
// ============================================================================
// ChatCompletionAgent 是 Semantic Kernel Agents 框架中的核心元件
// 它封裝了聊天完成功能，並提供：
// - Name: Agent 的識別名稱
// - Description: Agent 的功能描述
// - Instructions: 系統提示詞，定義 Agent 的行為規則
// - Kernel: 連接的 Semantic Kernel 實例
// - Arguments: 執行設定，包含函數呼叫行為
ChatCompletionAgent agent =
    new(){
        // Agent 名稱，用於識別和日誌記錄
        Name = "SupportAgent",

        // Agent 描述，說明此 Agent 的用途
        Description = "一個可以回答訂單狀態與產品資訊的AI客服",

        // 系統提示詞（Instructions）
        // 這是 Agent 的「人設」和行為規則
        // AI 會根據這些指令來回應使用者
        Instructions = @"你是一位專業且有禮貌的 AI 客服專員，負責協助顧客查詢訂單狀態與產品資訊。請依照以下規則提供回覆：
                        1. 如果顧客詢問訂單狀態，請引導顧客提供訂單編號，並使用已提供的查詢工具（如：GetOrderStatus）查詢對應資料。
                        2. 如果顧客詢問產品資訊，請根據產品名稱，並使用產品查詢工具（如：GetProductInfo）提供詳細資訊。
                        3. 回覆時要友善、清楚，避免使用過於技術化的語言。
                        4. 若問題無法直接回答，請告知顧客「我會轉交給專人協助」，並避免捏造答案。
                        5. 僅限提供查詢訂單狀態與產品資訊，若超出職責範圍，請禮貌回覆並引導顧客聯繫客服專線0800-888-888。",

        // 連接 Kernel，讓 Agent 可以使用已註冊的 Plugin
        Kernel = kernel,

        // 執行參數設定
        // FunctionChoiceBehavior.Auto() 讓 AI 自動決定何時呼叫函數
        // 其他選項包括：
        // - FunctionChoiceBehavior.Required(): 強制呼叫函數
        // - FunctionChoiceBehavior.None(): 禁止呼叫函數
        Arguments = new(new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
    };

// ============================================================================
// 第五部分：建立對話歷史執行緒
// ============================================================================
// ChatHistoryAgentThread 負責管理對話歷史
// 它會自動追蹤所有的對話訊息，讓 AI 能夠：
// 1. 理解對話上下文
// 2. 記住之前討論的內容
// 3. 提供連貫的回應
//
// 每個 Thread 都有唯一的 Id，可用於識別和管理不同的對話
ChatHistoryAgentThread chatHistoryAgentThread = new();

// ============================================================================
// 第六部分：使用者介面初始化
// ============================================================================
// 顯示歡迎訊息和使用說明
Console.WriteLine("=== Semantic Kernel Plugin + CustomerSupport 查詢訂單示範 ===");
Console.WriteLine("這個應用程式會記住您的對話歷史，讓 AI 能夠理解上下文。");
Console.WriteLine("這個應用程式可查詢訂單狀態與產品資訊．也可查詢對話歷史");
Console.WriteLine("試試看問「我想查訂單？」，問某項「產品資訊？」以及問問「我剛問了哪些問題？」");
Console.WriteLine("輸入 'exit' 或 'quit' 離開程式");
Console.WriteLine("================================================\n");

// ============================================================================
// 第七部分：主要對話迴圈
// ============================================================================
// 這是程式的主要執行迴圈，持續接收使用者輸入並處理
while (true)
{
    // 顯示提示符號，等待使用者輸入
    Console.Write("您: ");
    var input = Console.ReadLine();

    // ------------------------------------------------------------------------
    // 處理退出命令
    // ------------------------------------------------------------------------
    // 當使用者輸入空白、exit 或 quit 時結束程式
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再見！");
        break;
    }

    // ------------------------------------------------------------------------
    // 建立使用者訊息
    // ------------------------------------------------------------------------
    // ChatMessageContent 封裝了訊息的角色（User/Assistant/System）和內容
    // AuthorRole.User 表示這是來自使用者的訊息
    ChatMessageContent message = new (AuthorRole.User, input);

    // ------------------------------------------------------------------------
    // 串流處理 Agent 回應
    // ------------------------------------------------------------------------
    // InvokeStreamingAsync 以串流方式取得 AI 回應
    // 這樣可以即時顯示回應，而不需等待完整回應生成
    //
    // 參數說明：
    // - message: 使用者的輸入訊息
    // - chatHistoryAgentThread: 對話歷史執行緒，AI 會參考之前的對話
    bool isFirst = false;  // 用於控制是否已顯示回應前綴

    await foreach(StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, chatHistoryAgentThread))
    {
        // --------------------------------------------------------------------
        // 處理函數呼叫追蹤
        // --------------------------------------------------------------------
        // 當 response.Content 為空時，可能是函數呼叫事件
        // 我們可以從 Items 中取得 StreamingFunctionCallUpdateContent
        // 來追蹤 AI 正在呼叫哪個函數
        if (string.IsNullOrEmpty(response.Content))
        {
            // 嘗試取得函數呼叫資訊
            StreamingFunctionCallUpdateContent? functionCall = response.Items
                .OfType<StreamingFunctionCallUpdateContent>()
                .SingleOrDefault();

            // 如果有函數呼叫，顯示追蹤訊息
            // 這有助於了解 AI 的決策過程
            if (!string.IsNullOrEmpty(functionCall?.Name))
            {
                Console.WriteLine($"\n# trace {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
            }
            continue;  // 跳過空內容，繼續處理下一個串流片段
        }

        // --------------------------------------------------------------------
        // 顯示回應內容
        // --------------------------------------------------------------------
        // 第一次收到有內容的回應時，顯示前綴（角色和作者名稱）
        if (!isFirst)
        {
            Console.Write($"{response.Role} - {response.AuthorName ?? "*"} > ");
            isFirst = true;
        }

        // 即時輸出回應內容（串流效果）
        Console.Write($"{response.Content}");
    }

    // ------------------------------------------------------------------------
    // 顯示對話追蹤資訊
    // ------------------------------------------------------------------------
    // 每次對話後顯示 Agent 資訊和 Thread ID
    // 這有助於除錯和了解系統狀態
    Console.WriteLine();
    Console.WriteLine($"\n# trace chat thread with agent: {agent.Name} - {agent.Description},threadId: {chatHistoryAgentThread.Id} \n");
}

/*
 * ============================================================================
 * 延伸閱讀與相關資源
 * ============================================================================
 *
 * 1. Semantic Kernel 官方文件：
 *    https://learn.microsoft.com/semantic-kernel/
 *
 * 2. Semantic Kernel Agents 文件：
 *    https://learn.microsoft.com/semantic-kernel/agents/
 *
 * 3. OpenAI Function Calling：
 *    https://platform.openai.com/docs/guides/function-calling
 *
 * 4. 相關 NuGet 套件：
 *    - Microsoft.SemanticKernel
 *    - Microsoft.SemanticKernel.Agents.Core
 *    - Microsoft.SemanticKernel.Connectors.OpenAI
 *
 * ============================================================================
 */
