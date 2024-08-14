using Microsoft.Playwright;
using LoukosterParser;
using NCrontab;
using DotNetEnv;

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

List<FlightVariant> flightVariants =
[
    new FlightVariant("CAI", "MOW", 1, 15, 20, 8, 30000),
    new FlightVariant("HRG", "MOW", 1, 15, 20, 8, 30000)
];

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

Timer timer = new (async _ =>
{
    using var pw = await Playwright.CreateAsync();
    bool headless = Env.GetBool("BROWSER_HEADLESS");
    await using var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
    
    IPage page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1920, Height = 1080 } });
    
    Parser parser = new(page, infoToParse);
    await parser.Parse();

    await browser.CloseAsync();
}, null, TimeSpan.Zero, TimeSpan.FromMinutes(timeoutInMinutes));


while (true)
{
    Thread.Sleep(Timeout.Infinite);
}