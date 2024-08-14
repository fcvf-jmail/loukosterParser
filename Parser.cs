
using Microsoft.Playwright;

namespace LoukosterParser
{
    class Parser(IPage page, List<FlightInfo> infoToParse)
    {
        private IPage Page { get; set; } = page;
        private List<FlightInfo> InfoToParse { get; set; } = infoToParse;
        
        public async Task Parse()
        {
            foreach (FlightInfo flightInfo in InfoToParse) await flightInfo.Parse(Page);
        }
    }
}
