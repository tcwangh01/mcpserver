// ==========================================================================
// Azure AI Agent 互動式管理工具
// --------------------------------------------------------------------------
// 本程式展示如何使用 Semantic Kernel + Azure AI Agent (Persistent Agent) 建立
// 一個可透過終端機互動操作的 AI 商務助理。
//
// 主要流程：
//   1. 使用者輸入 "create agent" → 在 Azure AI Foundry 上建立一個持久化 Agent
//   2. 建立後即可直接輸入訊息與 Agent 對話（支援串流回應）
//   3. 輸入 "delete agent" / "刪除 agent" → 刪除 Agent 及其對話 Thread
//   4. 輸入 "exit" → 離開程式（若 Agent 尚未刪除，會自動清理）
//
// 必要套件（已在 .csproj 中設定）：
//   dotnet add package Microsoft.SemanticKernel.Agents.AzureAI --prerelease
//   dotnet add package Azure.Identity
//
// 驗證方式：
//   使用 Azure CLI 的 Entra ID 驗證，執行前請先登入：
//   az login --tenant <tenant-id>
// ==========================================================================

using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;

// ==========================================================================
// 設定區：Azure AI Foundry 專案的端點與部署模型 ID
// --------------------------------------------------------------------------
// azureEndpoint: Azure AI Foundry 專案端點（非 Azure OpenAI 端點）
//   格式：https://<resource>.services.ai.azure.com/api/projects/<project>
//   注意：若使用 Azure OpenAI 端點（cognitiveservices.azure.com）會回傳 404
// modelId: 在 Azure AI Foundry 中部署的模型名稱
// ==========================================================================
//var azureEndpoint = "https://my-azure-ai-agent-testing-area.cognitiveservices.azure.com/";
var azureEndpoint = "https://my-azure-ai-agent-testing-area.services.ai.azure.com/api/projects/ai-agent-poc-proj";
var modelId = "gpt-4o-mini";

// ==========================================================================
// 初始化
// --------------------------------------------------------------------------
// PersistentAgentsClient: 用於與 Azure AI Agent 服務溝通的客戶端
//   - 透過 AzureAIAgent.CreateAgentsClient() 建立，傳入端點及認證方式
//   - AzureCliCredential 會使用本機 az login 的身分進行驗證
//
// KernelPlugin: 將 BusinessAssistantService 類別註冊為 Semantic Kernel Plugin
//   - Plugin 中的方法（標註 [KernelFunction]）會被 Agent 當作可呼叫的工具
//   - Agent 會根據使用者的問題自動判斷是否需要呼叫這些工具
// ==========================================================================
PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(azureEndpoint, new AzureCliCredential());
KernelPlugin plugin = KernelPluginFactory.CreateFromType<BusinessAssistantService>();

// agent 與 agentThread 初始為 null，等使用者輸入 "create agent" 後才建立
AzureAIAgent? agent = null;
AzureAIAgentThread? agentThread = null;

// 顯示歡迎畫面與可用指令說明
Console.WriteLine("=== Azure AI Agent 互動式管理工具 ===\n");
Console.WriteLine("可用指令：");
Console.WriteLine("  create agent  - 建立新的 Azure AI Agent（部署至 Azure AI Foundry）");
Console.WriteLine("  刪除 agent    - 刪除目前的 Agent（或 delete agent）");
Console.WriteLine("  exit          - 離開程式\n");
Console.WriteLine("請先輸入 'create agent' 建立 Agent，建立後即可直接輸入訊息進行對話。\n");

// ==========================================================================
// 主迴圈：持續讀取使用者輸入，根據指令執行對應操作
// --------------------------------------------------------------------------
// 提示符號會依狀態切換：
//   - ">"      : Agent 尚未建立，等待指令
//   - "User >" : Agent 已建立，可以對話
// ==========================================================================
Console.Write("> ");
string? input;
while ((input = Console.ReadLine()) is not null)
{
    // 忽略空白輸入
    if (string.IsNullOrWhiteSpace(input))
    {
        Console.Write(agent is not null ? "User > " : "> ");
        continue;
    }

    var trimmed = input.Trim();

    // ===== 指令：exit / quit → 跳出主迴圈 =====
    if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // ==========================================================================
    // 指令：create agent → 在 Azure AI Foundry 上建立持久化 Agent
    // --------------------------------------------------------------------------
    // 流程：
    //   1. 定義 Agent 的 instructions（系統提示詞），告訴 AI 它的角色與行為規範
    //   2. 呼叫 client.Administration.CreateAgentAsync() 在 Azure 上建立 Agent
    //      - 這會在 Azure AI Foundry 上建立一個持久化的 Agent 資源
    //      - 即使程式結束，Agent 仍會存在於 Azure 上（除非主動刪除）
    //   3. 用回傳的 PersistentAgent 定義建立本地的 AzureAIAgent 包裝物件
    //      - 設定 Kernel 並加入 ChatCompletion 服務（用於處理對話）
    //      - 掛載 BusinessAssistantService Plugin（自訂工具函式）
    //   4. 建立 AzureAIAgentThread 作為對話的 Thread（對話記錄容器）
    //
    // 重要概念：
    //   - PersistentAgent: Azure 上的 Agent 資源定義（持久化，有獨立 ID）
    //   - AzureAIAgent: Semantic Kernel 對 PersistentAgent 的本地包裝
    //   - AzureAIAgentThread: 對話 Thread，儲存一連串的訊息來回
    //   - 自訂 Plugin 不會被 Agent 持久化，每次啟動都需要重新掛載
    // ==========================================================================
    if (trimmed.Equals("create agent", StringComparison.OrdinalIgnoreCase))
    {
        // 防止重複建立
        if (agent is not null)
        {
            Console.WriteLine($"[!] Agent 已存在 (ID: {agent.Id})，請先輸入 '刪除 agent' 刪除後再建立新的。\n");
            Console.Write("User > ");
            continue;
        }

        Console.WriteLine("[*] 正在建立 Azure AI Agent...");

        // Agent 的系統指令（System Prompt）：定義 AI 的角色、能力範圍與行為準則
        string instructions = """
            你是一位專業的商務助理 AI，負責協助查詢以下資訊：
            1. 根據客戶名稱，查詢客戶的帳戶資訊與合約狀態。
            2. 根據業務代表姓名，查詢對應業務代表的聯絡資訊。

            請遵守以下原則：
            - 回覆時要簡潔清楚、準確不誤導。
            - 當使用者輸入不明確或資訊不足時，請主動詢問更多細節。
            - 絕對不能憑空編造任何資訊（例如：合約狀態、聯絡方式）。
            - 一律透過內部資料庫或指定 API 工具來取得資料，不要自行假設。
            - 如果查無資料，請禮貌地說明找不到相關資訊。

            若使用者的問題超出你的職責範圍，請清楚說明你能處理的類型，並建議他重新描述需求或洽詢相關單位。
            """;

        // 步驟 1：在 Azure AI Foundry 上建立持久化 Agent 定義
        PersistentAgent definition = await client.Administration.CreateAgentAsync(
            modelId,
            name: "Business Assistant",
            description: "這是一位專業的商務助理 AI，專門協助內部人員快速查詢客戶帳戶資訊、合約狀態，以及對應業務聯絡方式。",
            instructions: instructions);

        // 步驟 2：建立 Semantic Kernel 的 AzureAIAgent 本地包裝
        //   - 設定 Kernel 並加入 AzureOpenAI ChatCompletion 服務
        //   - 這個 Kernel 用於本地端處理 Plugin 的函式呼叫
        agent = new AzureAIAgent(definition, client)
        {
            Kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(modelId, azureEndpoint, new AzureCliCredential())
                .Build()
        };

        // 步驟 3：掛載自訂 Plugin（BusinessAssistantService）
        //   - 自訂 Plugin 不會隨 Agent 持久化到 Azure，每次啟動都要重新掛載
        agent.Kernel.Plugins.Add(plugin);

        // 步驟 4：建立對話 Thread（用於儲存這次對話的所有訊息）
        agentThread = new AzureAIAgentThread(agent.Client);

        Console.WriteLine($"[v] Agent 已建立！");
        Console.WriteLine($"    Agent ID  : {agent.Id}");
        Console.WriteLine($"    Agent Name: {agent.Name}");
        Console.WriteLine($"    Thread ID : {agentThread.Id}");
        Console.WriteLine($"    您可以在 Azure AI Foundry 中查看此 Agent。");
        Console.WriteLine($"\n現在可以開始對話，輸入 'delete agent' 可刪除 Agent。\n");
        Console.Write("User > ");
        continue;
    }

    // ==========================================================================
    // 指令：刪除 agent / delete agent → 刪除 Azure 上的 Agent 與對話 Thread
    // --------------------------------------------------------------------------
    // 流程：
    //   1. 先刪除 Thread（對話記錄）
    //   2. 再刪除 Agent（Azure AI Foundry 上的資源）
    //   3. 將本地參考設為 null
    // ==========================================================================
    if (trimmed.Equals("刪除 agent", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("刪除agent", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("delete agent", StringComparison.OrdinalIgnoreCase))
    {
        if (agent is null)
        {
            Console.WriteLine("[!] 目前沒有 Agent 可以刪除。\n");
            Console.Write("> ");
            continue;
        }

        var agentId = agent.Id;
        Console.WriteLine($"[*] 正在刪除 Agent (ID: {agentId})...");

        // 先刪除對話 Thread
        if (agentThread is not null)
        {
            await agentThread.DeleteAsync();
            agentThread = null;
        }

        // 再刪除 Azure 上的 Agent 資源
        await client.Administration.DeleteAgentAsync(agentId);
        agent = null;

        Console.WriteLine($"[v] Agent (ID: {agentId}) 與對話 Thread 已刪除。\n");
        Console.Write("> ");
        continue;
    }

    // ==========================================================================
    // 對話模式：將使用者輸入發送給 Agent，以串流方式接收回應
    // --------------------------------------------------------------------------
    // 流程：
    //   1. 將使用者輸入包裝為 ChatMessageContent（角色為 User）
    //   2. 呼叫 agent.InvokeStreamingAsync() 以串流方式取得回應
    //   3. 串流回應中會包含兩種內容：
    //      a. Function Call：Agent 決定呼叫 Plugin 中的工具（顯示 trace 資訊）
    //      b. 文字內容：Agent 的回覆文字（逐步輸出到終端機）
    //   4. 對話結束後顯示 trace 資訊（Agent 名稱、Thread ID）
    //
    // InvokeStreamingAsync 的運作方式：
    //   - Agent 分析使用者問題 → 判斷是否需要呼叫工具
    //   - 若需要，自動呼叫對應的 KernelFunction（如 GetCustomerInfo）
    //   - 取得工具回傳結果後，Agent 再組織自然語言回覆
    //   - 整個過程以串流方式逐步回傳給呼叫端
    // ==========================================================================
    if (agent is null)
    {
        Console.WriteLine("[!] 尚未建立 Agent，請先輸入 'create agent' 建立。\n");
        Console.Write("> ");
        continue;
    }

    // 將使用者輸入包裝為 User 角色的訊息
    ChatMessageContent message = new(AuthorRole.User, trimmed);
    bool isFirst = false;

    // 以串流方式接收 Agent 的回應
    await foreach (StreamingChatMessageContent response in agent.InvokeStreamingAsync(message, agentThread!))
    {
        // 情況 A：回應內容為空，檢查是否為 Function Call（Agent 正在呼叫工具）
        if (string.IsNullOrEmpty(response.Content))
        {
            var functionCall = response.Items.OfType<StreamingFunctionCallUpdateContent>().SingleOrDefault();
            if (!string.IsNullOrEmpty(functionCall?.Name))
            {
                // 輸出 trace：顯示 Agent 正在呼叫哪個 Plugin 函式
                Console.WriteLine($"\n# trace {response.Role} - {response.AuthorName ?? "*"}: FUNCTION CALL - {functionCall.Name}");
            }
            continue;
        }

        // 情況 B：有文字內容，逐步輸出 Agent 的回覆
        if (!isFirst)
        {
            // 第一段回覆，先印出角色與 Agent 名稱作為前綴
            Console.Write($"\n{response.Role} - {response.AuthorName ?? "*"} > ");
            isFirst = true;
        }

        // 串流輸出文字（不換行，持續接在同一行）
        Console.Write(response.Content);
    }

    Console.WriteLine();
    // 輸出 trace：顯示本次對話的 Agent 資訊與 Thread ID，方便除錯追蹤
    Console.WriteLine($"\n# trace chat thread with agent: {agent.Name} - {agent.Description}, threadId: {agentThread!.Id}\n");
    Console.Write("User > ");
}

// ==========================================================================
// 程式結束清理
// --------------------------------------------------------------------------
// 若使用者直接輸入 exit 但尚未刪除 Agent，程式會自動清理：
//   1. 刪除對話 Thread
//   2. 刪除 Azure 上的 Agent 資源
// 這樣可以避免在 Azure AI Foundry 上留下未使用的 Agent 資源
// ==========================================================================
if (agent is not null)
{
    Console.WriteLine($"\n[*] 程式結束前清理：正在刪除 Agent (ID: {agent.Id})...");
    if (agentThread is not null)
        await agentThread.DeleteAsync();
    await client.Administration.DeleteAgentAsync(agent.Id);
    Console.WriteLine("[v] Agent 已刪除。");
}

Console.WriteLine("再見！");
