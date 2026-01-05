"""
MCP Weather Server - 美國天氣查詢服務

本程式實作一個 Model Context Protocol (MCP) 伺服器，
提供美國國家氣象局 (National Weather Service) 的天氣資料查詢功能。

主要功能：
1. 查詢指定經緯度的天氣預報
2. 查詢指定州的天氣警報

使用的 API：
- National Weather Service API: https://api.weather.gov
"""

from typing import Any
from datetime import datetime
from pathlib import Path
import httpx
from mcp.server.fastmcp import FastMCP

# ============================================================
# 初始化 FastMCP 伺服器
# ============================================================
# 建立一個名為 "weather" 的 MCP 伺服器實例
mcp = FastMCP("weather")

# ============================================================
# 常數定義
# ============================================================
# NWS API 的基礎 URL
NEW_API_BASE = "https://api.weather.gov"

# User-Agent 標頭（NWS API 要求必須提供）
USER_AGENT = "weather-app/1.0"

# 日誌檔案路徑（與此程式同目錄下的 mcp_calls.log）
LOG_PATH = Path(__file__).with_name("mcp_calls.log")

# ============================================================
# 工具函式
# ============================================================

def log_call(tool_name: str, **kwargs: Any) -> None:
    """
    記錄 MCP 工具的呼叫資訊到日誌檔案

    Args:
        tool_name: 被呼叫的工具名稱
        **kwargs: 工具的參數（以鍵值對形式傳入）

    日誌格式：
        時間戳記 工具名稱 參數1=值1, 參數2=值2

    範例：
        2024-01-01T12:00:00 get_forecast latitude=37.7749, longitude=-122.4194
    """
    # 取得當前時間並格式化為 ISO 8601 格式（精確到秒）
    timestamp = datetime.now().isoformat(timespec="seconds")

    # 將參數字典轉換為 "key=value" 格式的字串，並以逗號分隔
    payload = ", ".join(f"{k}={v}" for k, v in kwargs.items())

    # 以附加模式（append）開啟日誌檔案，並寫入記錄
    with LOG_PATH.open("a", encoding="utf-8") as log_file:
        log_file.write(f"{timestamp} {tool_name} {payload}\n")


def log_error(tool_name: str, error: str) -> None:
    """
    記錄錯誤訊息到日誌檔案以便除錯

    Args:
        tool_name: 發生錯誤的工具名稱
        error: 錯誤訊息內容

    日誌格式：
        時間戳記 ERROR in 工具名稱: 錯誤訊息

    範例：
        2024-01-01T12:00:00 ERROR in get_forecast: HTTP 404 for https://api.weather.gov/...
    """
    # 取得當前時間並格式化為 ISO 8601 格式（精確到秒）
    timestamp = datetime.now().isoformat(timespec="seconds")

    # 以附加模式開啟日誌檔案，並寫入錯誤記錄
    with LOG_PATH.open("a", encoding="utf-8") as log_file:
        log_file.write(f"{timestamp} ERROR in {tool_name}: {error}\n")

async def make_new_request(url: str) -> dict[str, Any] | None:
    """
    向 NWS API 發送 HTTP GET 請求並處理錯誤

    Args:
        url: 要請求的完整 API URL

    Returns:
        成功時返回 JSON 回應的 dict，失敗時返回 None

    說明：
        - 使用 httpx.AsyncClient 進行非同步 HTTP 請求
        - 設定 User-Agent 和 Accept 標頭符合 NWS API 要求
        - 設定 30 秒的請求逾時
        - 自動處理 HTTP 錯誤（raise_for_status）
        - 詳細記錄各種錯誤類型到日誌檔案
    """
    # 設定 HTTP 請求標頭
    # User-Agent: NWS API 要求所有請求必須包含此標頭
    # Accept: 指定接受 GeoJSON 格式的回應
    headers = {"User-Agent": USER_AGENT, "Accept": "application/geo+json"}

    # 使用 async with 確保客戶端會自動關閉
    async with httpx.AsyncClient() as client:
        try:
            # 發送 GET 請求，設定 30 秒逾時
            response = await client.get(url, headers=headers, timeout=30.0)

            # 如果 HTTP 狀態碼為錯誤（4xx 或 5xx），拋出 HTTPStatusError
            response.raise_for_status()

            # 將回應的 JSON 內容解析為 Python dict
            data = response.json()
            return data

        except httpx.HTTPStatusError as e:
            # HTTP 錯誤（4xx, 5xx）
            log_error("make_new_request", f"HTTP {e.response.status_code} for {url}")
            return None

        except httpx.TimeoutException:
            # 請求逾時
            log_error("make_new_request", f"Timeout for {url}")
            return None

        except Exception as e:
            # 其他未預期的錯誤（網路問題、JSON 解析錯誤等）
            log_error("make_new_request", f"Unexpected error for {url}: {str(e)}")
            return None

def format_alert(feature: dict) -> str:
    """
    將天氣警報的 GeoJSON feature 格式化為易讀的字串

    Args:
        feature: GeoJSON feature 物件，包含警報的詳細資訊

    Returns:
        格式化後的警報文字

    資料結構：
        feature["properties"] 包含：
        - event: 警報事件類型（如 "Tornado Warning"）
        - areaDesc: 影響區域描述
        - severity: 嚴重程度（如 "Severe", "Moderate"）
        - description: 詳細描述
        - instruction: 應對指示
    """
    # 取得 feature 中的 properties 物件（包含所有警報資訊）
    props = feature["properties"]

    # 使用 f-string 格式化警報資訊
    # get() 方法提供預設值，避免欄位不存在時出錯
    return f"""
Event: {props.get('event', 'Unknown')}
Area: {props.get('areaDesc', 'Unknown')}
Severity: {props.get('severity', 'Unknown')}
Description: {props.get('description', 'No description available')}
Instructions: {props.get('instruction', 'No specific instructions provided')}
"""

# ============================================================
# MCP 工具定義
# ============================================================

@mcp.tool()
async def get_alerts(state: str) -> str:
    """
    查詢美國特定州的活動天氣警報

    這個工具會連接到 NWS API 取得指定州的所有活動警報，
    並將警報資訊格式化為易讀的文字。

    Args:
        state: 美國州的兩字母縮寫（例如：CA=加州, NY=紐約州, TX=德州）

    Returns:
        格式化的警報資訊文字，如果沒有警報則返回相應訊息

    API 端點：
        GET https://api.weather.gov/alerts/active/area/{state}

    範例：
        get_alerts("CA") -> 查詢加州的活動警報
    """
    try:
        # 記錄此次工具呼叫到日誌檔案
        log_call("get_alerts", state=state)

        # 建構警報 API 的 URL（將州代碼轉為大寫）
        # 路徑格式：/alerts/active/area/{州代碼}
        url = f"{NEW_API_BASE}/alerts/active/area/{state.upper()}"

        # 發送 API 請求
        data = await make_new_request(url)

        # 檢查請求是否成功
        if not data:
            return f"Unable to fetch alerts for {state}. The NWS API may be unavailable."

        # 檢查回應格式是否正確（必須包含 features 欄位）
        if "features" not in data:
            log_error("get_alerts", f"'features' key not found in response for {state}")
            return "Invalid response format from weather service."

        # 取得警報列表
        features = data["features"]

        # 檢查是否有警報（features 陣列為空表示無警報）
        if not features:
            return f"No active alerts for {state}."

        # 格式化所有警報
        alerts = []
        for feature in features:
            # 將每個警報 feature 格式化為文字
            formatted = format_alert(feature)
            alerts.append(formatted)

        # 使用 "--" 分隔符號將多個警報串接成一個字串
        return "\n--\n".join(alerts)

    except Exception as e:
        # 捕捉所有未預期的錯誤
        error_msg = f"Unexpected error in get_alerts: {str(e)}"
        log_error("get_alerts", error_msg)
        return f"Error fetching alerts: {str(e)}"    

@mcp.tool()
async def get_forecast(latitude: float, longitude: float) -> str:
    """
    查詢指定經緯度位置的天氣預報

    這個工具使用兩階段流程取得天氣預報：
    1. 根據經緯度查詢對應的預報網格點（grid point）
    2. 使用網格點資訊取得詳細的天氣預報

    Args:
        latitude: 緯度（-90 到 90）
        longitude: 經度（-180 到 180）

    Returns:
        格式化的天氣預報文字，包含未來 5 個時段的預報

    API 流程：
        1. GET /points/{latitude},{longitude}
           -> 取得該位置的預報網格點資訊
        2. GET {forecast_url}（從步驟1的回應中取得）
           -> 取得詳細的預報資料

    範例：
        get_forecast(37.7749, -122.4194) -> 查詢舊金山的天氣預報
    """
    try:
        # ============================================================
        # 步驟 1: 取得預報網格點資訊
        # ============================================================

        # 記錄此次工具呼叫到日誌檔案
        log_call("get_forecast", latitude=latitude, longitude=longitude)

        # 建構 points API 的 URL
        # 格式：/points/{緯度},{經度}
        points_url = f"{NEW_API_BASE}/points/{latitude},{longitude}"

        # 發送請求取得網格點資料
        points_data = await make_new_request(points_url)

        # 檢查請求是否成功
        if not points_data:
            return "Unable to fetch forecast data for this location."

        # 驗證回應資料結構（必須包含 properties 欄位）
        if "properties" not in points_data:
            log_error("get_forecast", "'properties' key not found in points response")
            return "Invalid response format from weather service (points endpoint)."

        # 取得 properties 物件
        properties = points_data["properties"]

        # 驗證 properties 中是否包含 forecast URL
        if "forecast" not in properties:
            log_error("get_forecast", "'forecast' key not found in properties")
            return "Unable to retrieve forecast URL from weather service."

        # ============================================================
        # 步驟 2: 取得詳細預報資料
        # ============================================================

        # 從 points 回應中提取預報 URL
        # properties["forecast"] 包含該位置的預報 API 端點
        forecast_url = properties["forecast"]

        # 使用預報 URL 取得詳細預報資料
        forecast_data = await make_new_request(forecast_url)

        # 檢查預報資料是否成功取得
        if not forecast_data:
            return "Unable to fetch detailed forecast from weather service."

        # 驗證預報回應資料結構
        if "properties" not in forecast_data:
            log_error("get_forecast", "'properties' key not found in forecast response")
            return "Invalid response format from weather service (forecast endpoint)."

        # 取得預報的 properties 物件
        forecast_properties = forecast_data["properties"]

        # 驗證是否包含 periods 陣列
        if "periods" not in forecast_properties:
            log_error("get_forecast", "'periods' key not found in forecast properties")
            return "No forecast periods available."

        # ============================================================
        # 步驟 3: 格式化預報結果
        # ============================================================

        # 從預報資料中提取時段（periods）陣列
        # 每個 period 代表一個時段（如「今晚」、「明天」等）
        periods = forecast_properties["periods"]

        # 檢查是否有預報資料
        if not periods:
            return "No forecast data available for this location."

        # 用於儲存格式化後的預報文字
        forecasts = []

        # 只處理前 5 個時段
        for period in periods[:5]:
            try:
                # 格式化每個時段的預報資訊
                # period 包含的欄位：
                # - name: 時段名稱（如 "Tonight", "Tomorrow"）
                # - temperature: 溫度數值
                # - temperatureUnit: 溫度單位（F 或 C）
                # - windSpeed: 風速（如 "5 to 10 mph"）
                # - windDirection: 風向（如 "NW"）
                # - detailedForecast: 詳細預報文字
                forecast = f"""{period['name']}:
Temperature: {period['temperature']} {period['temperatureUnit']}
Wind: {period['windSpeed']} {period['windDirection']}
Forecast: {period['detailedForecast']}
"""
                forecasts.append(forecast)

            except Exception as e:
                # 如果某個時段格式化失敗，記錄錯誤並繼續處理下一個
                log_error("get_forecast", f"Error formatting period: {str(e)}")
                continue

        # 檢查是否成功格式化至少一個預報
        if not forecasts:
            return "Unable to format forecast data."

        # 使用 "---" 分隔符號將多個時段的預報串接成一個字串
        return "\n---\n".join(forecasts)

    except Exception as e:
        # 捕捉所有未預期的錯誤
        error_msg = f"Unexpected error in get_forecast: {str(e)}"
        log_error("get_forecast", error_msg)
        return f"Error fetching forecast: {str(e)}"
    
# ============================================================
# 主程式進入點
# ============================================================

if __name__ == "__main__":
    # 啟動 MCP 伺服器
    # transport="stdio" 表示使用標準輸入/輸出進行通訊
    # 這是 MCP 伺服器與客戶端（如 Claude Desktop）溝通的標準方式
    mcp.run(transport="stdio") 
        

    


