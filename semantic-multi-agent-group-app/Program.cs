// ============================================================================
// Semantic Kernel Multi-Agent Group Chat 範例
// ============================================================================
// 本範例展示如何使用 Semantic Kernel 的 GroupChatOrchestration 建立
// 「多代理群組辯論」系統。三個 Agent 分別扮演正方、反方與中立策略顧問，
// 針對同一個行銷議題進行多輪討論，模擬真實會議中的多方觀點碰撞。
//
// 架構說明：
//   ┌──────────────────────────────────────────────┐
//   │         GroupChatOrchestration                │
//   │  ┌────────┐  ┌────────┐  ┌──────────────┐   │
//   │  │ProAgent│→ │ConAgent│→ │StrategyAgent │   │
//   │  │(正方)  │  │(反方)  │  │(中立策略顧問)│   │
//   │  └────────┘  └────────┘  └──────────────┘   │
//   │         ↑ RoundRobin 輪流發言 ↓              │
//   └──────────────────────────────────────────────┘
//
// 發言順序（RoundRobin，最多 5 輪）：
//   第1輪: ProAgent → 第2輪: ConAgent → 第3輪: StrategyAgent
//   → 第4輪: ProAgent → 第5輪: ConAgent
//
// 重點觀念：
// - ChatCompletionAgent：每個 Agent 都是獨立的聊天完成代理，擁有各自的
//   Name、Description 與 Instructions（系統提示詞），決定其立場與行為。
// - GroupChatOrchestration：群組聊天編排器，負責管理多個 Agent 的對話流程。
// - RoundRobinGroupChatManager：輪流發言管理器，依序讓每個 Agent 發表意見。
// - ResponseCallback：回呼函式，用於即時攔截並顯示每位 Agent 的回應。
//   若不設定此回呼，只有 GetValueAsync() 回傳的最終結果會被顯示，
//   中間每位 Agent 的逐輪對話內容將不會輸出。
// - InProcessRuntime：本機執行環境，負責驅動 Agent 之間的訊息傳遞。
// ============================================================================

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
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
// 第三部分：定義三個具有不同立場的 Agent
// ============================================================================
// 每個 ChatCompletionAgent 都有獨立的角色設定：
// - Kernel：共用同一個 Semantic Kernel 實例（共享同一個 LLM 連線）
// - Name：Agent 的識別名稱，會出現在 ResponseCallback 的 AuthorName 中
// - Description：Agent 的簡短描述，供編排器了解其角色定位
// - Instructions：系統提示詞（System Prompt），決定 Agent 的行為與立場

// 正方 Agent（降價支持者）
// 角色：從市場競爭、銷量提升、客群擴展等正面角度，積極論述降價的好處
var proAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ProAgent",
    Description = "降價支持者",
    Instructions =
        """
            你是一位行銷專家，專責從產品市場面積極推動限時降價的角度思考，根據以下幾個關鍵因素進行評估與決策
            - 產品屬性與生命週期
            - 競爭對手的定價策略
            - 目標市場與消費者客群

            請詳細說明降價促銷帶來的好處、正面效益與潛在機會。
        """
};

// 反方 Agent（降價反對者）
// 角色：從品牌價值、利潤空間、長期定位等角度，論述降價的風險與壞處
var conAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ConAgent",
    Description = "降價反對者",
    Instructions =
        """
            你是一位行銷專家，專責從產品市場面反對大幅度降價的角度思考，根據以下幾個關鍵因素進行評估與決策
            - 產品屬性與生命週期
            - 競爭對手的定價策略
            - 目標市場與消費者客群

            請詳細說明降價可能帶來的壞處、風險與品牌損害。
        """
};

// 中立策略 Agent（策略顧問）
// 角色：綜合正反雙方觀點，提出平衡的建議方案，並點出雙方可能忽略的盲點
var strategyAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "StrategyAgent",
    Description = "策略顧問",
    Instructions = @"你是一位中立的策略顧問，請綜合正反兩方的觀點，提出平衡、創新的行銷方案，或提醒團隊注意雙方可能忽略的重點。"
};

// ============================================================================
// 第四部分：建立 GroupChatOrchestration（群組聊天編排）
// ============================================================================
// GroupChatOrchestration 是多代理群組對話的核心編排器，負責：
// 1. 管理 Agent 的發言順序（透過 GroupChatManager）
// 2. 將每位 Agent 的回應傳遞給下一位 Agent 作為上下文
// 3. 控制討論的總輪數
//
// RoundRobinGroupChatManager：輪流發言管理器
// - 按照傳入的 Agent 順序依序發言：ProAgent → ConAgent → StrategyAgent → ...
// - MaximumInvocationCount = 5：最多進行 5 輪發言後結束討論
//
// ResponseCallback（關鍵設定）：
// - 每當一個 Agent 產生回應時，此回呼函式會被呼叫
// - 若不設定，中間的對話過程不會顯示，只有最終 GetValueAsync() 的結果可見
// - response.AuthorName：回應的 Agent 名稱（如 ProAgent、ConAgent）
// - response.Content：該 Agent 的回應內容
GroupChatOrchestration groupOrchestration =
    new(new RoundRobinGroupChatManager()
    {
        MaximumInvocationCount = 5
    },
    proAgent, conAgent, strategyAgent)
    {
        // ResponseCallback：即時印出每位 Agent 的回應，讓使用者看到完整的辯論過程
        ResponseCallback = (response) =>
        {
            Console.WriteLine($"\n========== 【{response.AuthorName}】 ==========");
            Console.WriteLine(response.Content);
            Console.WriteLine("================================================\n");
            return ValueTask.CompletedTask;
        }
    };

// ============================================================================
// 第五部分：建立 InProcessRuntime 並啟動
// ============================================================================
// InProcessRuntime 是本機記憶體內的執行環境，負責：
// - 驅動 Agent 之間的訊息傳遞與排程
// - 管理 Agent 的生命週期
// - 必須先呼叫 StartAsync() 啟動後，才能執行編排
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// ============================================================================
// 第六部分：定義討論主題並啟動群組討論
// ============================================================================
// 將完整的產品背景資料作為討論主題傳入，
// 三個 Agent 會依序針對此主題發表各自立場的意見
string topic =
    """
        【產品資料】
        產品名稱：U Smart Watch
        產品定位：中高階運動健康智慧手錶，支援睡眠偵測、24小時心率監控，主打時尚設計。
        售價：新台幣5,990元

        【公司背景】
        品牌：UGO，專注於年輕消費者運動穿戴
        品牌調性：活力、創新、時尚
        市佔：約台灣市場第三

        【目標客群（TA）】
        18-35歲運動族群，重視外型與功能兼具、願意為品牌買單、偏好線上購物。

        【市場與競品】
        主要競品：Apple Watch SE、小米手環Pro、Garmin Venu Sq
        競品價格區間：2,000~9,000元

        【歷史促銷】
        過去618節日曾嘗試全站9折，銷量提升30%，但次月回落，部分客戶反映對品牌價值認知下滑。

        【本次行銷討論主題】
        考慮在U Smart Watch上市首月推動『限時新品上市降價促銷』，請各方針對上述背景資料進行具體討論與建議。
    """;

Console.WriteLine($"【群組討論主題】\n{topic}\n");

// InvokeAsync()：啟動群組聊天編排
// - 將 topic 作為初始訊息傳入，觸發第一個 Agent（ProAgent）開始發言
// - 每位 Agent 回應後，ResponseCallback 會即時輸出其內容
// - 回傳 OrchestrationResult，可透過 GetValueAsync() 取得最終結果
var result = await groupOrchestration.InvokeAsync(topic, runtime);

// ============================================================================
// 第七部分：取得最終結果並等待執行完成
// ============================================================================
// GetValueAsync()：等待整個群組討論完成，取得最終彙整結果
// - TimeSpan.FromSeconds(300)：最多等待 300 秒（5 分鐘）
// - 回傳值通常是最後一位發言 Agent 的回應內容
// - 注意：中間的逐輪對話已透過 ResponseCallback 即時輸出
var finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(300));
Console.WriteLine(finalReport);

// RunUntilIdleAsync()：等待 Runtime 中所有待處理的訊息都處理完畢
// 確保所有 Agent 的回應都已完成，避免程式提前結束導致輸出不完整
await runtime.RunUntilIdleAsync();
