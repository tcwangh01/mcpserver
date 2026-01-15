using System.ComponentModel;
using Microsoft.SemanticKernel;

/// <summary>
/// 天氣服務 Plugin
/// </summary>
public class WeatherService
{
    /// <summary>
    /// 取得指定城市的天氣資訊
    /// </summary>
    /// <param name="city">城市名稱</param>
    /// <returns>天氣資訊</returns>
    [KernelFunction, Description("取得指定城市的天氣資訊")]
    public string GetWeather(
        [Description("城市名稱，例如：台北、台中、高雄")] string city)
    {
        // 加入日誌追蹤，確認函數有被調用
        Console.WriteLine($"[WeatherService.GetWeather] 被調用，參數 city = {city}");

        // 這是一個模擬的天氣服務
        // 實際應用中，您可以呼叫真實的天氣 API
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

    /// <summary>
    /// 取得指定城市未來幾天的天氣預報
    /// </summary>
    /// <param name="city">城市名稱</param>
    /// <param name="days">預報天數</param>
    /// <returns>天氣預報資訊</returns>
    [KernelFunction, Description("取得指定城市未來幾天的天氣預報")]
    public string GetWeatherForecast(
        [Description("城市名稱，例如：台北、台中、高雄")] string city,
        [Description("預報天數，例如：3、5、7")] int days = 3)
    {
        return $"{city}未來{days}天的天氣預報：\n" +
               $"- 第1天：晴朗，25-30°C\n" +
               $"- 第2天：多雲，23-28°C\n" +
               $"- 第3天：陰天，22-26°C";
    }
}