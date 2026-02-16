// ============================================================================
// Semantic Kernel Multi-Agent Sequential Orchestration — 企業文件審查系統
// ============================================================================
// 本專案展示如何使用 Semantic Kernel 的 SequentialOrchestration（循序協作）模式，
// 建立一個多代理人（Multi-Agent）系統，模擬企業內部的文件審查流程。
//
// 【架構概覽】
//   使用者提交草案 → HRAgent（人資審查）→ ITAgent（資安審查）→ ComplianceAgent（法遵審查）→ 最終報告
//
// 【核心概念】
//   - ChatCompletionAgent：每個 Agent 都是一個獨立的 AI 角色，擁有自己的名稱、描述與指令（Prompt）
//   - SequentialOrchestration：將多個 Agent 串接成一條流水線，前一個 Agent 的輸出會自動作為下一個 Agent 的輸入
//   - InProcessRuntime：在同一個程序（Process）內執行所有 Agent，無需外部服務
//
// 【流程說明】
//   1. 使用者提交一份公司政策草案
//   2. HRAgent 先審查是否符合人資政策（個資、請假、打卡、歧視等）
//   3. ITAgent 接收 HRAgent 的審查結果，再加上資安觀點的審查意見
//   4. ComplianceAgent 接收前兩個 Agent 的累積結果，再加上法遵觀點的審查意見
//   5. 最終輸出包含所有部門的綜合審查報告
// ============================================================================

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
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
// 第三部分：定義各部門審查 Agent
// ============================================================================
// 每個 ChatCompletionAgent 代表一個獨立的 AI 審查角色，關鍵屬性：
//   - Kernel：共用同一個 Kernel 實例（共用模型連線設定）
//   - Name：Agent 的唯一識別名稱，用於在 Orchestration 中辨識
//   - Description：Agent 的簡短描述，供 Orchestration 或其他 Agent 參考
//   - Instructions：Agent 的系統提示詞（System Prompt），定義其行為準則與輸出格式
//
// 【重要設計】Sequential Orchestration 中，每個 Agent 的 Instructions 必須明確要求：
//   「保留前面部門的審查意見」，否則後續 Agent 可能會覆蓋先前的審查結果。
// ============================================================================

// --- Agent 1：人資部門審查員 (HRAgent) ---
// 職責：審查草案是否符合公司人資政策
// 順序：第一個執行，直接接收使用者的原始草案
var hrAgent = new ChatCompletionAgent(){
                Kernel = kernel,
                Name = "HRAgent",
                Description = "人資政策審查員",
                Instructions =
                    """
                        請審查使用者提交內容是否符合公司人資政策
                        - 不得出現員工個人資料
                        - 請假加班必須填單申請
                        - 上下班必須打卡
                        - 不得有性騷擾或性別歧視言論
                        若有疑慮請給出建議或補充意見，若是合規則請回覆「合規」。

                        ## 輸出格式：
                        [草案內容]
                        ...（草案）

                        [人資部門審查意見]
                        ...（內容)
                    """
            };

// --- Agent 2：IT / 資安部門審查員 (ITAgent) ---
// 職責：審查草案是否涉及資訊安全風險
// 順序：第二個執行，接收 HRAgent 的輸出（含草案 + 人資審查意見）
// 注意：Instructions 中特別要求「保留所有既有意見內容」，確保人資意見不會被覆蓋
var itAgent = new ChatCompletionAgent(){
            Kernel = kernel,
            Name = "ITAgent",
            Description = "資訊/技術審查員",
            Instructions =
                """
                    你將會收到一份公司政策草案以及前面部門的審查意見（若有）。請保留所有既有意見內容，再加上你本部門的審查意見，不可刪除任何意見。

                    請審查使用者提交內容是否涉及資訊安全
                    - 不得出現帳號、密碼、伺服器主機位置資訊
                    - 不得安裝私人軟體

                    若有疑慮請給出建議或補充意見，若是合規則請回覆「合規」。

                    ## 輸出格式：
                    [草案內容]
                    ...（草案）

                    [人資部門審查意見]
                    ...（內容)

                    [IT部門審查意見]
                    ...（內容）
                """
        };

// --- Agent 3：合規 / 法遵部門審查員 (ComplianceAgent) ---
// 職責：審查草案是否違反公司內部守則或相關法規
// 順序：第三個（最後一個）執行，接收前兩個 Agent 的累積輸出
// 其輸出即為最終的綜合審查報告
var complianceAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ComplianceAgent",
    Description = "法遵審查員",
    Instructions =
    """
    你將會收到一份公司政策草案以及前面部門的審查意見（若有）。請保留所有既有意見內容，再加上你本部門的審查意見，不可刪除任何意見。

    請審查使用者提交內容有無違反公司內部守則或法規
    - 不得出現客戶資料
    - 不得涉及商業機密洩漏
    - 不得出現仇恨言論或歧視性內容

    若有疑慮請給出建議或補充意見，若是合規則請回覆「合規」。

    ## 輸出格式：
    [草案內容]
    ...（草案）

    [人資部門審查意見]
    ...（內容）

    [IT部門審查意見]
    ...（內容）

    [合規部門審查意見]
    ...（內容）
    """
};

// ============================================================================
// 第四部分：建立 Sequential Orchestration（循序協作流程）
// ============================================================================
// SequentialOrchestration 是 Semantic Kernel 提供的多代理人協作模式之一，特性：
//   - 依照建構時傳入的 Agent 順序，依序執行每個 Agent
//   - 前一個 Agent 的完整輸出，會自動作為下一個 Agent 的輸入
//   - 適用於「流水線」式的工作流程（如：草稿 → 審查 → 覆核）
//
// 其他可用的協作模式包括：
//   - GroupChatOrchestration：多 Agent 群組對話，適合需要討論的場景
//   - HandoffOrchestration：Agent 之間可主動交接控制權，適合條件分支流程
SequentialOrchestration orchestration = new(hrAgent, itAgent, complianceAgent){
    Name = "DocumentReviewOrchestration",
    Description = "多代理人協同審查文件內容，確保符合人資政策、資訊安全與合規要求。"
};

// ============================================================================
// 第五部分：啟動 InProcessRuntime（執行環境）
// ============================================================================
// InProcessRuntime 是 Agent 的執行環境，負責：
//   - 在同一個 .NET 程序內管理所有 Agent 的生命週期
//   - 處理 Agent 之間的訊息傳遞與排程
//   - 無需額外的外部服務或基礎設施
//
// 【重要注意事項】
//   runtime.StartAsync() 必須在 orchestration.InvokeAsync() 之前呼叫，
//   Runtime 啟動後才能開始執行 Agent 任務。
InProcessRuntime runtime = new();
await runtime.StartAsync();

// ============================================================================
// 第六部分：準備待審查的草案內容
// ============================================================================
// 這裡模擬一份包含多項政策的公司草案，其中故意包含一條違規內容：
//   「女性同仁不得申請生理假」— 這違反性別平等相關法規，
//   預期 HRAgent 和 ComplianceAgent 都應該標記此問題。
string input =
    """
        ### 以下是使用者的具體提交內容

        公司新政策草案：
        自2024年起，所有員工可申請每月1天遠端工作日，
        加班工時上限每月40小時，須提前主管同意。
        女性同仁不得申請生理假。
        另外為了提升資安意識，所有員工必須每半年參加資安線上訓練一次。

        ### 請審查此草案是否符合人資政策、資訊安全與合規要求。
    """;


Console.WriteLine($"草案內容：\n{input}\n");
Console.WriteLine("開始進行文件審查協作流程...\n");

// ============================================================================
// 第七部分：執行審查流程與進度顯示
// ============================================================================
// orchestration.InvokeAsync() 會啟動整個 Sequential 流程：
//   1. 將 input 送給第一個 Agent（HRAgent）
//   2. HRAgent 完成後，將結果自動傳給 ITAgent
//   3. ITAgent 完成後，將結果自動傳給 ComplianceAgent
//   4. 回傳一個 OrchestrationResult，透過 GetValueAsync() 取得最終結果
//
// 由於整個流程可能耗時數十秒（每個 Agent 都需要呼叫 LLM），
// 我們在背景執行一個 Spinner 動畫，讓使用者知道程式仍在運作中。
var result = await orchestration.InvokeAsync(input, runtime);

// --- Spinner 動畫：提供視覺化的進度回饋 ---
// 使用 Braille 字元（⠋⠙⠹...）製造旋轉效果
// 透過 \r（Carriage Return）覆寫同一行，避免產生大量輸出
var spinnerChars = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
// 三個 Agent 的顯示名稱，用於推估目前進行到哪個階段
var agents = new[] { "人資部門(HRAgent)", "IT部門(ITAgent)", "合規部門(ComplianceAgent)" };
// CancellationTokenSource 用於在審查完成後通知 Spinner 停止
var cts = new CancellationTokenSource();
var spinnerTask = Task.Run(async () =>
{
    // 使用 Stopwatch 計算已經過的時間
    var elapsed = System.Diagnostics.Stopwatch.StartNew();
    int i = 0;
    while (!cts.Token.IsCancellationRequested)
    {
        // 根據經過時間推估目前處理到哪個 Agent（每 12 秒切換一個階段）
        // 注意：這只是「推估」，實際每個 Agent 的處理時間取決於 LLM 回應速度
        int agentIndex = Math.Min((int)(elapsed.Elapsed.TotalSeconds / 12), agents.Length - 1);
        // 使用 \r 將游標移回行首，覆寫上一次的輸出（不換行）
        Console.Write($"\r{spinnerChars[i % spinnerChars.Length]} 審查進行中... 預估階段：{agents[agentIndex]}（已耗時 {elapsed.Elapsed.TotalSeconds:F0} 秒）");
        i++;
        // 每 100ms 更新一次動畫，同時監聽取消訊號
        try { await Task.Delay(100, cts.Token); } catch (OperationCanceledException) { break; }
    }
    // 審查完成後，用空白覆蓋 Spinner 行，保持輸出整潔
    Console.Write("\r" + new string(' ', 80) + "\r");
}, cts.Token);

// ============================================================================
// 第八部分：取得最終審查結果
// ============================================================================
// GetValueAsync() 會阻塞等待直到整個 Sequential 流程完成
// TimeSpan.FromSeconds(3000) 設定最長等待時間為 3000 秒（50 分鐘）
// 回傳值為最後一個 Agent（ComplianceAgent）的完整輸出文字
string finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(3000));

// 通知 Spinner 停止並等待其結束
cts.Cancel();
await spinnerTask;

Console.WriteLine("✅ 審查完成：");
Console.WriteLine(finalReport);

// ============================================================================
// 第九部分：等待 Runtime 閒置後結束
// ============================================================================
// RunUntilIdleAsync() 確保所有排程中的 Agent 任務都已完成，
// 避免程式在 Agent 尚未完全結束時就退出，造成資源洩漏。
await runtime.RunUntilIdleAsync();
