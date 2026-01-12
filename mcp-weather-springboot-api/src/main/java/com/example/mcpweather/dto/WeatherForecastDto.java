package com.example.mcpweather.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.util.List;

/**
 * REST API 天氣預報回應 DTO
 */
@Data
@NoArgsConstructor
@AllArgsConstructor
public class WeatherForecastDto {
    private double latitude;
    private double longitude;
    private List<ForecastPeriod> periods;

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ForecastPeriod {
        private String name;
        private Integer temperature;
        private String temperatureUnit;
        private String windSpeed;
        private String windDirection;
        private String detailedForecast;
    }
}
