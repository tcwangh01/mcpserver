# Semantic Kernel Multi-Agent Concurrent Orchestration 企業合約並行審查系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **ConcurrentOrchestration** 並行協作機制的 .NET 應用程式，實現一個多部門同時審查企業合約的 AI 審查系統。

## 為什麼使用 ConcurrentOrchestration？

### 問題：循序審查的效率瓶頸

在企業合約審查場景中，一份合約往往需要經過法務、財務、資安等多個部門審查。若使用 SequentialOrchestration 依序處理，每個部門必須等前一個部門完成後才能開始，耗時較長：

```csharp
// Sequential 模式 - 必須依序等待，總耗時 = Agent1 + Agent2 + Agent3
SequentialOrchestration orchestration = new(legalAgent, financeAgent, infosecAgent);
// 法務審完 → 財務才開始 → 財務審完 → 資安才開始
```

### 解決方案：ConcurrentOrchestration 並行協作

ConcurrentOrchestration 將同一份合約同時發送給所有 Agent，各自獨立、平行處理，大幅縮短總審查時間：

```csharp
// Concurrent 模式 - 同時審查，總耗時 ≈ 最慢的那個 Agent
ConcurrentOrchestration orchestration = new(legalAgent, financeAgent, infosecAgent);
// 法務、財務、資安三者同時開始，同時審查，各自獨立產出意見
```

## ConcurrentOrchestration 的核心優勢

| 優勢 | 說明 |
|------|------|
| **平行處理** | 所有 Agent 同時執行，總耗時等於最慢的 Agent，而非所有 Agent 的總和 |
| **獨立意見** | 每個 Agent 獨立審查，不會受其他 Agent 的意見影響，確保觀點客觀 |
| **職責分離** | 每個 Agent 專注於特定領域，Instructions 簡潔明確 |
| **可擴展性** | 新增審查部門只需新增 Agent 並加入 Orchestration，不影響其他 Agent |
| **零配置** | 不需設定轉交規則或發言順序，所有 Agent 自動並行處理 |

## 與其他 Orchestration 的差異

| 項目 | multi-agent-sequential-app | multi-agent-handoff-app | multi-agent-group-app | multi-agent-concurrent-app (本專案) |
|------|---------------------------|------------------------|----------------------|-----------------------------------|
| **協調模式** | SequentialOrchestration（循序流水線） | HandoffOrchestration（條件轉交） | GroupChatOrchestration（群組討論） | ConcurrentOrchestration（並行處理） |
| **執行順序** | 固定：依照 Agent 順序，單次通過 | 動態：由 AI 判斷轉交給誰 | 固定：輪流發言，可多輪 | 同時：所有 Agent 平行執行 |
| **適用場景** | 文件審查、流水線作業 | 問題分流、客服系統 | 辯論、腦力激盪、多方討論 | 多角度審查、平行評估 |
| **Agent 角色** | N 個依序執行的 Agent | 1 分流 + N 專責 | N 個平等討論的 Agent | N 個獨立平行的 Agent |
| **對話特性** | 前一個輸出作為下一個輸入 | 單次轉交、回答即結束 | 多輪對話、互相回應與反駁 | 各自獨立處理、互不干擾 |
| **結果形式** | 累積式（最終包含所有意見） | 單一 Agent 回答 | 最後一輪 Agent 回應 | 多份獨立報告（IList\<string\>） |

## 四種 Orchestration 模式比較

| 模式 | 適用場景 | 執行方式 |
|------|---------|---------|
| **SequentialOrchestration** | 流水線作業（審查、翻譯校對） | A → B → C，固定順序，單次通過 |
| **HandoffOrchestration** | 問題分流（客服、工單系統） | A → 依條件轉交 B 或 C |
| **GroupChatOrchestration** | 多方討論（腦力激盪、辯論） | A、B、C 輪流發言，多輪討論 |
| **ConcurrentOrchestration** | 多角度審查（合約、風險評估） | A、B、C 同時處理，各自獨立回覆 |

## 功能特性

- **ConcurrentOrchestration**：多代理人並行協作機制
- **同步審查**：合約草案同時交由三個專業部門審查
- **獨立意見**：各 Agent 互不干擾，確保審查意見的客觀性
- **審查部門**：
  - 法務審查員（合約條款合規性、權利義務、違約責任、智財權）
  - 財務審查員（付款條件、金額正確性、隱藏成本、財務風險）
  - 資訊安全審查員（資料加密、存取權限、個資法/GDPR、資安事故處理）

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
│                   提交合約草案內容                            │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│            ConcurrentOrchestration（並行協作編排）            │
│                                                             │
│     同一份合約同時發送給三個 Agent，各自獨立處理              │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  LegalAgent  │  │ FinanceAgent │  │  InfoSecAgent    │  │
│  │ (法務審查員)  │  │ (財務審查員)  │  │ (資安審查員)     │  │
│  │              │  │              │  │                  │  │
│  │ 審查項目：    │  │ 審查項目：    │  │ 審查項目：       │  │
│  │ - 權利義務   │  │ - 付款條件   │  │ - 資料加密       │  │
│  │ - 智財權歸屬 │  │ - 金額正確性 │  │ - 存取權限       │  │
│  │ - 違約責任   │  │ - 隱藏成本   │  │ - 個資法合規     │  │
│  │ - 爭議解決   │  │ - 匯率風險   │  │ - 資安事故處理   │  │
│  │ - 不可抗力   │  │ - 付款擔保   │  │ - 安全稽核       │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         ↓                ↓                  ↓               │
│    [法務意見]        [財務意見]          [資安意見]           │
│         └────────────────┼──────────────────┘               │
│                          ↓                                  │
│              IList<string> 匯整結果                          │
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
使用者提交：企業合約草案
        │
        ▼
┌───────────────────────────────────────────┐
│  ConcurrentOrchestration 接收合約內容      │
│  同時將合約分發給三個 Agent                │
└───────────────────────────────────────────┘
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  LegalAgent  │ │ FinanceAgent │ │ InfoSecAgent  │
│  獨立審查    │ │  獨立審查    │ │  獨立審查     │
│  法律合規    │ │  財務風險    │ │  資安合規     │
│  12 項審查點 │ │  10 項審查點 │ │  10 項審查點  │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘
       │                │                │
       ▼                ▼                ▼
┌───────────────────────────────────────────┐
│  所有 Agent 完成後，GetValueAsync() 匯整   │
│  回傳 IList<string>，每個元素對應一個 Agent│
│  的完整審查報告                            │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  主程式輸出所有部門的審查意見              │
│  以換行分隔顯示三份獨立報告               │
└───────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-multi-agent-concurrent-app/
├── Program.cs                                              # 主程式（Agent 定義、Orchestration 設定、並行審查執行）
├── semantic-multi-agent-concurrent-app.csproj               # 專案設定
└── README.md                                               # 本文件
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-multi-agent-concurrent-app
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

由於 `ConcurrentOrchestration` 目前標記為實驗性 API（`SKEXP0110`），需在 `.csproj` 中加入：

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

【合約草案】
    本合約規範甲乙雙方於2025年資訊系統採購合作事宜。
    甲方將於30日內支付首期款項，其餘分三期付款。
    雙方須遵守資料保護法，所有系統存取需經加密認證。
    但為求效率，系統個資可以不加密或不去識別化處理。
    合約期間若乙方未達KPI，甲方有權不經通知即可逕自終止合作。
    本案如有爭議，雙方同意於台北地方法院處理。

【各部門審查意見】：

[法務審查意見]
1. 合約雙方名稱、身份：❌ 不合格 — 僅以「甲方」「乙方」稱呼，未載明公司全名、統一編號、代表人。
2. 合約主體及服務內容：❌ 不合格 — 「資訊系統採購合作」描述過於籠統，未明確系統規格、範圍。
3. 履約期限：⚠️ 需補充 — 僅提及「2025年」，未載明起訖日期、交付時程、驗收標準。
4. 價金與付款條件：⚠️ 需補充 — 未載明具體金額、幣別、首期款比例。
5. 保密條款：❌ 不合格 — 合約未提及保密義務。
...
整體法律風險等級：🔴 高

[財務審查意見]
1. 付款條件：⚠️ 需補充 — 「30日內支付首期款項」起算日不明確，各期金額與比例未載。
2. 金額與貨幣單位：❌ 不合格 — 合約未載明具體金額與幣別。
3. 逾期付款罰則：❌ 不合格 — 未約定逾期利息或罰則。
4. 違約賠償：⚠️ 需補充 — 甲方可單方面終止但未明定賠償計算方式。
...

[資安審查意見]
1. 資料存取權限：⚠️ 有缺漏 — 未明確規範雙方各自的資料存取範圍與責任歸屬。
2. 數據加密：❌ 風險高 — 合約明文允許「個資可以不加密或不去識別化處理」，嚴重違反個資法。
3. 認證與權限設計：⚠️ 有缺漏 — 僅提及「加密認證」，缺乏多因子認證、最小權限原則等規範。
4. 資安事故通報：❌ 風險高 — 未明定資安事件發生時的通報機制、時限與責任劃分。
...
整體資訊安全風險：🔴 高風險
```

## 核心程式碼說明

### 1. 建立共用 Kernel

所有 Agent 共用同一個 Kernel，代表它們使用相同的 AI 模型：

```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();
```

### 2. 建立多個專業 ChatCompletionAgent

每個 Agent 擁有獨立的審查指引（Instructions），定義其專業行為：

```csharp
// 法務審查員 - 從法律角度審查合約
var legalAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "LegalAgent",
    Description = "法務審查員",
    Instructions = """
        你是專業企業法務審查員，請根據下列重點對收到的合約內容進行逐項詳細審查...
        1. 合約雙方名稱、身份、權利義務描述是否明確？
        2. 合約主體（標的）及服務內容是否清楚完整、無歧義？
        ...
    """
};

// 財務審查員 - 從財務角度審查合約
var financeAgent = new ChatCompletionAgent() { ... };

// 資安審查員 - 從資安角度審查合約
var infosecAgent = new ChatCompletionAgent() { ... };
```

### 3. 建立 ConcurrentOrchestration

只需將所有 Agent 傳入建構函式，即可建立並行協作：

```csharp
// 三個 Agent 會同時收到相同的輸入，各自獨立處理
ConcurrentOrchestration orchestration =
    new(legalAgent, financeAgent, infosecAgent);
```

### 4. 啟動 Runtime 並執行並行審查

```csharp
// 建立並啟動 InProcessRuntime
var runtime = new InProcessRuntime();
await runtime.StartAsync();

// 將合約內容送入 Orchestration，三個 Agent 同時開始審查
var result = await orchestration.InvokeAsync(contract, runtime);

// 等待所有 Agent 完成並取得匯整結果（最多等待 300 秒）
// 回傳 IList<string>，每個元素對應一個 Agent 的完整回覆
var finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(300));

// 輸出所有 Agent 的審查意見
Console.WriteLine($"{string.Join("\n\n", finalReport.Select(text => $"{text}"))}");

// 等待 Runtime 中所有待處理任務完成
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

### ConcurrentOrchestration

並行協作編排器，將同一份輸入同時分發給所有註冊的 Agent。每個 Agent 獨立、平行地處理輸入，互不干擾，最終匯整所有 Agent 的回覆結果。與 SequentialOrchestration 不同，Concurrent 模式下各 Agent 不會看到彼此的回覆，因此各自的意見是獨立、不受其他 Agent 影響的。

### GetValueAsync() 的回傳值

`GetValueAsync()` 回傳 `IList<string>`（而非單一 `string`），每個元素對應一個 Agent 的完整回覆。回覆順序與建立 `ConcurrentOrchestration` 時傳入的 Agent 順序一致。

### InProcessRuntime

提供代理人之間的訊息傳遞與排程機制，所有代理人在同一個行程內運行，透過記憶體中的訊息佇列通訊。必須在 `InvokeAsync()` 之前呼叫 `StartAsync()` 啟動。

### SKEXP0110 警告

ConcurrentOrchestration 相關的 API 目前標記為實驗性（Experimental），需在 `.csproj` 中加入 `<NoWarn>$(NoWarn);SKEXP0110</NoWarn>` 來抑制編譯警告。

## 常見問題

### Q: ConcurrentOrchestration 和 SequentialOrchestration 該如何選擇？

**A: 取決於 Agent 之間是否需要看到彼此的輸出。**

- **需要意見累積**（後面的 Agent 需要參考前面的意見）→ 用 **Sequential**
- **各自獨立審查**（每個 Agent 不需要看其他人的意見）→ 用 **Concurrent**

例如：合約審查中，法務、財務、資安各自的專業領域不同，不需要互相參考，適合用 Concurrent。翻譯校對中，校對員需要看到翻譯員的結果才能校正，適合用 Sequential。

### Q: GetValueAsync() 回傳的順序是什麼？

**A: 與建立 ConcurrentOrchestration 時傳入的 Agent 順序一致。** 本專案中，`new(legalAgent, financeAgent, infosecAgent)` 代表 `finalReport[0]` 是法務意見、`finalReport[1]` 是財務意見、`finalReport[2]` 是資安意見。

### Q: 如何新增一個審查部門？

**A: 只需兩步：**
1. 建立新的 `ChatCompletionAgent`，定義該部門的審查指令
2. 將其加入 `ConcurrentOrchestration` 的建構參數中

```csharp
// 例如新增人資審查員
var hrAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "HRAgent",
    Description = "人資審查員",
    Instructions = "請從人資法規角度審查合約中的勞動條件、工時、福利等條款..."
};

// 加入並行審查
ConcurrentOrchestration orchestration =
    new(legalAgent, financeAgent, infosecAgent, hrAgent);
```

### Q: 可以讓不同 Agent 使用不同的 AI 模型嗎？

**A: 可以。** 為每個 Agent 建立獨立的 Kernel 即可：

```csharp
// 法務用 GPT-4o（需要較強的推理能力）
var legalKernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();

// 財務用 GPT-4o-mini（成本較低）
var financeKernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o-mini", apiKey: apiKey)
    .Build();

var legalAgent = new ChatCompletionAgent() { Kernel = legalKernel, ... };
var financeAgent = new ChatCompletionAgent() { Kernel = financeKernel, ... };
```

### Q: 審查過程中 console 一直靜止怎麼辦？

**A: Concurrent 模式下，所有 Agent 必須全部完成後 `GetValueAsync()` 才會回傳。** 三個 Agent 同時呼叫 LLM 各需要 10-30 秒，合計等待時間約 10-30 秒（取決於最慢的 Agent）。請確認網路連線正常、OpenAI API Key 有效。

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **同時 API 呼叫**：Concurrent 模式會同時發出多個 API 請求，請確認 OpenAI 帳號的 Rate Limit 足夠
3. **成本考量**：GPT-4o 的 API 調用有費用，3 個 Agent 同時執行代表 3 次 API 呼叫
4. **無中間輸出**：與 GroupChatOrchestration 的 ResponseCallback 不同，ConcurrentOrchestration 不提供即時回呼，必須等所有 Agent 完成後才能取得結果
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
10. **semantic-multi-agent-group-app** - Multi-Agent Group Chat 協作
11. **semantic-multi-agent-concurrent-app** - Multi-Agent Concurrent 協作（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [Agent Orchestration 文檔](https://learn.microsoft.com/semantic-kernel/agents/agent-orchestration)
- [ConcurrentOrchestration 範例](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithAgents)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
