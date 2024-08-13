using System.Diagnostics.Contracts;

namespace LoukosterParser;

public class FlightVariant(string fromCityAbbreviation, string toCityAbbreviation, int numberOfPassangers, int startRangeDay, int endRangeDay, int month, decimal maxPrice)
{
    private string FromCityAbbreviation { get; set; } = fromCityAbbreviation;
    private string ToCityAbbreviation { get; set; } = toCityAbbreviation;
    private int NumberOfPassangers { get; set; } = numberOfPassangers;
    private int StartRangeDay { get; set; } = startRangeDay;
    private int EndRangeDay { get; set; } = endRangeDay;
    private string MonthStr { get; set; } = month.ToString().PadLeft(2, '0');
    public decimal MaxPrice { get; private set; } = maxPrice;

    public List<string> getUrls()
    {
        List<string> urls = [];
        for (int day = StartRangeDay; day <= EndRangeDay; day++)
        {
            string dayStr = day.ToString().PadLeft(2, '0');
            string url = $"https://avia.loukoster.com/flights/{FromCityAbbreviation}{dayStr}{MonthStr}{ToCityAbbreviation}{NumberOfPassangers}";
            urls.Add(url);
        }
        
        return urls;
    }
}