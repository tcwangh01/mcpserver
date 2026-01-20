using System.ComponentModel;
using Microsoft.SemanticKernel;

public class WeatherService
{
    [KernelFunction, Description("取得指定城市的天氣資訊")]
    public string GetWeather(
        [Description("城市名稱，例如：台北、台中、高雄")] string city)
    {
        Console.WriteLine($"[WeatherService.GetWeather] 被調用，參數 city = {city}");

        var weather = city switch
        {
            "台北" => "台北今天晴朗，溫度 25°C，濕度 60%",
            "台中" => "台中今天多雲，溫度 28°C，濕度 55%",
            "高雄" => "高雄今天晴朗，溫度 31°C，濕度 70%",
            _ => $"{city}今天天氣晴朗，溫度 26°C"
        };

        Console.WriteLine($"[WeatherService.GetWeather] 回傳結果：{weather}");
        return weather;
    }

    [KernelFunction, Description("取得指定城市未來幾天的天氣預報")]
    public string GetWeatherForecast(
        [Description("城市名稱，例如：台北、台中、高雄")] string city,
        [Description("預報天數，例如：3、5、7")] int days = 3)
    {
        Console.WriteLine($"[WeatherService.GetWeatherForecast] 被調用，參數 city = {city}, days = {days}");

        var forecast = $"{city}未來{days}天的天氣預報：\n" +
               $"- 第1天：晴朗，25-30°C\n" +
               $"- 第2天：多雲，23-28°C\n" +
               $"- 第3天：陰天，22-26°C";

        Console.WriteLine($"[WeatherService.GetWeatherForecast] 回傳結果：{forecast}");
        return forecast;
    }
}
