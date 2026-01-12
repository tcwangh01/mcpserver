package com.example.mcpweather.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.util.List;

/**
 * REST API 天氣警報回應 DTO
 */
@Data
@NoArgsConstructor
@AllArgsConstructor
public class WeatherAlertDto {
    private String state;
    private List<Alert> alerts;

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class Alert {
        private String event;
        private String areaDesc;
        private String severity;
        private String description;
        private String instruction;
    }
}
