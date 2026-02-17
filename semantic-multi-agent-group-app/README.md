# Semantic Kernel Multi-Agent Group Chat 行銷辯論系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **GroupChatOrchestration** 群組討論機制的 .NET 應用程式，實現一個正反辯論式的多代理人行銷策略討論系統。

## 為什麼使用 GroupChatOrchestration？

### 問題：單一 Agent 的局限性

在行銷策略討論場景中，單一 Agent 往往只能從一個角度分析問題，容易產生偏見或思考盲點：

```csharp
// 單一 Agent - 只有一種觀點，缺乏多方辯論
ChatCompletionAgent agent = new()
{
    Name = "MarketingAgent",
    Instructions = "你要同時考慮降價的優缺點...",  // 難以同時扮演正反立場
    Kernel = kernel
};
```

### 解決方案：GroupChatOrchestration 群組辯論

GroupChatOrchestration 讓多個具有不同立場的 Agent 在群組中輪流發言，模擬真實會議中的多方觀點碰撞：

```csharp
// 多 Agent - 正反辯論 + 中立仲裁
GroupChatOrchestration orchestration =
    new(new RoundRobinGroupChatManager()
    {
        MaximumInvocationCount = 5  // 最多 5 輪發言
    },
    proAgent, conAgent, strategyAgent);
// 發言順序：ProAgent → ConAgent → StrategyAgent → ProAgent → ConAgent
```

## GroupChatOrchestration 的核心優勢

| 優勢 | 說明 |
|------|------|
| **多元觀點** | 每個 Agent 代表不同立場，確保議題被多角度分析 |
| **輪流發言** | RoundRobinGroupChatManager 依序讓每個 Agent 發表意見 |
| **上下文共享** | 後發言的 Agent 能看到前面所有人的發言，進行回應與反駁 |
| **輪數控制** | MaximumInvocationCount 控制討論深度，避免無限循環 |
| **即時觀察** | 透過 ResponseCallback 即時看到每位 Agent 的發言內容 |

## 與其他 Orchestration 的差異

| 項目 | multi-agent-handoff-app | multi-agent-sequential-app | multi-agent-group-app (本專案) |
|------|------------------------|---------------------------|-------------------------------|
| **協調模式** | HandoffOrchestration（條件轉交） | SequentialOrchestration（循序流水線） | GroupChatOrchestration（群組討論） |
| **執行順序** | 動態：由 AI 判斷轉交給誰 | 固定：依照建構時的 Agent 順序，單次 | 固定：輪流發言，可多輪 |
| **適用場景** | 問題分流、客服系統 | 文件審查、流水線作業 | 辯論、腦力激盪、多方討論 |
| **Agent 角色** | 1 分流 + N 專責 | N 個依序執行的 Agent | N 個平等討論的 Agent |
| **對話特性** | 單次轉交、回答即結束 | 單次流水線、意見累積 | 多輪對話、互相回應與反駁 |
| **Plugin 使用** | 有（查詢企業資料） | 無（純 LLM 審查） | 無（純 LLM 辯論） |
| **互動模式** | 多輪使用者對話 | 單次提交、一次性審查 | 單次主題、多輪 Agent 討論 |

## 三種 Orchestration 模式比較

| 模式 | 適用場景 | 執行方式 |
|------|---------|---------|
| **SequentialOrchestration** | 流水線作業（審查、翻譯校對） | A → B → C，固定順序，單次通過 |
| **HandoffOrchestration** | 問題分流（客服、工單系統） | A → 依條件轉交 B 或 C |
| **GroupChatOrchestration** | 多方討論（腦力激盪、辯論） | A、B、C 輪流發言，多輪討論 |

## 功能特性

- **GroupChatOrchestration**：多代理人群組討論機制
- **正反辯論**：正方與反方 Agent 針對同一議題提出不同觀點
- **中立仲裁**：策略顧問 Agent 綜合正反意見提出平衡建議
- **RoundRobin 輪流發言**：依序讓每位 Agent 發言，確保公平參與
- **ResponseCallback**：即時攔截並顯示每位 Agent 的回應內容
- **討論角色**：
  - ProAgent（正方）：從市場競爭、銷量提升角度支持降價
  - ConAgent（反方）：從品牌價值、利潤空間角度反對降價
  - StrategyAgent（中立）：綜合正反觀點，提出平衡方案

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
│                   提交行銷討論主題                           │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│            GroupChatOrchestration（群組聊天編排）             │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  ProAgent    │  │  ConAgent    │  │  StrategyAgent   │  │
│  │ (正方：降價  │  │ (反方：降價  │  │  (中立策略顧問)  │  │
│  │  支持者)     │  │  反對者)     │  │                  │  │
│  │              │  │              │  │                  │  │
│  │ 論述：       │  │ 論述：       │  │ 論述：           │  │
│  │ - 銷量提升   │  │ - 品牌損害   │  │ - 綜合正反觀點   │  │
│  │ - 市場搶佔   │  │ - 利潤下滑   │  │ - 提出平衡方案   │  │
│  │ - 客群擴展   │  │ - 價格認知   │  │ - 點出遺漏盲點   │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
│         ↑                                     ↓             │
│         └──── RoundRobin 輪流發言（最多5輪）───┘             │
│                                                             │
│  ResponseCallback：即時印出每位 Agent 的回應                 │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│              InProcessRuntime（代理人執行環境）               │
│              訊息傳遞與排程、記憶體內佇列通訊                │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    OpenAI GPT-4o API                        │
└─────────────────────────────────────────────────────────────┘
```

## 執行流程

```
使用者提交：行銷討論主題（U Smart Watch 限時降價促銷）
        │
        ▼
┌───────────────────────────────────────────┐
│  第1輪：ProAgent（正方）發言              │
│  從市場面論述降價帶來的好處與機會          │
│  ResponseCallback 即時印出回應            │
└───────────────────────────────────────────┘
        │ 回應內容加入群組對話上下文
        ▼
┌───────────────────────────────────────────┐
│  第2輪：ConAgent（反方）發言              │
│  看到正方觀點後，提出降價的風險與壞處      │
│  ResponseCallback 即時印出回應            │
└───────────────────────────────────────────┘
        │ 回應內容加入群組對話上下文
        ▼
┌───────────────────────────────────────────┐
│  第3輪：StrategyAgent（中立）發言         │
│  綜合正反雙方觀點，提出平衡建議           │
│  ResponseCallback 即時印出回應            │
└───────────────────────────────────────────┘
        │ 回應內容加入群組對話上下文
        ▼
┌───────────────────────────────────────────┐
│  第4輪：ProAgent（正方）再次發言          │
│  回應反方與策略顧問的觀點，補充論述        │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  第5輪：ConAgent（反方）再次發言          │
│  進一步反駁，深化討論                     │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  主程式透過 GetValueAsync() 取得最終結果   │
│  顯示完整的群組討論報告                    │
└───────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-multi-agent-group-app/
├── Program.cs                                              # 主程式（Agent 定義、Orchestration 設定、群組討論執行）
├── semantic-multi-agent-group-app.csproj                    # 專案設定
└── README.md                                               # 本文件
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-multi-agent-group-app
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Orchestration --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Agents.Runtime.Core --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Agents.Runtime.InProcess --version 1.70.0-preview
```

### 3. 設定環境變數

**macOS/Linux：**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

**Windows PowerShell：**
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
```

### 4. 抑制實驗性 API 警告

由於 `GroupChatOrchestration` 目前標記為實驗性 API（`SKEXP0110`），需在 `.csproj` 中加入：

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);SKEXP0110</NoWarn>
</PropertyGroup>
```

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 執行範例

```
Hello, Multi-Agent System!

【群組討論主題】
【產品資料】
產品名稱：U Smart Watch
產品定位：中高階運動健康智慧手錶，支援睡眠偵測、24小時心率監控，主打時尚設計。
售價：新台幣5,990元
...

========== 【ProAgent】 ==========
作為行銷專家，我從積極推動限時降價的角度來分析 U Smart Watch 的上市策略：

**一、產品屬性與生命週期**
U Smart Watch 正處於新品上市期（導入期），此階段最重要的目標是快速建立市場知名度和
用戶基礎。限時降價促銷能有效降低消費者的嘗試門檻...

**二、競爭對手的定價策略**
目前競品價格區間為 2,000~9,000 元，U Smart Watch 定價 5,990 元處於中間偏高位置。
若推出限時優惠價（例如首月 4,990 元），將更具價格競爭力...
================================================

========== 【ConAgent】 ==========
我從反對大幅度降價的角度來分析這個促銷方案：

**一、品牌價值損害**
過去618促銷的經驗已經給出警訊——雖然銷量提升30%，但次月明顯回落，且部分客戶
反映品牌價值認知下滑。新品上市即降價，等於向市場傳達「這個產品不值原價」的訊號...

**二、利潤空間壓縮**
作為市場第三的品牌，UGO 本身議價能力有限。降價直接壓縮利潤空間，
若競品跟進降價，將陷入價格戰的惡性循環...
================================================

========== 【StrategyAgent】 ==========
綜合正反兩方的觀點，我提出以下平衡建議：

**建議方案：「不降價，但增值」策略**
1. 維持原價 5,990 元，保護品牌定位
2. 首月推出「早鳥加值禮包」：購買即贈運動錶帶 + 延長保固一年
3. 搭配 KOL 聯名限定錶面，創造稀缺感而非廉價感...
================================================

...（繼續輪流討論）
```

## 核心程式碼說明

### 1. 建立具有不同立場的 ChatCompletionAgent

每個 Agent 透過 Instructions（系統提示詞）定義其立場與行為：

```csharp
// 正方 Agent - 積極論述降價的好處
var proAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ProAgent",
    Description = "降價支持者",
    Instructions =
        """
            你是一位行銷專家，專責從產品市場面積極推動限時降價的角度思考...
            請詳細說明降價促銷帶來的好處、正面效益與潛在機會。
        """
};

// 反方 Agent - 論述降價的風險與壞處
var conAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ConAgent",
    Description = "降價反對者",
    Instructions =
        """
            你是一位行銷專家，專責從產品市場面反對大幅度降價的角度思考...
            請詳細說明降價可能帶來的壞處、風險與品牌損害。
        """
};

// 中立策略 Agent - 綜合雙方觀點
var strategyAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "StrategyAgent",
    Description = "策略顧問",
    Instructions = "你是一位中立的策略顧問，請綜合正反兩方的觀點，提出平衡、創新的行銷方案..."
};
```

### 2. 建立 GroupChatOrchestration 與 ResponseCallback

```csharp
// RoundRobinGroupChatManager：依照傳入順序輪流發言
// MaximumInvocationCount：控制最多發言輪數
GroupChatOrchestration groupOrchestration =
    new(new RoundRobinGroupChatManager()
    {
        MaximumInvocationCount = 5
    },
    proAgent, conAgent, strategyAgent)
    {
        // ResponseCallback：即時攔截每位 Agent 的回應
        // 若不設定此回呼，中間的對話過程不會顯示，
        // 只有最終 GetValueAsync() 的結果可見
        ResponseCallback = (response) =>
        {
            Console.WriteLine($"\n========== 【{response.AuthorName}】 ==========");
            Console.WriteLine(response.Content);
            Console.WriteLine("================================================\n");
            return ValueTask.CompletedTask;
        }
    };
```

> **重要**：若未設定 `ResponseCallback`，三個 Agent 的逐輪辯論內容將不會輸出，只有 `GetValueAsync()` 回傳的最終結果（通常是最後一位 Agent 的回應）會被顯示。

### 3. 啟動 Runtime 並執行群組討論

```csharp
// 啟動 Runtime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// 啟動群組討論，將主題傳入
var result = await groupOrchestration.InvokeAsync(topic, runtime);

// 等待最終結果（最多等待 300 秒）
var finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(300));
Console.WriteLine(finalReport);

// 等待 Runtime 中所有待處理的訊息都處理完畢
await runtime.RunUntilIdleAsync();
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.70.0 | 核心框架 |
| Microsoft.SemanticKernel.Agents.Core | 1.70.0 | Agent 框架 |
| Microsoft.SemanticKernel.Agents.Orchestration | 1.70.0-preview | Orchestration 協調機制 |
| Microsoft.SemanticKernel.Agents.Runtime.Core | 1.70.0-preview | Agent Runtime 核心 |
| Microsoft.SemanticKernel.Agents.Runtime.InProcess | 1.70.0-preview | 行程內 Runtime 實作 |

## 關鍵概念說明

### GroupChatOrchestration

群組聊天編排器，負責管理多個 Agent 的對話流程。與 SequentialOrchestration 不同，GroupChatOrchestration 中的 Agent 是在同一個「對話室」中輪流發言，每位 Agent 都能看到之前所有人的發言內容，可以進行回應與反駁。

### RoundRobinGroupChatManager

輪流發言管理器，按照傳入的 Agent 順序依序發言。`MaximumInvocationCount` 控制最多發言輪數。以本專案為例，3 個 Agent 搭配 5 輪發言，發言順序為：ProAgent → ConAgent → StrategyAgent → ProAgent → ConAgent。

### ResponseCallback

回呼函式，用於即時攔截並顯示每位 Agent 的回應。這是 GroupChatOrchestration 中**最關鍵的設定**——若不設定此回呼，只有 `GetValueAsync()` 回傳的最終結果會被顯示，中間每位 Agent 的逐輪對話內容將不會輸出。

### InProcessRuntime

提供代理人之間的訊息傳遞與排程機制，所有代理人在同一個行程內運行，透過記憶體中的訊息佇列通訊。必須在 `InvokeAsync()` 之前呼叫 `StartAsync()` 啟動。

### SKEXP0110 警告

GroupChatOrchestration 相關的 API 目前標記為實驗性（Experimental），需在 `.csproj` 中加入 `<NoWarn>$(NoWarn);SKEXP0110</NoWarn>` 來抑制編譯警告。

## 常見問題

### Q: 為什麼看不到正反兩面的對話建議？

**A: 未設定 `ResponseCallback`。** `GetValueAsync()` 只回傳最終結果（通常是最後一位 Agent 的回應），中間的辯論過程需要透過 `ResponseCallback` 即時輸出。請確認 `GroupChatOrchestration` 的建構中有設定 `ResponseCallback` 屬性。

### Q: 可以改變發言順序嗎？

**A: 調整傳入 Agent 的順序即可。** `RoundRobinGroupChatManager` 按照建構時的 Agent 順序輪流發言。例如想讓反方先發言：

```csharp
// 改為反方先發言
GroupChatOrchestration orchestration =
    new(manager, conAgent, proAgent, strategyAgent);
```

### Q: 如何增加討論深度？

**A: 調整 `MaximumInvocationCount`。** 增加輪數可以讓 Agent 有更多機會互相回應與深化討論：

```csharp
new RoundRobinGroupChatManager()
{
    MaximumInvocationCount = 9  // 每位 Agent 各發言 3 輪
}
```

### Q: 可以新增更多討論角色嗎？

**A: 只需兩步：**
1. 建立新的 `ChatCompletionAgent`，定義其立場與角色
2. 將其加入 `GroupChatOrchestration` 的建構參數中

```csharp
// 例如新增消費者代言人 Agent
var consumerAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "ConsumerAgent",
    Description = "消費者代言人",
    Instructions = "你代表消費者的立場，從購買意願與價值感受的角度發表意見..."
};

// 加入群組討論
GroupChatOrchestration orchestration =
    new(manager, proAgent, conAgent, strategyAgent, consumerAgent);
```

### Q: 除了 RoundRobin，還有其他發言管理方式嗎？

**A: Semantic Kernel 也支援自訂 `GroupChatManager`。** 你可以實作自己的管理器來決定發言順序，例如根據 AI 判斷哪位 Agent 最適合接續發言。

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **Token 消耗較高**：群組討論模式中，每位 Agent 都需要接收之前所有人的發言作為上下文，隨著輪數增加，每次 API 呼叫的 Token 量會逐步增長
3. **成本考量**：GPT-4o 的 API 調用有費用，5 輪發言代表 5 次 API 呼叫
4. **輪數與品質**：輪數過多可能導致 Agent 重複論點，建議 3-9 輪為宜
5. **Preview 套件**：Orchestration 相關套件目前為預覽版本，API 可能會變更

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-kernel-multi-function-app** - 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent
6. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent
7. **semantic-azure-aiagent-app** - 學習 AzureAIAgent
8. **semantic-multi-agent-handoff-app** - Multi-Agent Handoff 協作
9. **semantic-multi-agent-sequential-app** - Multi-Agent Sequential 協作
10. **semantic-multi-agent-group-app** - Multi-Agent Group Chat 協作（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [Agent Orchestration 文檔](https://learn.microsoft.com/semantic-kernel/agents/agent-orchestration)
- [GroupChatOrchestration 文檔](https://learn.microsoft.com/semantic-kernel/frameworks/agent/agent-orchestration/group-chat)
- [Agent Orchestration Advanced Topics](https://learn.microsoft.com/semantic-kernel/frameworks/agent/agent-orchestration/advanced-topics)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
