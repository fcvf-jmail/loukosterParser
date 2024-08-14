using Microsoft.Playwright;
using Telegram.Bot;
using DotNetEnv;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LoukosterParser;
class FlightInfo (string url, decimal maxPrice)
{
    public string Url { get; private set; } = url;
    public decimal MaxPrice { get; private set; } = maxPrice;

    private static readonly string botToken = Env.GetString("TELEGRAM_BOT_TOKEN") ?? throw new ArgumentNullException("TELEGRAM_BOT_TOKEN");
    private readonly TelegramBotClient BotClient = new(botToken);

    static private async Task<decimal> GetPrice(IElementHandle ticketElement)
    {
        var priceElement = await ticketElement.QuerySelectorAsync(".ticket-action-button-deeplink-text__price--not-mobile");
        if(priceElement is null)
        {
            Console.WriteLine("Не удалось найти priceElement");
            return 0;
        }
        string priceStr = await priceElement.InnerTextAsync();
        Console.WriteLine($"Нашел priceStr: ${priceStr}");
        string priceWithOnlyDigits = Regex.Replace(priceStr, @"\D", "");
        Console.WriteLine($"Убрал все не цифры: ${priceWithOnlyDigits}");
        bool priceIsParsed = decimal.TryParse(priceWithOnlyDigits, out decimal price);
        return priceIsParsed ? price : 0;
    }

    static private async Task<string> GetHref(IElementHandle buyButtonElement)
    {
        string? href = await buyButtonElement.GetAttributeAsync("href");
        if (href is null) return "href not found";
        href = $"https://avia.loukoster.com/{href}";
        return href;
    }
    static private async Task<string> GetId(IElementHandle buyButtonElement, decimal price)
    {
        string? id = await buyButtonElement.GetAttributeAsync("href");
        if (id is null) return "id not found";
        id = id.Split("/clicks/")[1] + price;
        return id;
    }

    private async Task SendAlert(long chatId, string caption, string photoPath)
    {
        using var stream = new FileStream(photoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await BotClient.SendPhotoAsync(
            chatId: chatId,
            photo: Telegram.Bot.Types.InputFile.FromStream(stream),
            caption: caption,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }

    private async Task<(decimal price, string id, string href)> GetTicketInfo(IElementHandle ticketElement, IPage page)
    {
        (decimal, string, string) smthMissedTupple = (0, "id not found", "href not found");;

        decimal price = await GetPrice(ticketElement);
        Console.WriteLine($"Нашел цену {price}");

        if (price == 0 || price > MaxPrice) return smthMissedTupple;

        var buyButtonElement = await page.QuerySelectorAsync(".ticket-action-button-deeplink--");

        if(buyButtonElement is null) return smthMissedTupple;

        string id = await GetId(buyButtonElement, price);

        string href = await GetHref(buyButtonElement);

        return (price, id, href);
    }

    private async Task useBaggageFilter(IPage page)
    {
        var withBaggageButton = await page.QuerySelectorAsync("#baggage_filter_0");
        
        if (withBaggageButton is null) return;

        await withBaggageButton.ClickAsync(new ElementHandleClickOptions { Force = true });
    }

    private async Task<IReadOnlyList<IElementHandle>?> GetTicketElements(IPage page)
    {
        var ticketsContrainer = await page.QuerySelectorAsync("div[role=\"tickets_container\"]");
        if (ticketsContrainer is null) return null;
        var ticketElements = await ticketsContrainer.QuerySelectorAllAsync("div.ticket");
        return ticketElements;
    }

    private async Task ProcessAndAlertTicket(IElementHandle ticketElement, IPage page)
    {
        HashSet<string> sentIds = new(SentIds.Get());
        (decimal price, string id, string urlToBuy) = await GetTicketInfo(ticketElement, page);

        if (id == "id not found" || sentIds.Contains(id)) return;

        Console.WriteLine($"Нашел id {id} для цены ${price}");

        string photoPath = Path.Combine(Directory.GetCurrentDirectory(), "ticket.png");

        await ticketElement.ScreenshotAsync(new ElementHandleScreenshotOptions{Path=photoPath});

        Console.WriteLine("Сделал скриншот билета");

        await SendAlert(1386450473, $"<i>Найден подходящий билет</i>\nЦена: <b>{price}</b>\n<a href=\"{Url}\">Страница с билетами</a>\n\n<a href=\"{urlToBuy}\">Купить</a>", photoPath);

        Console.WriteLine($"Отправил алерт в тг");

        sentIds.Add(id);
        SentIds.Write(sentIds);
    }
    
    public async Task Parse(IPage page)
    {
        await page.GotoAsync(Url);

        Console.WriteLine($"Открыл ссылку {Url}, жду загрузки прогресс бара");
        
        await page.WaitForSelectorAsync("div.search_progressbar-container", new PageWaitForSelectorOptions{State = WaitForSelectorState.Hidden});

        Console.WriteLine("Прогресс бар загрузился, нажимаю кнопку фильтрации по багажу");

        await useBaggageFilter(page);

        Console.WriteLine("Нажал кнопку, делаю скриншот");

        await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(Directory.GetCurrentDirectory(), "screen.png"), FullPage=true});
        
        Console.WriteLine("Сделал полный скриншот страницы");

        var ticketElements = await GetTicketElements(page);

        if (ticketElements is null) return;

        foreach (IElementHandle ticketElement in ticketElements) await ProcessAndAlertTicket(ticketElement, page);
    }
}