# Semantic Kernel Multi-Agent Sequential Orchestration 企業文件審查系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **SequentialOrchestration** 循序協作機制的 .NET 應用程式，實現一個多部門依序審查企業文件的 AI 審查系統。

## 為什麼使用 SequentialOrchestration？

### 問題：單一 Agent 的局限性

在企業文件審查場景中，一份草案往往需要經過多個部門的審查，每個部門有不同的審查標準與專業知識：

```csharp
// 單一 Agent - 所有審查都由同一個 Agent 處理，缺乏專業分工
ChatCompletionAgent agent = new()
{
    Name = "ReviewAgent",
    Instructions = "你要同時檢查人資政策、資安規範、法規合規...",  // Instructions 越來越長，審查品質下降
    Kernel = kernel
};
```

### 解決方案：SequentialOrchestration 循序協作

SequentialOrchestration 將多個專責 Agent 串接成一條流水線，前一個 Agent 的輸出自動作為下一個 Agent 的輸入：

```csharp
// 多 Agent 流水線 - 各部門依序審查，意見逐步累積
SequentialOrchestration orchestration = new(hrAgent, itAgent, complianceAgent)
{
    Name = "DocumentReviewOrchestration",
    Description = "多代理人協同審查文件內容"
};
// 執行順序：使用者草案 → HRAgent → ITAgent → ComplianceAgent → 最終報告
```

## SequentialOrchestration 的核心優勢

| 優勢 | 說明 |
|------|------|
| **職責分離** | 每個 Agent 專注於特定領域的審查，Instructions 簡潔明確 |
| **意見累積** | 前一個 Agent 的輸出（含審查意見）自動傳遞給下一個 Agent |
| **流程固定** | 審查順序明確，確保每個部門都會被經過，不會遺漏 |
| **可擴展性** | 新增審查部門只需新增 Agent 並加入流水線 |
| **零配置轉交** | 不需設定轉交規則，按建構順序自動串接 |

## 與 HandoffOrchestration 的差異

| 項目 | multi-agent-handoff-app | multi-agent-sequential-app (本專案) |
|------|------------------------|-------------------------------------|
| **協調模式** | HandoffOrchestration（條件轉交） | SequentialOrchestration（循序流水線） |
| **執行順序** | 動態：由 AI 判斷轉交給誰 | 固定：依照建構時的 Agent 順序 |
| **適用場景** | 問題分流、客服系統 | 文件審查、流水線作業 |
| **Agent 角色** | 1 分流 + N 專責 | N 個依序執行的 Agent |
| **轉交機制** | 自動注入 `transfer_to_XXX` 工具 | 自動按順序傳遞輸出 |
| **Plugin 使用** | 有（查詢企業資料） | 無（純 LLM 審查） |
| **互動模式** | 多輪對話 | 單次提交、一次性審查 |

## 三種 Orchestration 模式比較

| 模式 | 適用場景 | 執行方式 |
|------|---------|---------|
| **SequentialOrchestration** | 流水線作業（審查、翻譯校對） | A → B → C，固定順序 |
| **HandoffOrchestration** | 問題分流（客服、工單系統） | A → 依條件轉交 B 或 C |
| **GroupChatOrchestration** | 多方討論（腦力激盪、辯論） | A、B、C 輪流發言討論 |

## 功能特性

- **SequentialOrchestration**：多代理人循序協作機制
- **流水線審查**：草案依序經過三個部門審查
- **意見累積**：每個 Agent 保留前面部門的審查意見，並加上自己的意見
- **進度顯示**：Spinner 動畫讓使用者知道審查正在進行中
- **審查部門**：
  - 人資部門（個資保護、請假打卡、性別平等）
  - IT 部門（帳密安全、軟體安裝規範）
  - 合規部門（客戶資料保護、商業機密、仇恨言論）

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
│                   提交公司政策草案                            │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│               SequentialOrchestration（循序流水線）           │
│                                                             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │  HRAgent     │ →  │  ITAgent     │ →  │ Compliance   │  │
│  │ (人資審查員)  │    │ (資安審查員)  │    │  Agent       │  │
│  │              │    │              │    │ (法遵審查員)  │  │
│  │ 審查項目：    │    │ 審查項目：    │    │ 審查項目：    │  │
│  │ - 員工個資   │    │ - 帳密外洩   │    │ - 客戶資料   │  │
│  │ - 請假打卡   │    │ - 私人軟體   │    │ - 商業機密   │  │
│  │ - 性別歧視   │    │              │    │ - 仇恨言論   │  │
│  └──────────────┘    └──────────────┘    └──────────────┘  │
│       輸出：                輸出：              輸出：        │
│   [草案]+[人資意見]   [草案]+[人資]+[IT]  [草案]+[全部意見]  │
└─────────────────────────────────────────────────────────────┘
                             ↓
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
使用者提交：公司新政策草案
        │
        ▼
┌───────────────────────────────────────────┐
│  Step 1：HRAgent 接收原始草案              │
│  審查人資政策合規性                         │
│  輸出：[草案內容] + [人資部門審查意見]      │
└───────────────────────────────────────────┘
        │ 自動傳遞給下一個 Agent
        ▼
┌───────────────────────────────────────────┐
│  Step 2：ITAgent 接收 HRAgent 的輸出       │
│  保留人資意見，加上資安審查                 │
│  輸出：[草案] + [人資意見] + [IT意見]       │
└───────────────────────────────────────────┘
        │ 自動傳遞給下一個 Agent
        ▼
┌───────────────────────────────────────────┐
│  Step 3：ComplianceAgent 接收累積結果      │
│  保留所有既有意見，加上法遵審查             │
│  輸出：[草案] + [人資] + [IT] + [合規意見]  │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  主程式透過 GetValueAsync() 取得最終報告    │
│  顯示完整的多部門審查結果                   │
└───────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-multi-agent-sequential-app/
├── Program.cs                                              # 主程式（Agent 定義、Orchestration 設定、審查執行）
├── semantic-multi-agent-sequential-app.csproj               # 專案設定
└── README.md                                               # 本文件
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-multi-agent-sequential-app
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

由於 `SequentialOrchestration` 目前標記為實驗性 API（`SKEXP0110`），需在 `.csproj` 中加入：

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
草案內容：
    ### 以下是使用者的具體提交內容

    公司新政策草案：
    自2024年起，所有員工可申請每月1天遠端工作日，
    加班工時上限每月40小時，須提前主管同意。
    女性同仁不得申請生理假。
    另外為了提升資安意識，所有員工必須每半年參加資安線上訓練一次。

    ### 請審查此草案是否符合人資政策、資訊安全與合規要求。

開始進行文件審查協作流程...

✅ 審查完成：
[合規部門審查意見]
1. **性別歧視問題**：
   - 同意人資部門關於生理假的意見。目前草案明顯涉及性別歧視，違反法律和公司政策原則。
     建議刪除或修改該條以符合勞工法律和公司多元包容的政策。

2. **勞工法規遵循問題**：
   - 每月設定 40 小時加班上限需符合當地法律與行業標準。建議確認此上限是否符合勞工法規。
     有必要列出具體的計算機制和報酬支付方式，以避免後續若干法律問題。

3. **資安條款不足**：
   - 同意IT部門對資安措施的建議。資安條款應加強，以確保在遠端工作日員工所使用的所有
     設備及軟體都符合公司資安政策。建議明確列出必須遵循的資安步驟以及因未遵守這些步驟
     而可能產生的後果。

4. **內部政策透明度和合規培訓**：
   - 建議增加對新政策的內部培訓，提升員工對政策的理解與執行落實的能力。因此，建議在草案
     中加入詳細的合規培訓計劃，包括頻率、對象人員和內容大綱。

由於草案目前在性別平等和合法合規上存在瑕疵，建議在定稿前進行修訂，以保證符合法律規範和
公司規章制度的要求，並促進一個更包容的工作環境。
```

## 核心程式碼說明

### 1. 建立 ChatCompletionAgent

每個 Agent 都是獨立的 AI 審查角色，擁有專屬的審查指令：

```csharp
// 人資審查員 - 第一個執行，直接接收使用者草案
var hrAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "HRAgent",
    Description = "人資政策審查員",
    Instructions = """
        請審查使用者提交內容是否符合公司人資政策
        - 不得出現員工個人資料
        - 請假加班必須填單申請
        - 不得有性騷擾或性別歧視言論
        ...
    """
};
```

### 2. 設計「意見累積」的 Instructions

Sequential 模式的關鍵設計：後續 Agent 必須在 Instructions 中明確要求「保留前面部門的審查意見」：

```csharp
// IT 審查員 - 第二個執行，接收 HRAgent 的輸出
var itAgent = new ChatCompletionAgent()
{
    Instructions = """
        你將會收到一份公司政策草案以及前面部門的審查意見（若有）。
        請保留所有既有意見內容，再加上你本部門的審查意見，不可刪除任何意見。
        ...
    """
};
```

> **重要**：若未要求保留既有意見，後續 Agent 可能會覆蓋前面部門的審查結果。

### 3. 建立 SequentialOrchestration

只需將 Agent 按順序傳入建構函式，即可定義流水線：

```csharp
// 按順序傳入：HRAgent → ITAgent → ComplianceAgent
SequentialOrchestration orchestration = new(hrAgent, itAgent, complianceAgent)
{
    Name = "DocumentReviewOrchestration",
    Description = "多代理人協同審查文件內容"
};
```

### 4. 執行 Orchestration 並顯示進度

```csharp
// 啟動 Runtime
InProcessRuntime runtime = new();
await runtime.StartAsync();

// 啟動流水線
var result = await orchestration.InvokeAsync(input, runtime);

// 在背景顯示 Spinner 動畫
var cts = new CancellationTokenSource();
var spinnerTask = Task.Run(async () =>
{
    var elapsed = System.Diagnostics.Stopwatch.StartNew();
    while (!cts.Token.IsCancellationRequested)
    {
        int agentIndex = Math.Min((int)(elapsed.Elapsed.TotalSeconds / 12), agents.Length - 1);
        Console.Write($"\r⠹ 審查進行中... 預估階段：{agents[agentIndex]}（已耗時 {elapsed.Elapsed.TotalSeconds:F0} 秒）");
        await Task.Delay(100, cts.Token);
    }
}, cts.Token);

// 等待最終結果
string finalReport = await result.GetValueAsync(TimeSpan.FromSeconds(3000));
cts.Cancel();
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

### SequentialOrchestration

將多個 Agent 串接成一條流水線，前一個 Agent 的完整輸出自動作為下一個 Agent 的輸入。不需要設定轉交規則，執行順序完全由建構時的參數順序決定。

### InProcessRuntime

提供代理人之間的訊息傳遞與排程機制，所有代理人在同一個行程內運行，透過記憶體中的訊息佇列通訊。必須在 `InvokeAsync()` 之前呼叫 `StartAsync()` 啟動。

### 意見累積模式

Sequential Orchestration 中的關鍵設計模式。由於每個 Agent 只接收前一個 Agent 的輸出，若不在 Instructions 中明確要求「保留既有意見」，後續 Agent 可能只會輸出自己的審查結果，導致前面部門的意見遺失。

### SKEXP0110 警告

SequentialOrchestration 相關的 API 目前標記為實驗性（Experimental），需在 `.csproj` 中加入 `<NoWarn>$(NoWarn);SKEXP0110</NoWarn>` 來抑制編譯警告。

## 常見問題

### Q: 為什麼最終報告中只有最後一個 Agent 的意見？

**A: 後續 Agent 的 Instructions 中沒有要求保留前面的意見。** 請確保每個非第一個 Agent 的 Instructions 都包含類似「請保留所有既有意見內容，再加上你本部門的審查意見，不可刪除任何意見」的指示。

### Q: 可以動態調整 Agent 的執行順序嗎？

**A: SequentialOrchestration 的順序在建構時即固定。** 若需要動態分流，請改用 `HandoffOrchestration`。若需要多方討論，請改用 `GroupChatOrchestration`。

### Q: 如何新增一個審查部門？

**A: 只需兩步：**
1. 建立新的 `ChatCompletionAgent`，定義該部門的審查指令
2. 將其加入 `SequentialOrchestration` 的建構參數中

```csharp
// 例如新增財務審查員
var financeAgent = new ChatCompletionAgent()
{
    Kernel = kernel,
    Name = "FinanceAgent",
    Description = "財務審查員",
    Instructions = "請保留所有既有意見，再審查預算與財務合規..."
};

// 加入流水線
SequentialOrchestration orchestration = new(hrAgent, itAgent, complianceAgent, financeAgent);
```

### Q: 審查過程中 console 一直靜止怎麼辦？

**A: 本專案已加入 Spinner 動畫。** 若仍感覺卡住，請確認網路連線正常、OpenAI API Key 有效。每個 Agent 呼叫 LLM 通常需要 5-15 秒，三個 Agent 合計約 15-45 秒。

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **Token 限制**：每次 Orchestration 呼叫涉及多個 Agent，Token 消耗較高
3. **成本考量**：GPT-4o 的 API 調用有費用，多 Agent 模式會產生多次 API 呼叫
4. **意見累積膨脹**：隨著 Agent 數量增加，傳遞的文字量會逐步增長，需注意 Token 上限
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
9. **semantic-multi-agent-sequential-app** - Multi-Agent Sequential 協作（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [Agent Orchestration 文檔](https://learn.microsoft.com/semantic-kernel/agents/agent-orchestration)
- [SequentialOrchestration 範例](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithAgents)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
