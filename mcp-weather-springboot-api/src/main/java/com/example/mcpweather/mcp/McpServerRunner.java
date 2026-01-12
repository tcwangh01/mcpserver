package com.example.mcpweather.mcp;

import com.example.mcpweather.service.WeatherService;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;

import java.io.BufferedReader;
import java.io.InputStreamReader;

/**
 * MCP Server Runner
 *
 * 透過 STDIO 與 Claude Desktop 通訊
 * 只在 mcp.mode=true 時啟動
 */
@Slf4j
@Component
@ConditionalOnProperty(name = "mcp.mode", havingValue = "true")
public class McpServerRunner implements CommandLineRunner {

    private final WeatherService weatherService;
    private final ObjectMapper objectMapper;

    public McpServerRunner(WeatherService weatherService) {
        this.weatherService = weatherService;
        this.objectMapper = new ObjectMapper();
    }

    @Override
    public void run(String... args) throws Exception {
        log.info("MCP Weather Server started (STDIO mode)");

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

    private void processRequest(String requestLine) {
        try {
            JsonNode request = objectMapper.readTree(requestLine);
            String method = request.get("method").asText();
            JsonNode id = request.get("id");

            switch (method) {
                case "initialize" -> handleInitialize(id);
                case "tools/list" -> handleToolsList(id);
                case "tools/call" -> handleToolsCall(id, request.get("params"));
                default -> log.warn("Unknown method: {}", method);
            }
        } catch (Exception e) {
            log.error("Error processing request: {}", requestLine, e);
        }
    }

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

    private ObjectNode createServerInfo() {
        ObjectNode serverInfo = objectMapper.createObjectNode();
        serverInfo.put("name", "weather-api");
        serverInfo.put("version", "1.0.0");
        return serverInfo;
    }

    private ObjectNode createCapabilities() {
        ObjectNode capabilities = objectMapper.createObjectNode();
        ObjectNode tools = capabilities.putObject("tools");
        ArrayNode toolsList = tools.putArray("list");

        // get_forecast tool
        ObjectNode getForecastTool = toolsList.addObject();
        getForecastTool.put("name", "get_forecast");
        getForecastTool.put("description", "查詢指定經緯度位置的天氣預報");
        ObjectNode forecastSchema = getForecastTool.putObject("inputSchema");
        forecastSchema.put("type", "object");
        ObjectNode forecastProps = forecastSchema.putObject("properties");
        forecastProps.putObject("latitude").put("type", "number").put("description", "緯度 (-90 到 90)");
        forecastProps.putObject("longitude").put("type", "number").put("description", "經度 (-180 到 180)");
        forecastSchema.putArray("required").add("latitude").add("longitude");

        // get_alerts tool
        ObjectNode getAlertsTool = toolsList.addObject();
        getAlertsTool.put("name", "get_alerts");
        getAlertsTool.put("description", "查詢美國特定州的活動天氣警報");
        ObjectNode alertsSchema = getAlertsTool.putObject("inputSchema");
        alertsSchema.put("type", "object");
        alertsSchema.putObject("properties").putObject("state").put("type", "string").put("description", "美國州代碼（如 CA, NY, TX）");
        alertsSchema.putArray("required").add("state");

        return capabilities;
    }

    private void handleToolsList(JsonNode id) {
        try {
            ObjectNode response = objectMapper.createObjectNode();
            response.put("jsonrpc", "2.0");
            response.set("id", id);

            ObjectNode result = response.putObject("result");
            ArrayNode tools = result.putArray("tools");

            // get_forecast
            ObjectNode getForecast = tools.addObject();
            getForecast.put("name", "get_forecast");
            getForecast.put("description", "查詢指定經緯度位置的天氣預報");
            ObjectNode forecastSchema = getForecast.putObject("inputSchema");
            forecastSchema.put("type", "object");
            ObjectNode forecastProps = forecastSchema.putObject("properties");
            forecastProps.putObject("latitude").put("type", "number");
            forecastProps.putObject("longitude").put("type", "number");
            forecastSchema.putArray("required").add("latitude").add("longitude");

            // get_alerts
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

    private void handleToolsCall(JsonNode id, JsonNode params) {
        try {
            String toolName = params.get("name").asText();
            JsonNode arguments = params.get("arguments");

            String result;
            if ("get_forecast".equals(toolName)) {
                double latitude = arguments.get("latitude").asDouble();
                double longitude = arguments.get("longitude").asDouble();
                result = weatherService.getForecastText(latitude, longitude);
            } else if ("get_alerts".equals(toolName)) {
                String state = arguments.get("state").asText();
                result = weatherService.getAlertsText(state);
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
