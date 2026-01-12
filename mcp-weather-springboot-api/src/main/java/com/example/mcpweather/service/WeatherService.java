package com.example.mcpweather.service;

import com.example.mcpweather.dto.*;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;

import java.time.Duration;
import java.util.stream.Collectors;

/**
 * 天氣服務 - 提供天氣預報和警報查詢功能
 * 同時支援 REST API 和 MCP Server
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
     * 查詢天氣預報 - 返回結構化資料（用於 REST API）
     */
    public WeatherForecastDto getForecastData(double latitude, double longitude) {
        try {
            log.info("getForecastData latitude={}, longitude={}", latitude, longitude);

            String pointsUrl = String.format("/points/%s,%s", latitude, longitude);
            PointsResponse pointsResponse = webClient.get()
                    .uri(pointsUrl)
                    .retrieve()
                    .bodyToMono(PointsResponse.class)
                    .timeout(TIMEOUT)
                    .blockOptional()
                    .orElse(null);

            if (pointsResponse == null || pointsResponse.getProperties() == null) {
                log.error("Unable to fetch forecast data");
                return null;
            }

            String forecastUrl = pointsResponse.getProperties().getForecast();
            if (forecastUrl == null) {
                log.error("Forecast URL not found");
                return null;
            }

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
                return null;
            }

            var periods = forecastResponse.getProperties().getPeriods().stream()
                    .limit(5)
                    .map(p -> new WeatherForecastDto.ForecastPeriod(
                            p.getName(),
                            p.getTemperature(),
                            p.getTemperatureUnit(),
                            p.getWindSpeed(),
                            p.getWindDirection(),
                            p.getDetailedForecast()
                    ))
                    .collect(Collectors.toList());

            return new WeatherForecastDto(latitude, longitude, periods);

        } catch (Exception e) {
            log.error("Error fetching forecast data: ", e);
            return null;
        }
    }

    /**
     * 查詢天氣預報 - 返回格式化文字（用於 MCP）
     */
    public String getForecastText(double latitude, double longitude) {
        WeatherForecastDto data = getForecastData(latitude, longitude);
        if (data == null || data.getPeriods() == null || data.getPeriods().isEmpty()) {
            return "Unable to fetch forecast data for this location.";
        }

        return data.getPeriods().stream()
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
    }

    /**
     * 查詢天氣警報 - 返回結構化資料（用於 REST API）
     */
    public WeatherAlertDto getAlertsData(String state) {
        try {
            log.info("getAlertsData state={}", state);

            String alertsUrl = String.format("/alerts/active/area/%s", state.toUpperCase());
            AlertsResponse alertsResponse = webClient.get()
                    .uri(alertsUrl)
                    .retrieve()
                    .bodyToMono(AlertsResponse.class)
                    .timeout(TIMEOUT)
                    .blockOptional()
                    .orElse(null);

            if (alertsResponse == null || alertsResponse.getFeatures() == null) {
                log.error("Unable to fetch alerts");
                return null;
            }

            var alerts = alertsResponse.getFeatures().stream()
                    .map(feature -> {
                        var props = feature.getProperties();
                        return new WeatherAlertDto.Alert(
                                props.getEvent(),
                                props.getAreaDesc(),
                                props.getSeverity(),
                                props.getDescription(),
                                props.getInstruction()
                        );
                    })
                    .collect(Collectors.toList());

            return new WeatherAlertDto(state.toUpperCase(), alerts);

        } catch (Exception e) {
            log.error("Error fetching alerts: ", e);
            return null;
        }
    }

    /**
     * 查詢天氣警報 - 返回格式化文字（用於 MCP）
     */
    public String getAlertsText(String state) {
        WeatherAlertDto data = getAlertsData(state);
        if (data == null || data.getAlerts() == null) {
            return String.format("Unable to fetch alerts for %s.", state);
        }

        if (data.getAlerts().isEmpty()) {
            return String.format("No active alerts for %s.", state);
        }

        return data.getAlerts().stream()
                .map(alert -> String.format("""

                        Event: %s
                        Area: %s
                        Severity: %s
                        Description: %s
                        Instructions: %s
                        """,
                        alert.getEvent() != null ? alert.getEvent() : "Unknown",
                        alert.getAreaDesc() != null ? alert.getAreaDesc() : "Unknown",
                        alert.getSeverity() != null ? alert.getSeverity() : "Unknown",
                        alert.getDescription() != null ? alert.getDescription() : "No description",
                        alert.getInstruction() != null ? alert.getInstruction() : "No instructions"
                ))
                .collect(Collectors.joining("\n--\n"));
    }
}
