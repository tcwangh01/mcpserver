package com.example.mcpweather.service;

import com.example.mcpweather.dto.AlertsResponse;
import com.example.mcpweather.dto.ForecastResponse;
import com.example.mcpweather.dto.PointsResponse;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;

import java.time.Duration;
import java.util.stream.Collectors;

/**
 * MCP Weather Service - 美國天氣查詢服務
 *
 * 本服務整合美國國家氣象局 (National Weather Service) API，
 * 提供天氣預報和警報查詢功能。
 */
@Slf4j
@Service
public class WeatherService {

    private static final String NWS_API_BASE = "https://api.weather.gov";
    private static final String USER_AGENT = "weather-app/1.0";
    private static final Duration TIMEOUT = Duration.ofSeconds(30);

    private final WebClient webClient;

    public WeatherService() {
        this.webClient = WebClient.builder()
                .baseUrl(NWS_API_BASE)
                .defaultHeader("User-Agent", USER_AGENT)
                .defaultHeader("Accept", "application/geo+json")
                .build();
    }

    /**
     * 查詢指定經緯度位置的天氣預報
     *
     * 使用兩階段流程取得天氣預報：
     * 1. 根據經緯度查詢對應的預報網格點（grid point）
     * 2. 使用網格點資訊取得詳細的天氣預報
     *
     * @param latitude 緯度（-90 到 90）
     * @param longitude 經度（-180 到 180）
     * @return 格式化的天氣預報文字
     */
    public String getForecast(double latitude, double longitude) {
        try {
            log.info("getForecast latitude={}, longitude={}", latitude, longitude);

            // 步驟 1: 取得預報網格點資訊
            String pointsUrl = String.format("/points/%s,%s", latitude, longitude);

            PointsResponse pointsResponse = webClient.get()
                    .uri(pointsUrl)
                    .retrieve()
                    .bodyToMono(PointsResponse.class)
                    .timeout(TIMEOUT)
                    .blockOptional()
                    .orElse(null);

            if (pointsResponse == null || pointsResponse.getProperties() == null) {
                log.error("Unable to fetch forecast data for location: {}, {}", latitude, longitude);
                return "Unable to fetch forecast data for this location.";
            }

            String forecastUrl = pointsResponse.getProperties().getForecast();
            if (forecastUrl == null) {
                log.error("Forecast URL not found in response");
                return "Unable to retrieve forecast URL from weather service.";
            }

            // 步驟 2: 取得詳細預報資料
            ForecastResponse forecastResponse = webClient.get()
                    .uri(forecastUrl.replace(NWS_API_BASE, ""))
                    .retrieve()
                    .bodyToMono(ForecastResponse.class)
                    .timeout(TIMEOUT)
                    .blockOptional()
                    .orElse(null);

            if (forecastResponse == null ||
                forecastResponse.getProperties() == null ||
                forecastResponse.getProperties().getPeriods() == null) {
                log.error("No forecast data available");
                return "No forecast data available for this location.";
            }

            // 步驟 3: 格式化預報結果
            var periods = forecastResponse.getProperties().getPeriods();
            if (periods.isEmpty()) {
                return "No forecast data available for this location.";
            }

            return periods.stream()
                    .limit(5)
                    .map(period -> String.format("""
                            %s:
                            Temperature: %d %s
                            Wind: %s %s
                            Forecast: %s
                            """,
                            period.getName(),
                            period.getTemperature(),
                            period.getTemperatureUnit(),
                            period.getWindSpeed(),
                            period.getWindDirection(),
                            period.getDetailedForecast()
                    ))
                    .collect(Collectors.joining("\n---\n"));

        } catch (Exception e) {
            log.error("Error fetching forecast: ", e);
            return "Error fetching forecast: " + e.getMessage();
        }
    }

    /**
     * 查詢美國特定州的活動天氣警報
     *
     * 連接到 NWS API 取得指定州的所有活動警報，
     * 並將警報資訊格式化為易讀的文字。
     *
     * @param state 美國州的兩字母縮寫（例如：CA=加州, NY=紐約州, TX=德州）
     * @return 格式化的警報資訊文字
     */
    public String getAlerts(String state) {
        try {
            log.info("getAlerts state={}", state);

            String alertsUrl = String.format("/alerts/active/area/%s", state.toUpperCase());

            AlertsResponse alertsResponse = webClient.get()
                    .uri(alertsUrl)
                    .retrieve()
                    .bodyToMono(AlertsResponse.class)
                    .timeout(TIMEOUT)
                    .blockOptional()
                    .orElse(null);

            if (alertsResponse == null || alertsResponse.getFeatures() == null) {
                log.error("Unable to fetch alerts for state: {}", state);
                return String.format("Unable to fetch alerts for %s. The NWS API may be unavailable.", state);
            }

            var features = alertsResponse.getFeatures();
            if (features.isEmpty()) {
                return String.format("No active alerts for %s.", state);
            }

            return features.stream()
                    .map(feature -> {
                        var props = feature.getProperties();
                        return String.format("""

                                Event: %s
                                Area: %s
                                Severity: %s
                                Description: %s
                                Instructions: %s
                                """,
                                props.getEvent() != null ? props.getEvent() : "Unknown",
                                props.getAreaDesc() != null ? props.getAreaDesc() : "Unknown",
                                props.getSeverity() != null ? props.getSeverity() : "Unknown",
                                props.getDescription() != null ? props.getDescription() : "No description available",
                                props.getInstruction() != null ? props.getInstruction() : "No specific instructions provided"
                        );
                    })
                    .collect(Collectors.joining("\n--\n"));

        } catch (Exception e) {
            log.error("Error fetching alerts: ", e);
            return "Error fetching alerts: " + e.getMessage();
        }
    }
}
