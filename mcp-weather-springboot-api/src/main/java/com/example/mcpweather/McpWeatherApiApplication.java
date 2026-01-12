package com.example.mcpweather;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * MCP Weather API Application
 *
 * 提供兩種運行模式：
 * 1. HTTP API 模式（預設）：啟動 REST API server，提供 HTTP 端點
 * 2. MCP 模式：透過 STDIO 與 Claude Desktop 通訊
 *
 * 切換模式：
 * - HTTP API 模式：java -jar app.jar
 * - MCP 模式：java -jar app.jar --mcp.mode=true
 */
@SpringBootApplication
public class McpWeatherApiApplication {

    public static void main(String[] args) {
        SpringApplication.run(McpWeatherApiApplication.class, args);
    }
}
