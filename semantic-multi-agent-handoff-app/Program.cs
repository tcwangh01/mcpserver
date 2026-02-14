// ============================================================================
// Semantic Kernel Multi-Agent Handoff 示範應用程式
// ============================================================================
//
// 本程式展示如何使用 Microsoft Semantic Kernel 的 HandoffOrchestration 機制，
// 建立一個多代理人（Multi-Agent）企業客服系統。
//
// 【架構概述】
// 系統由 4 個 AI 代理人組成，採用「分流（Triage）→ 專責處理」的模式：
//
//   使用者提問
//       ↓
//   TriageAgent（分流代理人）
//       ↓ 根據問題類型，呼叫 transfer 工具轉交
//       ├── HRPolicyAgent     → 人資相關問題（請假、薪資、福利）
//       ├── ITSupportAgent    → IT 相關問題（VPN、帳號、設備）
//       └── ComplianceAgent   → 合規相關問題（資安、法規、公司治理）
//
// 【核心技術】
// 1. ChatCompletionAgent：基於聊天模型的代理人，支援 Function Calling
// 2. HandoffOrchestration：代理人間的轉交協調機制，自動注入 transfer 與
//    end_task_with_summary 工具，讓代理人能互相轉交任務並回傳最終結果
// 3. InProcessRuntime：在同一行程內運行的代理人執行環境
// 4. Plugin（KernelFunction）：自訂函數，供代理人查詢企業內部資料
//
// 【執行流程】
// 1. TriageAgent 收到使用者提問後，呼叫 transfer_to_XXXAgent 工具轉交
// 2. 專責代理人收到問題後，呼叫對應的 Plugin 函數查詢資料
// 3. 專責代理人整理回覆後，呼叫 end_task_with_summary 工具回傳結果
// 4. 主程式透過 GetValueAsync() 取得最終回應並顯示給使用者
// ============================================================================

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

Console.WriteLine("Hello, Multi-Agent System! \n\n");

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
// 3. 協調 AI 推理與函數呼叫（Function Calling）
var kernel = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(
                        modelId: "gpt-4o",  // 使用 GPT-4o 模型，支援函數呼叫與串流
                        apiKey: apiKey)
                    .Build();

// ============================================================================
// 第三部分：註冊 Plugin（企業服務函數）
// ============================================================================
// 將三個企業服務類別註冊為 Plugin，讓 AI 代理人可以透過 Function Calling 呼叫：
// - HRPolicyService：提供請假政策、福利政策、考勤規定等查詢功能
// - ITSupportService：提供 VPN 設定、帳號申請、備份政策等查詢功能
// - ComplianceService：提供合約條款、資安政策、公司治理等查詢功能
// 每個 Plugin 中的方法都標記了 [KernelFunction] 屬性，Semantic Kernel 會自動
// 將這些方法轉換為 OpenAI Function Calling 的工具定義
kernel.Plugins.AddFromType<HRPolicyService>();
kernel.Plugins.AddFromType<ITSupportService>();
kernel.Plugins.AddFromType<ComplianceService>();

// ============================================================================
// 第四部分：建立 AI 代理人（ChatCompletionAgent）
// ============================================================================
// 每個代理人都有以下屬性：
// - Kernel：共用同一個 Kernel 實例（共享 Plugin 和模型連接）
// - Name：代理人識別名稱，HandoffOrchestration 會用此名稱產生 transfer 工具
//         例如 Name="HRPolicyAgent" → 產生 transfer_to_HRPolicyAgent 工具
// - Description：代理人的角色描述，供其他代理人判斷何時轉交
// - Instructions：系統提示詞（System Prompt），定義代理人的行為準則與回覆風格

// 分流代理人（Triage Agent）
// 職責：判斷使用者問題的類型，並透過 transfer 工具轉交給對應的專責代理人
// 注意：此代理人不直接回答問題，僅負責分類與轉交
var triageAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "TriageAgent",
    Description = "問題分流助理",
    Instructions = @"對使用者的提問進行分類的企業支援代理。任務是判斷使用者問題屬於哪一類：人資(HR)、IT、或合規(Compliance)。
            若不屬於任何類型，也可請使用者釐清問題類別。"
};

// 人資代理人（HR Policy Agent）
// 職責：回答與人事制度、請假規定、員工福利相關的問題
// 可呼叫的 Plugin 函數：GetLeavePolicy、GetBenefitPolicy、GetAttendancePolicy
// 回答完畢後須呼叫 end_task_with_summary 工具，將結果回傳給 orchestration
var hrAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "HRPolicyAgent",
    Description = "負責回應與人資制度與公司內部 HR 規章相關的問題",
    Instructions = """
            你是公司的人資專員，請針對與以下主題相關的提問提供清楚、準確的解答：
            - 人事制度（如考勤、升遷、轉調等）
            - 請假規定（如病假、事假、特休等）
            - 員工福利（如保險、補助、活動等）

            如果問題不在你的職責範圍，請回覆「這部分建議您洽詢其他單位，我無法提供正確資訊」。
            回答完畢後，務必呼叫 end_task_with_summary 工具，將你的完整回覆作為摘要傳回。
        """
};

// IT 支援代理人（IT Support Agent）
// 職責：協助處理 IT 設備、帳號權限、VPN 等技術問題
// 可呼叫的 Plugin 函數：GetVpnSetup、GetAccountPolicy、GetBackupPolicy
// 回答完畢後須呼叫 end_task_with_summary 工具，將結果回傳給 orchestration
var itAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "ITSupportAgent",
    Description = "提供 IT 設備、VPN、帳號權限等技術支援服務",
    Instructions = """
        你是公司的 IT 支援人員，請協助處理以下類型的問題：
        - IT 設備（如電腦、印表機、網路）
        - 帳號與系統存取權限（如登入問題、權限申請）
        - VPN、遠端連線與系統備份設定

        請用清楚的步驟或指引協助提問者解決問題。若問題非 IT 支援職責，請委婉引導至相關單位。
        回答完畢後，務必呼叫 end_task_with_summary 工具，將你的完整回覆作為摘要傳回。
    """
};

// 合規代理人（Compliance Agent）
// 職責：回答與合約條款、資訊安全、公司治理相關的問題
// 可呼叫的 Plugin 函數：GetContractPolicy、GetDataSecurityPolicy、GetGovernancePolicy
// 回答完畢後須呼叫 end_task_with_summary 工具，將結果回傳給 orchestration
var complianceAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "ComplianceAgent",
    Description = "協助處理合規與資訊安全條例的詢問",
    Instructions = """
        你是法遵與資安顧問，請專注回答以下範圍的問題：
        - 公司合約條款與合規要求
        - 資訊安全政策（如存取控制、資料保護）
        - 公司治理規定與內部稽核事項

        若遇到與 HR 或 IT 技術相關的問題，請提醒使用者改由其他專責人員處理。
        回答完畢後，務必呼叫 end_task_with_summary 工具，將你的完整回覆作為摘要傳回。
    """
};

// ============================================================================
// 第五部分：設定 HandoffOrchestration（代理人轉交協調機制）
// ============================================================================
// HandoffOrchestration 是 Semantic Kernel 提供的代理人協調模式，特點：
//
// 1. 自動為每個代理人注入 HandoffPlugin，包含以下工具：
//    - transfer_to_HRPolicyAgent：將對話轉交給人資代理人
//    - transfer_to_ITSupportAgent：將對話轉交給 IT 代理人
//    - transfer_to_ComplianceAgent：將對話轉交給合規代理人
//    - end_task_with_summary：結束任務並將回覆結果回傳給主程式
//
// 2. OrchestrationHandoffs 定義轉交規則：
//    - StartWith(triageAgent)：所有使用者提問先由 TriageAgent 接收
//    - Add(source, target, description)：定義從 source 到 target 的轉交條件
//      description 會寫入 transfer 工具的說明，引導 AI 判斷何時該轉交
//
// 3. 建構函式的第二個參數（params Agent[]）列出所有參與協調的代理人
HandoffOrchestration orchestration =
    new(OrchestrationHandoffs
            .StartWith(triageAgent)
            .Add(triageAgent, hrAgent, "Transfer to this agent if the issue is about leave, salary, HR policy, or attendance.")
            .Add(triageAgent, itAgent, "Transfer to this agent if the issue is about IT, account, VPN, password, or device.")
            .Add(triageAgent, complianceAgent, "Transfer to this agent if the issue is about compliance, security, or internal company policy."),
                triageAgent, hrAgent, itAgent, complianceAgent);

// ============================================================================
// 第六部分：建立 InProcessRuntime（代理人執行環境）
// ============================================================================
// InProcessRuntime 提供代理人之間的訊息傳遞與排程機制
// 所有代理人在同一個行程（Process）內運行，透過記憶體中的訊息佇列通訊
// 需先呼叫 StartAsync() 啟動，使用完畢後呼叫 RunUntilIdleAsync() 等待收尾
InProcessRuntime runtime = new();

// ============================================================================
// 第七部分：使用者互動迴圈
// ============================================================================
// 顯示歡迎訊息和使用說明
Console.WriteLine("=== Semantic Kernel + Multi-Agent 示範 ===");
Console.WriteLine("這個應用程式會依據使用者提問類型，分派給不同的Agent負責回覆");
Console.WriteLine("這個應用程式可查詢公司內部的人資問題．法規問題與 IT 問題");
Console.WriteLine("試試看問「公司的特休怎麼算？」，「VPN 怎麼設？」以及問問「我剛問了哪些問題？」");
Console.WriteLine("輸入 'exit' 或 'quit' 離開程式");
Console.WriteLine("================================================\n");

// 主迴圈：持續接收使用者輸入，直到輸入 exit 或 quit
while (true)
{
    Console.Write("您: ");
    var input = Console.ReadLine();

    // 當使用者輸入空白、exit 或 quit 時結束程式
    if (string.IsNullOrWhiteSpace(input) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再見！");
        break;
    }

    // 步驟 1：啟動 Runtime，開始接收與處理代理人之間的訊息
    await runtime.StartAsync();

    // 步驟 2：將使用者輸入送入 HandoffOrchestration
    // InvokeAsync 會將訊息傳送給 StartWith 指定的 TriageAgent
    // 回傳 OrchestrationResult<string>，可用來非同步等待最終結果
    var result = await orchestration.InvokeAsync(input, runtime);

    // 步驟 3：等待最終結果（最多等待 300 秒）
    // 完整流程：TriageAgent → transfer_to_XXX → 專責 Agent → 呼叫 Plugin → end_task_with_summary
    // GetValueAsync 會阻塞直到某個代理人呼叫 end_task_with_summary 回傳結果
    // 注意：必須使用 await，否則 response 會是 Task<string> 物件而非字串
    Console.Write("AI > ");
    var response = await result.GetValueAsync(TimeSpan.FromSeconds(300));
    Console.Write(response);

    // 步驟 4：等待 Runtime 中所有待處理的訊息完成，確保代理人狀態一致
    await runtime.RunUntilIdleAsync();
    Console.WriteLine("\n\n");
}
