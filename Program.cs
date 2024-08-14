using Microsoft.Playwright;
using LoukosterParser;
using NCrontab;
using DotNetEnv;

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

List<FlightVariant> flightVariants = [];
flightVariants.Add(new FlightVariant("CAI", "MOW", 1, 15, 20, 8, 30000));
flightVariants.Add(new FlightVariant("HRG", "MOW", 1, 15, 20, 8, 30000));

List<FlightInfo> infoToParse = [];

foreach (FlightVariant flightVariant in flightVariants)
{
    List<string> urls = flightVariant.getUrls();
    foreach(string url in urls)
    {
        infoToParse.Add(new FlightInfo(url, flightVariant.MaxPrice));
    }
}


const int timeoutInMinutes = 10;
string cronExpression = $"*/{timeoutInMinutes} * * * *"; // Каждые 10 минут
CrontabSchedule schedule = CrontabSchedule.Parse(cronExpression);
DateTime nextRun = schedule.GetNextOccurrence(DateTime.Now);

Timer timer = new (async _ =>
{
    using var pw = await Playwright.CreateAsync();
    await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{Headless=true});
    IPage page = await browser.NewPageAsync();
    Parser parser = new(page, infoToParse);
    await parser.Parse();
    await browser.CloseAsync();
    nextRun = schedule.GetNextOccurrence(DateTime.Now);
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(timeoutInMinutes));

while (true)
{
    Thread.Sleep(Timeout.Infinite);
}