namespace LoukosterParser;
class FlightInfo (string url, decimal maxPrice)
{
    public string Url { get; private set; } = url;
    public decimal MaxPrice { get; private set; } = maxPrice;
}