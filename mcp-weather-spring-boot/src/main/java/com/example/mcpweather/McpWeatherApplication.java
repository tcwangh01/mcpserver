package com.example.mcpweather;

import com.example.mcpweather.service.WeatherService;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

import java.io.BufferedReader;
import java.io.InputStreamReader;

/**
 * MCP Weather Server - Spring Boot Implementation
 *
 * 本程式實作一個 Model Context Protocol (MCP) 伺服器，
 * 提供美國國家氣象局 (National Weather Service) 的天氣資料查詢功能。
 *
 * 主要功能：
 * 1. 查詢指定經緯度的天氣預報 (get_forecast)
 * 2. 查詢指定州的天氣警報 (get_alerts)
 *
 * MCP 通訊方式：
 * - 透過標準輸入/輸出 (STDIO) 與 Claude Desktop 通訊
 * - 使用 JSON-RPC 2.0 協議
 */
@Slf4j
@SpringBootApplication
public class McpWeatherApplication implements CommandLineRunner {

    private final WeatherService weatherService;
    private final ObjectMapper objectMapper;

    public McpWeatherApplication(WeatherService weatherService) {
        this.weatherService = weatherService;
        this.objectMapper = new ObjectMapper();
    }

    public static void main(String[] args) {
        SpringApplication.run(McpWeatherApplication.class, args);
    }

    @Override
    public void run(String... args) throws Exception {
        log.info("MCP Weather Server started");

        // 持續讀取來自 stdin 的請求
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        String line;

        while ((line = reader.readLine()) != null) {
            try {
                processRequest(line);
            } catch (Exception e) {
                log.error("Error processing request: ", e);
            }
        }
    }

    /**
     * 建立伺服器資訊
     */
    private ObjectNode createServerInfo() {
        ObjectNode serverInfo = objectMapper.createObjectNode();
        serverInfo.put("name", "weather");
        serverInfo.put("version", "1.0.0");
        return serverInfo;
    }

    /**
     * 建立能力描述
     */
    private ObjectNode createCapabilities() {
        ObjectNode capabilities = objectMapper.createObjectNode();

        ObjectNode tools = capabilities.putObject("tools");
        ArrayNode toolsList = tools.putArray("list");

        // 定義 get_forecast 工具
        ObjectNode getForecastTool = toolsList.addObject();
        getForecastTool.put("name", "get_forecast");
        getForecastTool.put("description", "查詢指定經緯度位置的天氣預報");

        ObjectNode forecastInputSchema = getForecastTool.putObject("inputSchema");
        forecastInputSchema.put("type", "object");
        ObjectNode forecastProperties = forecastInputSchema.putObject("properties");

        ObjectNode latitude = forecastProperties.putObject("latitude");
        latitude.put("type", "number");
        latitude.put("description", "緯度 (-90 到 90)");

        ObjectNode longitude = forecastProperties.putObject("longitude");
        longitude.put("type", "number");
        longitude.put("description", "經度 (-180 到 180)");

        ArrayNode forecastRequired = forecastInputSchema.putArray("required");
        forecastRequired.add("latitude");
        forecastRequired.add("longitude");

        // 定義 get_alerts 工具
        ObjectNode getAlertsTool = toolsList.addObject();
        getAlertsTool.put("name", "get_alerts");
        getAlertsTool.put("description", "查詢美國特定州的活動天氣警報");

        ObjectNode alertsInputSchema = getAlertsTool.putObject("inputSchema");
        alertsInputSchema.put("type", "object");
        ObjectNode alertsProperties = alertsInputSchema.putObject("properties");

        ObjectNode state = alertsProperties.putObject("state");
        state.put("type", "string");
        state.put("description", "美國州的兩字母縮寫（例如：CA, NY, TX）");

        ArrayNode alertsRequired = alertsInputSchema.putArray("required");
        alertsRequired.add("state");

        return capabilities;
    }

    /**
     * 處理來自 Claude Desktop 的請求
     */
    private void processRequest(String requestLine) {
        try {
            JsonNode request = objectMapper.readTree(requestLine);
            String method = request.get("method").asText();
            JsonNode id = request.get("id");

            if ("initialize".equals(method)) {
                handleInitialize(id);
            } else if ("tools/list".equals(method)) {
                handleToolsList(id);
            } else if ("tools/call".equals(method)) {
                handleToolsCall(id, request.get("params"));
            }
        } catch (Exception e) {
            log.error("Error processing request line: {}", requestLine, e);
        }
    }

    /**
     * 處理初始化請求
     */
    private void handleInitialize(JsonNode id) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("jsonrpc", "2.0");
            response.set("id", id);

            ObjectNode result = response.putObject("result");
            result.put("protocolVersion", "2024-11-05");
            result.set("serverInfo", createServerInfo());
            result.set("capabilities", createCapabilities());

            System.out.println(objectMapper.writeValueAsString(response));
            System.out.flush();
        } catch (Exception e) {
            log.error("Error handling initialize: ", e);
        }
    }

    /**
     * 處理工具列表請求
     */
    private void handleToolsList(JsonNode id) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("jsonrpc", "2.0");
            response.set("id", id);

            ObjectNode result = response.putObject("result");
            ArrayNode tools = result.putArray("tools");

            // get_forecast 工具
            ObjectNode getForecast = tools.addObject();
            getForecast.put("name", "get_forecast");
            getForecast.put("description", "查詢指定經緯度位置的天氣預報");
            ObjectNode forecastSchema = getForecast.putObject("inputSchema");
            forecastSchema.put("type", "object");
            ObjectNode forecastProps = forecastSchema.putObject("properties");
            forecastProps.putObject("latitude").put("type", "number");
            forecastProps.putObject("longitude").put("type", "number");
            forecastSchema.putArray("required").add("latitude").add("longitude");

            // get_alerts 工具
            ObjectNode getAlerts = tools.addObject();
            getAlerts.put("name", "get_alerts");
            getAlerts.put("description", "查詢美國特定州的活動天氣警報");
            ObjectNode alertsSchema = getAlerts.putObject("inputSchema");
            alertsSchema.put("type", "object");
            alertsSchema.putObject("properties").putObject("state").put("type", "string");
            alertsSchema.putArray("required").add("state");

            System.out.println(objectMapper.writeValueAsString(response));
            System.out.flush();
        } catch (Exception e) {
            log.error("Error handling tools/list: ", e);
        }
    }

    /**
     * 處理工具呼叫請求
     */
    private void handleToolsCall(JsonNode id, JsonNode params) {
        try {
            String toolName = params.get("name").asText();
            JsonNode arguments = params.get("arguments");

            String result;
            if ("get_forecast".equals(toolName)) {
                double latitude = arguments.get("latitude").asDouble();
                double longitude = arguments.get("longitude").asDouble();
                result = weatherService.getForecast(latitude, longitude);
            } else if ("get_alerts".equals(toolName)) {
                String state = arguments.get("state").asText();
                result = weatherService.getAlerts(state);
            } else {
                result = "Unknown tool: " + toolName;
            }

            ObjectNode response = objectMapper.createObjectNode();
            response.put("jsonrpc", "2.0");
            response.set("id", id);

            ObjectNode resultNode = response.putObject("result");
            ArrayNode content = resultNode.putArray("content");
            ObjectNode textContent = content.addObject();
            textContent.put("type", "text");
            textContent.put("text", result);

            System.out.println(objectMapper.writeValueAsString(response));
            System.out.flush();
        } catch (Exception e) {
            log.error("Error handling tools/call: ", e);
            sendErrorResponse(id, "Error executing tool: " + e.getMessage());
        }
    }

    /**
     * 發送錯誤回應
     */
    private void sendErrorResponse(JsonNode id, String message) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("jsonrpc", "2.0");
            response.set("id", id);

            ObjectNode error = response.putObject("error");
            error.put("code", -32603);
            error.put("message", message);

            System.out.println(objectMapper.writeValueAsString(response));
            System.out.flush();
        } catch (Exception e) {
            log.error("Error sending error response: ", e);
        }
    }
}
