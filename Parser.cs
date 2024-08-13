using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using DotNetEnv;
using Microsoft.Playwright;

namespace LoukosterParser
{
    class Parser(IPage page, List<FlightInfo> infoToParse)
    {
        private IPage Page { get; set; } = page;
        private List<FlightInfo> InfoToParse { get; set; } = infoToParse;
        private static readonly string botToken = Env.GetString("TELEGRAM_BOT_TOKEN") ?? throw new ArgumentNullException("TELEGRAM_BOT_TOKEN");
        private readonly TelegramBotClient BotClient = new(botToken);

        static private async Task<decimal> GetPrice(IElementHandle buyButtonElement)
        {
            var priceElement = await buyButtonElement.QuerySelectorAsync(".ticket-action-button-deeplink-text__price--not-mobile");
            if(priceElement is null)
            {
                Console.WriteLine("Не удалось найти priceElement");
                return 0;
            }
            string priceStr = await priceElement.InnerTextAsync();
            Console.WriteLine($"Нашел priceStr: ${priceStr}");
            bool priceIsParsed = decimal.TryParse(priceStr, out decimal price);
            return priceIsParsed ? price : 0;
        }

        static private async Task<string> GetId(IElementHandle buyButtonElement)
        {
            string? id = await buyButtonElement.GetAttributeAsync("href");
            if (id is null) return "id not found";
            id = id.Split("/clicks/")[1];
            return id;
        }

        private async Task SendAlert(long chatId, string text)
        {
            await BotClient.SendTextMessageAsync(
            chatId,
            text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
        }
        public async Task Parse()
        {
            HashSet<string> sentIds = new(SentIds.Get());

            foreach (FlightInfo flightInfo in InfoToParse)
            {
                await Page.GotoAsync(flightInfo.Url);

                Console.WriteLine($"Открыл ссылку {flightInfo.Url}, жду загрузки прогресс бара");
                
                await Page.WaitForSelectorAsync("div.search_progressbar-container", new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Hidden
                });

                Console.WriteLine("Прогресс бар загрузился, нажимаю кнопку фильтрации по багажу");

                var withBaggageButton = await Page.QuerySelectorAsync("#baggage_filter_0");
                                
                if (withBaggageButton is null) continue;

                await withBaggageButton.ClickAsync(new ElementHandleClickOptions { Force = true });

                Console.WriteLine("Нажал кнопку, делаю скриншот");

                await Page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(Directory.GetCurrentDirectory(), "screen.png"), FullPage=true});
                
                Console.WriteLine("Сделал скриншот");

                var buyButtonElements = await Page.QuerySelectorAllAsync(".ticket-action-button-deeplink--");

                foreach (var buyButtonElement in buyButtonElements)
                {
                    decimal price = await GetPrice(buyButtonElement);

                    Console.WriteLine($"Нашел цену {price}");

                    if (price == 0 || price > flightInfo.MaxPrice) continue;

                    string id = await GetId(buyButtonElement);

                    if (id == "id not found" || sentIds.Contains(id)) continue;
                    
                    Console.WriteLine($"Нашел id {id} для цены ${price}");

                    await SendAlert(1386450473, $"<i>Найден подходящий билет</i>\nЦена: <b>{price}</b>\n<a href=\"{flightInfo.Url}\">Тык</a>");
                    
                    Console.WriteLine($"Отправил алерт в тг");

                    sentIds.Add(id);
                    SentIds.Write(sentIds);
                }
            }
        }
    }
}
