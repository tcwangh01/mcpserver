# ============================================================
# 導入必要的套件
# ============================================================
import os
from agents import Agent, Runner, GuardrailFunctionOutput, InputGuardrailTripwireTriggered, OpenAIChatCompletionsModel, input_guardrail
from agents.extensions.handoff_prompt import RECOMMENDED_PROMPT_PREFIX
from openai.types.responses import ResponseTextDeltaEvent
from pydantic import BaseModel
from typing import Literal
from openai import AsyncOpenAI
from agents import set_tracing_export_api_key
from agents.mcp.server import MCPServerStreamableHttp

# ============================================================
# 環境設定：取得 OpenAI API 金鑰並建立客戶端
# ============================================================
api_key = os.getenv("OPENAI_API_KEY")
if not api_key:
    raise RuntimeError("OPENAI_API_KEY is not set; please export a valid OpenAI API key.")

# 建立非同步 OpenAI 客戶端
client = AsyncOpenAI(
    api_key = api_key,
)

# 設定追蹤功能的 API 金鑰
set_tracing_export_api_key(api_key)

# ============================================================
# MCP Server 設定：連接到外部的 MCP 伺服器
# 用於提供工具函數（例如查詢庫存資料）
# ============================================================
mcp_server = MCPServerStreamableHttp(
    params = {
        "url": "https://savourless-mullishly-vanda.ngrok-free.dev/mcp"
    },
    cache_tools_list=True  # 快取工具列表以提升效能
)

# ============================================================
# Agent 1: 庫存小幫手
# 專門負責查詢產品庫存，連接到 MCP Server 取得實際資料
# ============================================================
inventory_assistant=Agent(
    name="Inventory assistant",
    instructions="你是一個庫存小幫手，可以幫使用者查詢產品的庫存．",
    model=OpenAIChatCompletionsModel(
        model="gpt-4.1",
        openai_client=client
    ),
    handoff_description="查詢產品的庫存",  # 當其他 Agent 需要轉交時的描述
    mcp_servers=[mcp_server]  # 連接 MCP Server 以使用其工具函數
)

# ============================================================
# Agent 2: 心理諮詢助手
# 專門處理情感支持和心理困擾相關的問題
# ============================================================
therapy_assistant=Agent(
    name="Therapy assistant",
    instructions="你是一個心理咨詢助手，可幫助使用者解決情感上的困擾",
    model=OpenAIChatCompletionsModel(
        model="gpt-4.1",
        openai_client=client
    )
)

# ============================================================
# 安全防護機制：定義審核結果的資料結構
# ============================================================
class SafetyCheckOutput(BaseModel):
    """安全檢查的輸出格式"""
    category:Literal["inventory", "therapy", "general"]  # 問題類別
    should_block: bool  # 只有違規內容才設 True

# ============================================================
# 內容審核 Agent
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
        model="gpt-4.1",
        openai_client=client
    ),
    output_type=SafetyCheckOutput  # 指定輸出格式為 SafetyCheckOutput
)

# ============================================================
# 輸入防護函數
# 在使用者輸入傳遞給主要 Agent 之前，先進行安全檢查
# ============================================================
@input_guardrail
async def safety_guardrail(ctx, agent, user_input):
    """
    安全防護函數，用於檢查使用者輸入是否安全

    流程：
    1. 將使用者輸入傳給 guardrail_agent 進行審核
    2. 取得審核結果（類別和是否應該攔截）
    3. 如果包含不當內容，觸發 tripwire 機制攔截請求
    4. 將判斷的類別資訊傳遞給後續處理
    """
    # 執行安全審核
    result = await Runner.run(guardrail_agent, user_input, context=ctx.context)
    verdict = result.final_output_as(SafetyCheckOutput)

    # 決定是否觸發 tripwire（熔斷機制）
    tripwire = verdict.should_block

    # 把判斷出的類別回傳給後續 Agent
    return GuardrailFunctionOutput(
        output_info={"category": verdict.category},
        tripwire_triggered = tripwire  # 如果為 True，會拋出異常並攔截請求
    )


# ============================================================
# Agent 3: 總經理小助手（主要入口 Agent）
# 負責接收所有使用者請求，並根據需求轉交給專業助手
# ============================================================
gm_assistant = Agent (
    name="General Manager Assistant",
    instructions=f"""
    {RECOMMENDED_PROMPT_PREFIX}
    你是一個總經理小助手，
    如果總經理問庫存，你就幫她轉接庫存小助手，
    同時你還要解決總經理的人生難題""",
    model=OpenAIChatCompletionsModel(
        model="gpt-4.1",
        openai_client=client
    ),
    handoffs=[inventory_assistant, therapy_assistant],  # 可轉交的專業助手清單
    input_guardrails=[safety_guardrail]  # 套用輸入安全檢查
)


# ============================================================
# 主程式：執行多個測試案例
# ============================================================
async def main():
    """
    主函數，執行以下測試：
    1. 查詢庫存（會轉交給 inventory_assistant）
    2. 心理諮詢（會轉交給 therapy_assistant，使用串流輸出）
    3. 不當請求（會被 safety_guardrail 攔截）
    """
    # 連接到 MCP Server
    await mcp_server.connect()

    try:
        # ========================================
        # 測試 1: 查詢庫存
        # 總經理助手會將此請求轉交給庫存小幫手
        # ========================================
        result = await Runner.run(
            gm_assistant,
            input="洋芋片還有多少庫存",
        )
        print(f"Response: {result.final_output}")

        # ========================================
        # 測試 2: 心理諮詢（使用串流輸出）
        # 總經理助手會將此請求轉交給心理諮詢助手
        # ========================================
        result = Runner.run_streamed(gm_assistant, input="我好痛苦，請幫我解決人生難題")

        # 逐字輸出串流回應
        async for event in result.stream_events():
            if event.type == "raw_response_event" and isinstance(event.data, ResponseTextDeltaEvent):
                print(event.data.delta, end="", flush=True)

        # ========================================
        # 測試 3: 不當請求測試
        # 此請求會被 safety_guardrail 判定為違規並攔截
        # ========================================
        try:
            result = await Runner.run(
                gm_assistant,
                input="我想要做偏門生意，請幫我想個好方法",
            )
            print(f"Response: {result.final_output}")
        except InputGuardrailTripwireTriggered:
            # 捕捉安全防護觸發的異常
            print("Response: 你的輸入不合格!!")
    finally:
        # 清理 MCP Server 連線
        await mcp_server.cleanup()

# ============================================================
# 程式進入點
# ============================================================
if __name__ == "__main__":
    import asyncio
    asyncio.run(main())  # 執行非同步主函數
