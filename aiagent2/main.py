"""
OpenAI Agents 多代理工作流範例
本程式展示如何使用 OpenAI Agents SDK 建立多個專業代理，實現：
1. 多店舖庫存查詢系統
2. 心理諮詢服務
3. 輸入內容安全審核
4. 代理之間的自動交接（Handoff）
"""

# ============================================================
# 標準函式庫導入
# ============================================================
import os          # 用於存取環境變數
import sys         # 系統相關功能
import json        # JSON 資料處理
import asyncio     # 非同步程式設計支援
from typing import Literal  # 型別提示，限定特定字面值

# ============================================================
# 第三方函式庫導入
# ============================================================
from pydantic import BaseModel  # 資料驗證與設定管理
from openai import AsyncOpenAI  # OpenAI 非同步客戶端
from openai.types.responses import ResponseTextDeltaEvent  # 串流回應事件類型

# 設定環境變數以抑制 asyncio 的 ResourceWarning 警告
os.environ["PYTHONWARNINGS"] = "ignore::ResourceWarning"

# ============================================================
# OpenAI Agents SDK 核心元件導入
# ============================================================
from agents import (
    Agent,                        # 代理類別，定義單一 AI 助手
    Runner,                       # 執行器，用於運行代理工作流
    GuardrailFunctionOutput,      # 防護機制的輸出格式
    OpenAIChatCompletionsModel,   # OpenAI Chat Completions 模型封裝
    input_guardrail,              # 輸入防護裝飾器
    set_tracing_export_api_key,   # 設定追蹤功能的 API 金鑰
)

# ============================================================
# OpenAI Agents SDK 擴充功能導入
# ============================================================
from agents.extensions.models.litellm_model import LitellmModel  # LiteLLM 模型支援（可使用 Gemini 等）
from agents.extensions.handoff_prompt import RECOMMENDED_PROMPT_PREFIX  # 推薦的交接提示詞前綴
from agents.extensions.visualization import draw_graph  # 代理結構視覺化工具
from agents.lifecycle import AgentHooks  # 代理生命週期鉤子，用於監控與日誌

# ============================================================
# 其他函式庫導入與設定
# ============================================================
import warnings  # 警告控制
import litellm   # LiteLLM 函式庫，支援多種 LLM 提供商

# 抑制不必要的警告訊息
warnings.filterwarnings("ignore", category=UserWarning, module="pydantic")
warnings.filterwarnings("ignore", category=ResourceWarning, message="unclosed.*")

# LiteLLM 設定
litellm.suppress_debug_info = True  # 抑制除錯訊息
litellm.drop_params = True           # 自動移除不支援的參數

# ============================================================
# 環境設定：取得 API 金鑰並建立客戶端
# ============================================================

# 從環境變數取得 OpenAI API 金鑰
# 使用方式：在終端機執行 export OPENAI_API_KEY="your-api-key"
openai_api_key = os.getenv("OPENAI_API_KEY")
if not openai_api_key:
    raise RuntimeError("OPENAI_API_KEY is not set; please export a valid OpenAI API key.")

# 建立非同步 OpenAI 客戶端
# AsyncOpenAI 支援非同步操作，適合處理多個並發請求
client = AsyncOpenAI(
    api_key = openai_api_key,
)

# 設定追蹤功能的 API 金鑰
# 此功能可用於追蹤和監控代理的執行過程
set_tracing_export_api_key(openai_api_key)

# 從環境變數取得 Gemini API 金鑰
# Gemini 是 Google 的 LLM，透過 LiteLLM 整合使用
gemini_api_key = os.getenv("GEMINI_API_KEY")
if not gemini_api_key:
    raise RuntimeError("GEMINI_API_KEY is not set; please export a valid Gemini API key.")

# 設定 Gemini 模型名稱，預設為 gemini-2.5-flash
# LiteLLM 使用格式：提供商/模型名稱
gemini_model = os.getenv("GEMINI_MODEL", "gemini/gemini-2.5-flash")

# ============================================================
# 模擬庫存資料庫
# ============================================================
# 使用 JSON 格式定義各分店的商品庫存
# 實際應用中可替換為真實資料庫查詢
inventory_json="""
{
    "台北店":{"咖啡":12, "洋芋片":7},
    "台中店":{"咖啡":9, "洋芋片":14}
}
"""

# 將 JSON 字串解析為 Python 字典，方便程式存取
inventory_db=json.loads(inventory_json)

# ============================================================
# 動態建立分店專屬的庫存查詢工具
# ============================================================
def make_inventory_tool(branch:str):
    """
    建立特定分店的庫存查詢工具

    參數：
        branch: 分店名稱（例如：「台北店」、「台中店」）

    返回：
        function_tool 裝飾的函數，可供 Agent 使用

    用法：
        tool_a = make_inventory_tool("台北店")  # 建立台北店專屬工具
    """
    from agents import function_tool

    @function_tool
    def get_inventory(商品:str) -> str:
        """
        查詢指定商品的庫存數量

        參數：
            商品: 商品名稱（例如：「咖啡」、「洋芋片」）

        返回：
            包含分店名稱、商品名稱和庫存數量的字串
        """
        qty = inventory_db.get(branch,{}).get(商品,0)
        return f"{branch}{商品}庫存量為 {qty}"
    return get_inventory

# 建立台北店和台中店的專屬庫存查詢工具
tool_a = make_inventory_tool("台北店")
tool_b = make_inventory_tool("台中店")

# ============================================================
# 代理生命週期監控鉤子（Hooks）
# ============================================================
class AuditHooks(AgentHooks):
    """
    自定義的代理監控鉤子類別
    用於追蹤和記錄代理的各種操作行為

    主要功能：
    - 監控工具調用的開始與結束
    - 記錄代理之間的交接過程
    - 便於除錯和了解代理執行流程
    """
    async def on_tool_start(self, context, agent, tool):
        """當代理開始使用工具時觸發"""
        print(f"[Audit] {agent.name} 將使用工具 {tool.name}")

    async def on_tool_end(self, context, agent, tool, result):
        """當工具執行完成時觸發"""
        print(f"[Audit] {tool.name} 完成，結果：{result}")

    async def on_handoff(
            self,
            context,
            agent,
            source
    ):
        """當代理交接發生時觸發"""
        print(f"[Audit]{source.name}->交接給 {agent.name}")

# ============================================================
# 定義專業代理（Agents）
# ============================================================

# 台北店助手代理
# 負責處理台北店的庫存查詢
branch_a_agent = Agent (
    name="Branch Taipei assistant",
    instructions="你是台北店的小助手，只回答台北店產品庫存．",
    model=OpenAIChatCompletionsModel(model="gpt-5", openai_client=client),
    tools=[tool_a]  # 配備台北店專屬的庫存查詢工具
)

# 台中店助手代理
# 負責處理台中店的庫存查詢
branch_b_agent = Agent (
    name="Branch Taichung assistant",
    instructions="你是台中店的小助手，只回答台中店產品庫存．",
    model=OpenAIChatCompletionsModel(model="gpt-5", openai_client=client),
    tools=[tool_b]  # 配備台中店專屬的庫存查詢工具
)

# 心理諮商助手代理
# 使用 Gemini 模型提供心理支持服務
# 示範如何透過 LiteLLM 整合非 OpenAI 的模型
therapy_agent = Agent (
    name="Therapy assistant",
    instructions="你是一位具備同理與專業知識的心理諮商助手．",
    model=LitellmModel(
        model=gemini_model,      # 使用 Gemini 模型
        api_key=gemini_api_key   # Gemini API 金鑰
    ),
)

# ============================================================
# 安全檢查資料模型
# ============================================================
class SafetyCheck(BaseModel):
    """
    安全審核結果的資料結構

    屬性：
        category: 使用者輸入的分類
            - "inventory": 庫存相關查詢
            - "therapy": 心理諮商相關
            - "general": 一般性問題
        should_block: 是否應該攔截此請求（true=攔截, false=放行）
    """
    category: Literal["inventory","therapy","general"]
    should_block: bool

# ============================================================
# 內容審核代理（Guardrail Agent）
# 負責判斷使用者輸入的類別，並檢查是否含有不當內容
# ============================================================
guardrail_agent=Agent(
    name="Guardrail check",
    instructions=(
        "你是內容審核助手．"
        "請判斷 user_input 屬於下列哪一類："
        "1. inventory -> 詢問商品存貨、庫存、價格..."
        "2. therapy   -> 心理支持、情感抒發、尋求建議..."
        "3. generay   -> 其他一般商務或聊天內容"
        "若問題含暴力、違法、色情、自殘等禁忌，請將 should_block 設為 true,"
        "否則應為 false. 請依下列 JSON 輸出："
        '{"category":<category>, "should_block":<true/false>}'
    ),
    model=OpenAIChatCompletionsModel(
        model="gpt-5",
        openai_client=client
    ),
    output_type=SafetyCheck  # 指定輸出格式為 SafetyCheck，確保結構化回應
)

# ============================================================
# 輸入防護函數（Input Guardrail）
# 在使用者輸入傳遞給主要 Agent 之前，先進行安全檢查
# ============================================================
@input_guardrail
async def safety_guardrail(ctx, agent, user_input):
    """
    安全防護函數，用於檢查使用者輸入是否安全

    工作流程：
    1. 將使用者輸入傳給 guardrail_agent 進行審核
    2. 取得審核結果（類別和是否應該攔截）
    3. 如果包含不當內容，觸發 tripwire 機制攔截請求
    4. 將判斷的類別資訊傳遞給後續處理

    參數：
        ctx: 執行上下文
        agent: 當前代理
        user_input: 使用者輸入的內容

    返回：
        GuardrailFunctionOutput: 包含類別資訊和是否觸發攔截
    """
    # 執行安全審核
    result = await Runner.run(guardrail_agent, user_input, context=ctx.context)
    verdict = result.final_output_as(SafetyCheck)

    # 決定是否觸發 tripwire（熔斷機制）
    tripwire = verdict.should_block

    # 把判斷出的類別回傳給後續 Agent
    return GuardrailFunctionOutput(
        output_info={"category": verdict.category},
        tripwire_triggered = tripwire  # 如果為 True，會拋出異常並攔截請求
    )

# ============================================================
# 總經理助手代理（協調者角色）
# 負責接收使用者請求，並根據需求轉交給專業代理
# ============================================================
gm_agent = Agent(
    name="General Manager Assistant",
    instructions=(
        f"{RECOMMENDED_PROMPT_PREFIX}\n"  # 使用推薦的交接提示詞前綴
        " 你是一個總經理小助手：\n"
        " . 如詢問庫存 -> 交接到正確分店 Agent．\n"
        " . 若尋求心理支持 -> 交接 Therapy assistant．\n"
        " . 其他問題直接回答．"
    ),
    model=OpenAIChatCompletionsModel(
        model="gpt-5",
        openai_client=client
    ),
    handoffs=[branch_a_agent, branch_b_agent, therapy_agent],  # 可轉交的專業助手清單
    input_guardrails=[safety_guardrail],  # 套用輸入安全檢查
    hooks=AuditHooks(),  # 啟用監控鉤子
)

# ============================================================
# 輔助函數：平行查詢多個分店的庫存
# ============================================================
async def parallel_inventory_query(product:str) -> str:
    """
    同時查詢台北店和台中店的商品庫存

    參數：
        product: 商品名稱

    返回：
        合併的查詢結果字串

    說明：
        使用 asyncio.gather 並行執行兩個代理任務，
        比依序執行更快速有效率
    """
    a_task = Runner.run(branch_a_agent,f"{product} 庫存?")
    b_task = Runner.run(branch_b_agent,f"{product} 庫存?")
    res_a, res_b = await asyncio.gather(a_task,b_task)
    return f"{res_a.final_output}:{res_b.final_output}"

# ============================================================
# 主程式
# ============================================================
async def main() -> None:
    """
    主要執行函數，展示 OpenAI Agents SDK 的各種功能：
    1. 自動交接（Handoff）機制
    2. 串流回應
    3. 平行查詢
    4. 代理結構視覺化（目前版本有 Bug）
    """
    try:
        # ====================================================
        # 範例 1：庫存查詢（展示自動交接功能）
        # ====================================================
        # 使用者向總經理助手詢問台北店庫存
        # gm_agent 會自動判斷並交接給 branch_a_agent 處理
        result = await Runner.run(gm_agent,input="台北店洋芋片還有多少?")
        print("-" * 40)
        print("[庫存回覆]", result.final_output)

        # ====================================================
        # 範例 2：心理諮詢（展示 LiteLLM 整合與串流回應）
        # ====================================================
        # 使用者尋求心理支持，gm_agent 會交接給 therapy_agent
        # therapy_agent 使用 Gemini 模型，透過 LiteLLM 調用
        print("-" * 40)
        stream = Runner.run_streamed(gm_agent, input="我最近很焦慮，該怎麼辦?")
        # 使用串流模式即時顯示回應，提升使用者體驗
        async for event in stream.stream_events():
            if (event.type == "raw_response_event"
                and isinstance(event.data,ResponseTextDeltaEvent)
            ):
                print(event.data.delta, end="", flush=True)
        print()

        # ====================================================
        # 範例 3：平行查詢（展示非同步並發處理）
        # ====================================================
        # 同時查詢台北店和台中店的咖啡庫存
        # 比依序查詢快一倍
        print("-" * 40)
        combined = await parallel_inventory_query("咖啡")
        print("平行查詢", combined)

        # ====================================================
        # 範例 4：代理結構視覺化（目前有 Bug）
        # ====================================================
        """
        ⚠️  繪圖功能目前無法使用

        問題原因：
        openai-agents 0.6.4 版本的 draw_graph 函數存在 Bug
        在 visualization.py:142 行使用了不正確的類型檢查：
            isinstance(agent, Tool)

        其中 Tool 是一個 Union 泛型類型，在 Python 3.9+ 中
        不能用於 isinstance() 檢查，會拋出 TypeError：
        "Subscripted generics cannot be used with class and instance checks"

        解決方案：
        1. 暫時跳過繪圖功能（當前做法）
        2. 等待官方修復此 Bug
        3. 向 OpenAI 報告：https://github.com/openai/openai-agents-python/issues

        更多資訊：
        - 此問題影響所有 Python 3.9+ 版本
        - 即使降級 Python 版本也無法解決
        - 需要修改 openai-agents 套件本身的程式碼
        """
        try:
            from graphviz.backend import ExecutableNotFound
            draw_graph(gm_agent,filename="agent_graph")
            print("Agent結構圖已輸出為 agent_graph.png")
        except ExecutableNotFound:
            print("[提示] 系統未安裝 Graphviz，已跳過繪圖")
        except TypeError as exc:
            print(f"[提示] 目前 openai-agents 版本無法繪圖")
            print(f"      錯誤訊息：{exc}")
            print(f"      詳情請參閱程式碼註解（main.py:389-397 行）")
    finally:
        # ====================================================
        # 資源清理：確保所有連線正確關閉
        # ====================================================
        # 1. 關閉 OpenAI 客戶端
        # 避免警告：Unclosed client session
        await client.close()

        # 2. 強制關閉 LiteLLM 的全域 aiohttp session
        # LiteLLM 內部會維護一個預設的 session，需要手動關閉
        # 避免警告：Unclosed connector
        try:
            import litellm
            # 關閉主要的 aiohttp session
            if hasattr(litellm, "aiohttp_session") and litellm.aiohttp_session:
                await litellm.aiohttp_session.close()

            # 關閉 LiteLLM 內部的所有 session
            # 這是關鍵：需要呼叫 litellm 的清理函數
            if hasattr(litellm, "module_level_aclient") and litellm.module_level_aclient is not None:
                await litellm.module_level_aclient.close()

        except Exception as e:
            print(f"Warning during litellm cleanup: {e}")

        # 3. 確保所有待處理的協程都完成
        # 給予系統較長時間完成清理工作（從 0.25 增加到 0.5 秒）
        await asyncio.sleep(0.5)

# ============================================================
# 程式進入點
# ============================================================
if __name__ == "__main__":
    # 使用 asyncio.run() 執行非同步主程式
    # 這會自動處理事件迴圈的建立和清理
    asyncio.run(main())
