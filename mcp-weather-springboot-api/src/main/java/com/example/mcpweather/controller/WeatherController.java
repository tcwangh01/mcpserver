package com.example.mcpweather.controller;

import com.example.mcpweather.dto.WeatherAlertDto;
import com.example.mcpweather.dto.WeatherForecastDto;
import com.example.mcpweather.service.WeatherService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

/**
 * 天氣 REST API Controller
 *
 * 提供 HTTP 端點供外部呼叫
 */
@Slf4j
@RestController
@RequestMapping("/api/weather")
public class WeatherController {

    private final WeatherService weatherService;

    public WeatherController(WeatherService weatherService) {
        this.weatherService = weatherService;
    }

    /**
     * GET /api/weather/forecast?lat={latitude}&lon={longitude}
     *
     * 查詢指定經緯度的天氣預報
     *
     * @param latitude 緯度 (-90 到 90)
     * @param longitude 經度 (-180 到 180)
     * @return 天氣預報資料
     */
    @GetMapping("/forecast")
    public ResponseEntity<WeatherForecastDto> getForecast(
            @RequestParam("lat") double latitude,
            @RequestParam("lon") double longitude) {

        log.info("REST API: getForecast lat={}, lon={}", latitude, longitude);

        WeatherForecastDto forecast = weatherService.getForecastData(latitude, longitude);

        if (forecast == null) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.ok(forecast);
    }

    /**
     * GET /api/weather/alerts?state={state}
     *
     * 查詢指定州的天氣警報
     *
     * @param state 美國州代碼（如 CA, NY, TX）
     * @return 天氣警報資料
     */
    @GetMapping("/alerts")
    public ResponseEntity<WeatherAlertDto> getAlerts(@RequestParam("state") String state) {

        log.info("REST API: getAlerts state={}", state);

        WeatherAlertDto alerts = weatherService.getAlertsData(state);

        if (alerts == null) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.ok(alerts);
    }

    /**
     * GET /api/weather/health
     *
     * 健康檢查端點
     *
     * @return 服務狀態
     */
    @GetMapping("/health")
    public ResponseEntity<String> health() {
        return ResponseEntity.ok("Weather API is running");
    }
}
