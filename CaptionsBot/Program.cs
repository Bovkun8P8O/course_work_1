using CaptionsBot;
using Telegram.Bot;

ITelegramBotClient botClient = new TelegramBotClient(Constants.TELEGRAM_BOT_TOKEN);

TelegramBot telegramBot = new TelegramBot(botClient);
telegramBot.Start().Wait();
Console.ReadLine();
