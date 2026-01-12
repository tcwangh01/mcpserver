#!/bin/bash

echo "Testing MCP Weather Server..."
echo ""

# 測試 initialize
echo "1. Testing initialize request..."
echo '{"jsonrpc":"2.0","method":"initialize","id":1,"params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | java -jar target/mcp-weather-spring-boot-1.0.0.jar &
PID=$!
sleep 3
kill $PID 2>/dev/null

echo ""
echo "2. Check log file..."
if [ -f "mcp-weather.log" ]; then
    echo "Last 10 lines from mcp-weather.log:"
    tail -10 mcp-weather.log
else
    echo "Log file not found"
fi
