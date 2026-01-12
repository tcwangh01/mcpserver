package com.example.mcpweather.dto;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;
import java.util.List;

@Data
@JsonIgnoreProperties(ignoreUnknown = true)
public class ForecastResponse {
    private Properties properties;

    @Data
    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class Properties {
        private List<Period> periods;
    }

    @Data
    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class Period {
        private String name;
        private Integer temperature;
        private String temperatureUnit;
        private String windSpeed;
        private String windDirection;
        private String detailedForecast;
    }
}
