/*
 * ============================================================================
 * Program.cs - Semantic Kernel OpenAI Assistant Agent 客服系統主程式
 * ============================================================================
 *
 * 本程式展示如何使用 Microsoft Semantic Kernel 的 OpenAI Assistant Agent
 * 建立一個具備「工具呼叫」能力的智慧客服系統。
 *
 * 核心架構：
 * ┌──────────────────────────────────────────────────────────────────────┐
 * │                        程式執行流程                                  │
 * │                                                                      │
 * │  1. 讀取 API Key → 建立 AssistantClient                             │
 * │  2. 建立 Assistant（定義模型、名稱、系統指令）                        │
 * │  3. 註冊 Plugin（CustomerSupportService 的函數）                      │
 * │  4. 建立 Agent（將 Assistant + Client + Plugin 組合）                 │
 * │  5. 建立 Thread（對話執行緒，維持上下文記憶）                         │
 * │  6. 進入互動迴圈：                                                   │
 * │     使用者輸入 → Agent 串流處理 → 即時顯示回應                       │
 * └──────────────────────────────────────────────────────────────────────┘
 *
 * OpenAI Assistant Agent vs ChatCompletion Agent 差異：
 * ┌────────────────────────┬─────────────────────────────────────────────┐
 * │  ChatCompletionAgent   │  OpenAIAssistantAgent                      │
 * ├────────────────────────┼─────────────────────────────────────────────┤
 * │  對話歷史由本地管理     │  對話歷史由 OpenAI 伺服器端管理（Thread）   │
 * │  使用 ChatHistory 物件 │  使用 AgentThread 物件                      │
 * │  每次請求傳送完整歷史   │  透過 Thread ID 自動維護上下文              │
 * │  較輕量，適合簡單對話   │  支援更多進階功能（Code Interpreter 等）    │
 * └────────────────────────┴─────────────────────────────────────────────┘
 *
 * 使用的 NuGet 套件：
 * - Microsoft.SemanticKernel                  (核心框架)
 * - Microsoft.SemanticKernel.Agents.OpenAI    (OpenAI Assistant Agent 支援)
 * - Microsoft.SemanticKernel.Connectors.OpenAI(OpenAI 連接器)
 *
 * ============================================================================
 */

// ============================================================================
// 抑制實驗性 API 警告
// ============================================================================
// OpenAI Assistants API 目前仍標記為實驗性（Experimental），
// 編譯器會產生 OPENAI001 警告。此 pragma 指令抑制這些警告，
// 表示我們了解該 API 未來可能變更，並接受這個風險。
#pragma warning disable OPENAI001

// ============================================================================
// 命名空間引用
// ============================================================================
using Microsoft.SemanticKernel;            // Semantic Kernel 核心（KernelPlugin、KernelPluginFactory 等）
using Microsoft.SemanticKernel.Agents;     // Agent 基礎抽象（StreamingChatMessageContent 等）
using Microsoft.SemanticKernel.ChatCompletion; // 聊天完成相關型別（AuthorRole、ChatMessageContent 等）

using System.ClientModel;                  // ApiKeyCredential — 用於安全地包裝 API Key
using Microsoft.SemanticKernel.Agents.OpenAI; // OpenAIAssistantAgent、OpenAIAssistantAgentThread
using OpenAI.Assistants;                   // AssistantClient、Assistant — OpenAI Assistants API 底層型別


// ============================================================================
// 第一部分：環境設定與 API Key 驗證
// ============================================================================
// 從環境變數讀取 OpenAI API Key
// 這是安全的做法，避免將敏感資訊硬編碼在程式碼中
// 設定方式：export OPENAI_API_KEY="sk-xxxxxxx"（macOS/Linux）
//          set OPENAI_API_KEY=sk-xxxxxxx（Windows CMD）
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數");
}


// ============================================================================
// 第二部分：建立 OpenAI Assistants API 客戶端
// ============================================================================
// AssistantClient 是 OpenAI SDK 中用來管理 Assistant 的客戶端
// ApiKeyCredential 將 API Key 包裝為安全的認證物件，避免直接傳遞原始字串
// 此客戶端負責：建立/刪除 Assistant、建立/管理 Thread、執行對話等
AssistantClient assistantClient = new AssistantClient(new ApiKeyCredential(apiKey));


// ============================================================================
// 第三部分：建立 OpenAI Assistant（在 OpenAI 伺服器端建立）
// ============================================================================
// CreateAssistantAsync 會向 OpenAI API 發送請求，在伺服器端建立一個 Assistant
// 參數說明：
//   - "gpt-4o"：指定使用的 LLM 模型
//   - name：Assistant 的名稱，用於識別和追蹤
//   - instructions：系統指令（System Prompt），定義 Agent 的行為規範
//
// 注意：每次程式執行都會建立一個新的 Assistant，
// 正式環境應考慮重用已建立的 Assistant（透過 Assistant ID）
Assistant assistant = await assistantClient.CreateAssistantAsync(
    "gpt-4o",
    name : "Support Agent",
    instructions: @"你是一位專業且有禮貌的 AI 客服專員，負責協助顧客查詢訂單狀態與產品資訊。請依照以下規則提供回覆：
                    1. 如果顧客詢問訂單狀態，請引導顧客提供訂單編號，並使用已提供的查詢工具（如：GetOrderStatus）查詢對應資料。
                    2. 如果顧客詢問產品資訊，請根據產品名稱，並使用產品查詢工具（如：GetProductInfo）提供詳細資訊。
                    3. 回覆時要友善、清楚，避免使用過於技術化的語言。
                    4. 若問題無法直接回答，請告知顧客「我會轉交給專人協助」，並避免捏造答案。
                    5. 僅限提供查詢訂單狀態與產品資訊，若超出職責範圍，請禮貌回覆並引導顧客聯繫客服專線0800-888-888。");


// ============================================================================
// 第四部分：建立 Kernel Plugin（工具註冊）
// ============================================================================
// KernelPluginFactory.CreateFromType<T>() 會掃描 CustomerSupportService 類別，
// 將所有標記 [KernelFunction] 的方法註冊為 AI 可呼叫的工具（Tools / Functions）
//
// Plugin 的運作原理：
//   1. 掃描類別中所有帶 [KernelFunction] 屬性的方法
//   2. 讀取 [Description] 屬性作為函數描述（AI 用此判斷何時呼叫）
//   3. 讀取參數的 [Description] 屬性（AI 用此了解需要什麼輸入）
//   4. 將這些資訊轉換為 OpenAI Function Calling 的 JSON Schema
//
// 本範例註冊的函數：
//   - GetOrderStatus(orderId)：查詢訂單狀態
//   - GetProductInfo(productName)：查詢產品資訊
KernelPlugin plugin = KernelPluginFactory.CreateFromType<CustomerSupportService>();


// ============================================================================
// 第五部分：組裝 OpenAIAssistantAgent
// ============================================================================
// OpenAIAssistantAgent 將三個元件組合在一起：
//   1. assistant：OpenAI 伺服器端的 Assistant 定義（模型、指令）
//   2. assistantClient：與 OpenAI API 通訊的客戶端
//   3. [plugin]：已註冊的工具集合（Collection Expression 語法，等同 new[] { plugin }）
//
// Agent 會自動將 Plugin 中的函數轉換為 OpenAI Assistant 的 Tools，
// 使 AI 能在對話中根據需要呼叫這些函數
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);


// ============================================================================
// 第六部分：建立對話執行緒（Thread）
// ============================================================================
// OpenAIAssistantAgentThread 代表一個 OpenAI Assistants API 的對話執行緒
// Thread 的特性：
//   - 對話歷史儲存在 OpenAI 伺服器端，不需要本地維護
//   - 每個 Thread 有唯一的 ID，可用於追蹤和除錯
//   - Agent 自動透過 Thread 維持對話上下文（記住之前的對話內容）
//   - 與 ChatCompletionAgent 不同，不需要手動管理 ChatHistory 物件
OpenAIAssistantAgentThread agentThread = new(assistantClient);


// ============================================================================
// 第七部分：顯示歡迎訊息
// ============================================================================
Console.WriteLine("=== Semantic Kernel Plugin + CustomerSupport 查詢訂單示範 ===");
Console.WriteLine("這個應用程式會記住您的對話歷史，讓 AI 能夠理解上下文。");
Console.WriteLine("這個應用程式可查詢訂單狀態與產品資訊．也可查詢對話歷史");
Console.WriteLine("試試看問「我想查訂單？」，問某項「產品資訊？」以及問問「我剛問了哪些問題？」");
Console.WriteLine("輸入 'exit' 或 'quit' 離開程式");
Console.WriteLine("================================================\n");


// ============================================================================
// 第八部分：主要互動迴圈
// ============================================================================
// 無限迴圈持續接收使用者輸入，直到使用者輸入 exit/quit 為止
// 每一輪迴圈的流程：
//   1. 接收使用者輸入
//   2. 包裝為 ChatMessageContent
//   3. 透過 Agent 串流處理（InvokeStreamingAsync）
//   4. 即時顯示 AI 回應（含函數呼叫追蹤）
while (true){
    // 顯示提示符號，等待使用者輸入
    Console.Write("您: ");
    var input = Console.ReadLine();

    // ------------------------------------------------------------------------
    // 處理退出命令
    // ------------------------------------------------------------------------
    // 當使用者輸入空白、exit 或 quit 時結束程式
    // StringComparison.OrdinalIgnoreCase 使比較不區分大小寫
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再見！");
        break;
    }

    // ------------------------------------------------------------------------
    // 建立使用者訊息並執行串流回應
    // ------------------------------------------------------------------------
    // ChatMessageContent 封裝了訊息的角色（User/Assistant/System）和內容
    // AuthorRole.User 表示這是來自使用者的訊息
    ChatMessageContent message = new (AuthorRole.User, input);

    // isFirst 旗標用於控制是否已顯示回應前綴（角色 + 作者名稱）
    // 確保前綴只在第一個有內容的串流片段時顯示一次
    bool isFirst = false;

    // ------------------------------------------------------------------------
    // InvokeStreamingAsync — 串流式呼叫 Agent
    // ------------------------------------------------------------------------
    // 此方法向 OpenAI Assistants API 發送訊息並以串流方式接收回應
    // 參數：
    //   - message：使用者的訊息內容
    //   - agentThread：對話執行緒（Agent 會自動將訊息加入 Thread）
    //
    // 返回：IAsyncEnumerable<StreamingChatMessageContent>
    //   每個 StreamingChatMessageContent 包含：
    //   - Content：文字內容片段（可能為空，例如函數呼叫時）
    //   - Role：訊息角色（Assistant）
    //   - AuthorName：作者名稱（Agent 名稱）
    //   - Items：附加內容項目（可能包含 StreamingFunctionCallUpdateContent）
    //
    // await foreach 用於非同步迭代串流資料，每收到一個片段就立即處理
    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message,agentThread))
    {
        // ====================================================================
        // 處理空內容（通常是函數呼叫事件）
        // ====================================================================
        if (string.IsNullOrEmpty(response.Content))
        {
            // 嘗試從 Items 集合中取得函數呼叫資訊
            // StreamingFunctionCallUpdateContent 包含：
            //   - Name：被呼叫的函數名稱（如 "GetOrderStatus"）
            //   - Arguments：函數的參數（JSON 格式）
            // SingleOrDefault()：預期最多只有一個函數呼叫，否則回傳 null
            StreamingFunctionCallUpdateContent? functionCall = response.Items
                .OfType<StreamingFunctionCallUpdateContent>()
                .SingleOrDefault();

            // 如果有函數呼叫，顯示追蹤訊息（Trace）
            // 這有助於開發者了解 AI 的決策過程：
            //   AI 分析了使用者意圖 → 決定呼叫哪個函數 → 傳入什麼參數
            if (!string.IsNullOrEmpty(functionCall?.Name))
            {
                Console.WriteLine($"\n# trace {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
            }
            continue;  // 跳過空內容，繼續處理下一個串流片段
        }

        // ====================================================================
        // 顯示回應內容（串流即時輸出）
        // ====================================================================
        // 第一次收到有內容的回應時，顯示前綴（角色和作者名稱）
        // 格式範例："assistant - Support Agent > 您好！請問有什麼..."
        if (!isFirst)
        {
            Console.Write($"{response.Role} - {response.AuthorName ?? "*"} > ");
            isFirst = true;
        }

        // 使用 Console.Write（非 WriteLine）即時輸出每個串流片段
        // 這樣可以實現「打字機效果」，讓使用者看到文字逐步出現
        Console.Write($"{response.Content}");

    }

    // ------------------------------------------------------------------------
    // 顯示對話追蹤資訊（Debug / Trace）
    // ------------------------------------------------------------------------
    // 每次對話後顯示 Agent 資訊和 Thread ID
    // 這有助於除錯和了解系統狀態：
    //   - agent.Name：Agent 名稱（"Support Agent"）
    //   - agent.Description：Agent 描述
    //   - agentThread.Id：OpenAI Thread ID（可用於在 OpenAI Dashboard 中查找對話記錄）
    Console.WriteLine();
    Console.WriteLine($"\n# trace chat thread with agent: {agent.Name} - {agent.Description},threadId: {agentThread.Id} \n");



}




Console.WriteLine("Hello, World!");
