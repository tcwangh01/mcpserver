#!/bin/bash

# 啟動 MCP Server（STDIO 模式）
echo "Starting MCP Weather Server (MCP mode)..."
echo "This mode is for Claude Desktop integration"
echo "Logs will be written to ~/mcp-weather-api.log"
echo ""

java -jar target/mcp-weather-springboot-api-1.0.0.jar --spring.profiles.active=mcp
