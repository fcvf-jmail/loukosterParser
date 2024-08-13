using Microsoft.Playwright;
using LoukosterParser;
using NCrontab;

List<FlightVariant> flightVariants = [];
flightVariants.Add(new FlightVariant("HRG", "MOW", 1, 19, 19, 8, 30000));

// flightVariants.Add(new FlightVariant("CAI", "MOW", 1, 15, 20, 8, 25000));
// flightVariants.Add(new FlightVariant("HRG", "MOW", 1, 15, 20, 8, 30000));

List<FlightInfo> infoToParse = [];

foreach (FlightVariant flightVariant in flightVariants)
{
    List<string> urls = flightVariant.getUrls();
    foreach(string url in urls)
    {
        infoToParse.Add(new FlightInfo(url, flightVariant.MaxPrice));
    }
}

using var pw = await Playwright.CreateAsync();


var cronExpression = "*/1 * * * *"; // Каждые 10 минут
var schedule = CrontabSchedule.Parse(cronExpression);
var nextRun = schedule.GetNextOccurrence(DateTime.Now);
var timer = new Timer(async _ =>
{
    await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{Headless=false});
    var page = await browser.NewPageAsync();
    Parser parser = new(page, infoToParse);
    await parser.Parse();
    await browser.CloseAsync();
    nextRun = schedule.GetNextOccurrence(DateTime.Now);
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

Console.WriteLine("Press [Enter] to exit.");
Console.ReadLine();