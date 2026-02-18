// ============================================================================
// Semantic Kernel Multi-Agent Concurrent Orchestration — 企業合約多部門並行審查系統
// ============================================================================
// 本程式展示如何使用 Microsoft Semantic Kernel 的「Concurrent Orchestration（並行協作）」
// 模式，讓多個 AI Agent 同時（並行）處理同一份輸入資料，各自獨立產出審查意見。
//
// 【核心概念】
// - ConcurrentOrchestration：將同一份訊息同時發送給多個 Agent，
//   每個 Agent 獨立運作、互不干擾，最終匯整所有 Agent 的回覆結果。
//   適用於「多角度審查」「多專家平行評估」等場景。
// - 與 Sequential（循序）不同，Concurrent 模式下各 Agent 不會看到彼此的回覆，
//   因此各自的意見是獨立、不受其他 Agent 影響的。
//
// 【應用場景】
// 企業合約審查：一份合約草案同時交由法務、財務、資安三個專家 Agent 審查，
// 各自從專業角度提出意見，最終匯整為完整的審查報告。
//
// 【執行流程】
// 1. 建立 Semantic Kernel（連接 OpenAI GPT-4o 模型）
// 2. 定義三個專業 Agent（法務、財務、資安），各自有獨立的審查指引（Instructions）
// 3. 建立 ConcurrentOrchestration，將三個 Agent 註冊為並行處理單元
// 4. 啟動 InProcessRuntime（Agent 的本地執行環境）
// 5. 將合約草案送入 Orchestration，三個 Agent 同時開始審查
// 6. 等待所有 Agent 完成後，匯整並輸出各部門的審查意見
// ============================================================================

// 引入 Semantic Kernel 核心命名空間
using Microsoft.SemanticKernel;
// 引入 Agent 基礎類別（ChatCompletionAgent 等）
using Microsoft.SemanticKernel.Agents;
// 引入 Concurrent Orchestration（並行協作模式）
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
// 引入 InProcessRuntime（本地端 Agent 執行環境，不需要外部服務）
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

Console.WriteLine("Hello, Multi-Agent System! \n\n");

// ============================================================================
// 第一部分：環境設定與 API Key 驗證
// ============================================================================
// 從環境變數讀取 OpenAI API Key
// 這是安全的做法，避免將敏感資訊硬編碼在程式碼中
// 設定方式（終端機）：export OPENAI_API_KEY="sk-..."
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
//
// 注意：在 Concurrent 模式下，所有 Agent 共用同一個 Kernel 實例，
// 代表它們都使用相同的 AI 模型與設定。若需要不同 Agent 使用不同模型，
// 可為每個 Agent 建立獨立的 Kernel。
var kernel = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(
                        modelId: "gpt-4o",  // 使用 GPT-4o 模型，支援函數呼叫與串流
                        apiKey: apiKey)
                    .Build();

// ============================================================================
// 第三部分：定義專業 Agent
// ============================================================================
// 每個 ChatCompletionAgent 代表一個獨立的 AI 專家角色。
// 關鍵屬性說明：
// - Kernel：指定此 Agent 使用的 AI 核心（模型、Plugin 等）
// - Name：Agent 的唯一識別名稱（英文），用於系統內部識別與日誌追蹤
// - Description：Agent 的中文描述，方便開發者理解其角色
// - Instructions：Agent 的「系統提示詞（System Prompt）」，定義其專業行為與審查準則
//
// 在 ConcurrentOrchestration 中，每個 Agent 會獨立收到相同的使用者輸入，
// 並根據各自的 Instructions 產出獨立的回覆，彼此之間不會互相影響。

// ----- Agent 1：法務審查員 -----
// 負責從法律合規角度審查合約條款，包括權利義務、智財權、違約責任、
// 爭議解決機制等面向，最終評估整體法律風險等級。
var legalAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "LegalAgent",
    Description = "法務審查員",
    Instructions =
    """
    你是專業企業法務審查員，請根據下列重點對收到的合約內容進行逐項詳細審查，逐點列舉每一項條款之合規性、潛在法律風險，以及改善建議：

    1. 合約雙方名稱、身份、權利義務描述是否明確？
    2. 合約主體（標的）及服務內容是否清楚完整、無歧義？
    3. 履約期限、重要時程、交付/驗收條件是否具體明載？
    4. 價金與付款條件有無明確、是否約定幣別、支付方式、分期規範？
    5. 保密條款是否周延，包含資訊、技術、個資等範圍與存續期間？
    6. 智慧財產權歸屬、授權、使用限制是否明確，是否有爭議風險？
    7. 違約責任及損害賠償機制（定義、計算方式、範圍）是否合理？
    8. 不可抗力條款（如天災、疫情等）有無明定及其處理方式？
    9. 合約終止、解約之條件、程序及其權利義務分配是否合理？
    10. 爭議解決條款：管轄法院/仲裁機制、準據法條款是否明確？
    11. 合約是否提及相關法律規範（如公司法、民法、個資法等），有無抵觸？
    12. 其他特殊條款（如保證、保固、再委託、合約轉讓等）之合規性檢查。

    請針對每一項重點逐一檢查，指出合格/不合格/需補充之處，並列出潛在法律風險與建議修正內容。
    最後，請總結整體合約法律風險等級（低/中/高）及必須優先修正之重大條款。
    """
};

// ----- Agent 2：財務審查員 -----
// 負責從財務角度審查合約中的付款條件、金額正確性、隱藏成本、
// 違約賠償計算、匯率風險等，確保合約不會對公司造成財務損失。
var financeAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "FinanceAgent",
    Description = "財務審查員",
    Instructions =
        """
    你是企業財務審查專家，請依下列重點細查合約內容：

    1. 付款條件（付款時間、分期方式、預付款與尾款設計）是否明確合理？
    2. 金額、貨幣單位及總額是否正確，與預算/報價是否一致？
    3. 有無約定逾期付款之罰則或利息？
    4. 退費、折讓、違約賠償之計算方式是否明確？
    5. 有無未明定的附加費用、隱藏成本？
    6. 若合約涉外，是否考慮匯率波動、國際稅法、境外帳戶等風險？
    7. 付款憑證（如發票、收據）取得及存查方式有無具體規定？
    8. 合約終止、變更、解約條款對財務影響？
    9. 有無雙方授信額度/付款擔保機制（如保證金、擔保函等）？
    10. 其他涉及重大財務風險或不利於本公司條款。

    請針對上述審查點，逐一檢查並列出建議與風險。
    """
};

// ----- Agent 3：資訊安全審查員 -----
// 負責從資安角度審查合約中的資料保護措施、加密要求、存取權限控制、
// 資安事故處理機制、個資法/GDPR 合規性等，確保合約符合資安標準。
var infosecAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "InfoSecAgent",
    Description = "資訊安全審查員",
    Instructions =
    """
    你是企業資安審查專家，請根據以下清單，針對收到的合約內容進行逐項審查，務必指出具體條文對應的資安疑慮與建議改善方式。

    請重點審查下列面向（如未提及，請標註風險）：
    1. 是否明確規範雙方的資料存取權限、責任歸屬？
    2. 是否要求數據傳輸與儲存全程加密？（如AES、TLS等）
    3. 系統登入、管理、維護的認證與權限設計是否充分？
    4. 合約有無明定資訊安全事故的通報機制與責任劃分？
    5. 是否遵循個資法、GDPR 或相關法令要求？
    6. 第三方（如供應商、分包）存取權限及風險管理條款是否明確？
    7. 合約期間結束或終止時，資料移轉、刪除等交接程序是否完備？
    8. 有無規範定期安全稽核、弱點掃描或滲透測試之義務？
    9. 有無明確懲處/賠償條款，若一方造成資安事故損失？
    10. 是否有明訂系統異動/維護必須經過雙方書面同意的規範？

    請依上列審查點，針對本合約逐條點評（如「合格/有缺漏/風險高」），並提出具體改善建議。
    最後給出整體資訊安全風險總結評語。
    """
};

// ============================================================================
// 第四部分：建立 Concurrent Orchestration（並行協作編排）
// ============================================================================
// ConcurrentOrchestration 是 Semantic Kernel 提供的並行協作模式：
// - 將同一份輸入（合約內容）同時分發給所有註冊的 Agent
// - 每個 Agent 獨立、平行地處理輸入，互不干擾
// - 所有 Agent 完成後，Orchestration 會匯整所有回覆結果
//
// 與其他協作模式的比較：
// - Sequential（循序）：Agent 依序執行，前一個的輸出作為下一個的輸入
// - Handoff（交接）：Agent 之間可以動態轉交任務
// - GroupChat（群聊）：Agent 之間可以互相對話討論
// - Concurrent（並行）：所有 Agent 同時處理相同輸入，各自獨立回覆 ← 本範例使用
ConcurrentOrchestration orchestration =
            new(legalAgent, financeAgent, infosecAgent);

// ============================================================================
// 第五部分：建立並啟動 Runtime（Agent 執行環境）
// ============================================================================
// InProcessRuntime 是本地端的 Agent 執行環境：
// - 所有 Agent 在同一個程序（Process）內執行，不需要外部服務或網路連接
// - 負責管理 Agent 的生命週期、訊息傳遞、排程與執行
// - StartAsync() 啟動 Runtime，開始接受並處理 Agent 任務
// - 適合開發測試階段使用；正式環境可替換為分散式 Runtime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// ============================================================================
// 第六部分：準備合約內容（模擬輸入）
// ============================================================================
// 這是一份刻意包含多項問題的合約草案，用於測試各 Agent 的審查能力：
// - 「30日內支付首期款項」：付款條件不夠具體（起算日？金額？）
// - 「個資可以不加密或不去識別化處理」：明顯違反資安與個資法規
// - 「不經通知即可逕自終止合作」：單方面終止權，法律上有爭議
// - 缺少保密條款、智財權歸屬、不可抗力等重要條款
string contract =
    """
        本合約規範甲乙雙方於2025年資訊系統採購合作事宜。
        甲方將於30日內支付首期款項，其餘分三期付款。
        雙方須遵守資料保護法，所有系統存取需經加密認證。
        但為求效率，系統個資可以不加密或不去識別化處理。
        合約期間若乙方未達KPI，甲方有權不經通知即可逕自終止合作。
        本案如有爭議，雙方同意於台北地方法院處理。
    """;

// ============================================================================
// 第七部分：執行並行審查與輸出結果
// ============================================================================

// 輸出合約草案內容，方便對照審查結果
Console.WriteLine($"【合約草案】\n{contract}\n");

// InvokeAsync()：將合約內容送入 ConcurrentOrchestration
// - 系統會將 contract 同時發送給 legalAgent、financeAgent、infosecAgent
// - 三個 Agent 平行處理，各自呼叫 GPT-4o 產生審查意見
// - 回傳 OrchestrationResult 物件，用於後續取得各 Agent 的回覆
var result = await orchestration.InvokeAsync(contract, runtime);

// GetValueAsync()：等待所有 Agent 完成並取得匯整結果
// - TimeSpan.FromSeconds(300)：設定最長等待時間為 300 秒（5 分鐘）
// - 回傳 IList<string>，每個元素對應一個 Agent 的完整回覆
// - 回覆順序與建立 ConcurrentOrchestration 時傳入的 Agent 順序一致
var finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(300));

// 輸出所有 Agent 的審查意見
// 使用 string.Join 將各 Agent 的回覆以換行分隔，依序顯示
Console.WriteLine("【各部門審查意見】：\n");
Console.WriteLine($"{string.Join("\n\n", finalReport.Select(text => $"{text}"))}");

// RunUntilIdleAsync()：等待 Runtime 中所有待處理任務完成
// - 確保所有背景工作都已結束後才關閉程式
// - 這是良好的資源清理實踐，避免任務被意外中斷
await runtime.RunUntilIdleAsync();
