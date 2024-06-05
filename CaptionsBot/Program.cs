using CaptionsBot;
using Telegram.Bot;
using Microsoft.Extensions.Logging;

ITelegramBotClient botClient = new TelegramBotClient(Constants.TELEGRAM_BOT_TOKEN);
/*
//ILogger<TelegramBot> logger;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure logging
builder.Logging.AddConsole();

var app = builder.Build();
*/

TelegramBot telegramBot = new TelegramBot(botClient);
telegramBot.Start().Wait();
Console.ReadLine();

/*
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("{YOUR_ACCESS_TOKEN_HERE}");

using CancellationTokenSource cts = new ();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new ()
{
AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
updateHandler: HandleUpdateAsync,
pollingErrorHandler: HandlePollingErrorAsync,
receiverOptions: receiverOptions,
cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
// Only process Message updates: https://core.telegram.org/bots/api#message
if (update.Message is not { } message)
return;
// Only process text messages
if (message.Text is not { } messageText)
return;

var chatId = message.Chat.Id;

Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

// Echo received message text
Message sentMessage = await botClient.SendTextMessageAsync(
chatId: chatId,
text: "You said:\n" + messageText,
cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
var ErrorMessage = exception switch
{
ApiRequestException apiRequestException
    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
_ => exception.ToString()
};

Console.WriteLine(ErrorMessage);
return Task.CompletedTask;
}



*/
