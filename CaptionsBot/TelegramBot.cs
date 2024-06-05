using CaptionsBot.Database;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CaptionsBot
{
    public class TelegramBot
    {
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        Users users = new Users();
        private static HttpClient _httpClient;
        private static string _apiUrl;
        private readonly ITelegramBotClient _botClient;
        private static Dictionary<long, string>? _userStates;
        private static List<CaptionsRequestParameters>? _userParameters;

        public TelegramBot(ITelegramBotClient botClient)
        {
            _botClient = botClient;
            _apiUrl = Constants.MY_API_URL;
            _userStates = new Dictionary<long, string>();
            _userParameters = new List<CaptionsRequestParameters>();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_apiUrl);
        }


        public async Task Start()
        {
            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandlePollingErrorAsync), receiverOptions, cancellationToken);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            // Cooldown in case of network connection error
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
            {
                return;
            }
            // реєстрація користувача в базі, якщо його там немає
            var Message = update.Message;
            var chatId = Message.Chat.Id.ToString(); // однаковий з userId
            var userId = Message.From.Id.ToString();
            string? username = Message.From.Username;
            if (username == null)
            {
                username = Message.From.FirstName + Message.From.LastName;
            }
            if (!await users.UserExistsAsync(userId)) // chatId
            {
                await users.AddUserAsync(userId, username); // chatId
            }
            Console.WriteLine($"Received a '{Message.Type}' message in chat {chatId} from {userId} ({username}).");

            // створення об'єкту CaptionsRequestParameters для зберігання параметрів запиту (змінювати наявний, якщо є)
            if (_userParameters.Find(x => x.UserID == userId) == null)
            {
                _userParameters.Add(new CaptionsRequestParameters(userId, "", "", "", ""));
            }

            // перевірка отримання ботом повідомлення
            //if (Message.Text != null)
            //{
            //    await botClient.SendTextMessageAsync(Message.Chat.Id, "Received: " + Message.Text);
            //}
            var handler = update switch
            {
                { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update, cancellationToken)
            };

            await handler;
        }

        async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Text is not { } messageText)
            {
                return;
            }


            // обробка скасування задачі та станів користувачів
            if (messageText.StartsWith("/cancel"))
            {
                _userStates[message.Chat.Id] = null;                            // зміна стану
                await HandleCancel(_botClient, message, cancellationToken);
            }
            Console.WriteLine("Got through cancel");

            _userStates.TryGetValue(message.Chat.Id, out string? userState);

            if (userState == "waiting for video ID")
            {
                _userStates[message.Chat.Id] = null;                            // зміна стану
                string videoID = message.Text;
                // додавання videoID до параметрів запиту даного користувача
                _userParameters.Find(x => x.UserID == message.Chat.Id.ToString()).VideoID = videoID;
                await HandleVideoID(_botClient, videoID, message, cancellationToken);
            }
            Console.WriteLine("Got through video ID");

            if (userState == "waiting for language")
            {
                _userStates[message.Chat.Id] = null;                            // зміна стану
                await HandleLanguageChoice(_botClient, message, cancellationToken);
            }
            Console.WriteLine("Got through language choice");

            if (userState == "UPDATE waiting for subs from api")
            {
                _userStates[message.Chat.Id] = null;                            // зміна стану
                string[] languages = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string translationLanguage = languages[0];
                string localeLanguage = Constants.DEFAULT_LOCALE;
                if (languages.Length > 1) localeLanguage = languages[1];

                CaptionsRequestParameters thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.Lang = localeLanguage;
                thisUserParams.TargetLang = translationLanguage;

                // запит на оновлення субтитрів у апі
                await UpdateSubtitles(_botClient, message, thisUserParams, cancellationToken);
            }
            Console.WriteLine("Got through update subs");

            if (userState == "waiting for subs from api")
            {
                _userStates[message.Chat.Id] = null;                            // зміна стану
                string[] languages = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string translationLanguage = languages[0];
                string localeLanguage = Constants.DEFAULT_LOCALE;
                if (languages.Length > 1) localeLanguage = languages[1];

                CaptionsRequestParameters thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.Lang = localeLanguage;
                thisUserParams.TargetLang = translationLanguage;

                // запит на створення субтитрів у апі
                await GetSubtitlesFromAPI(_botClient, message, thisUserParams, cancellationToken);
            }
            Console.WriteLine("Got through get subs");

            // обробка доступних користувачу команд
            var action = messageText.Split(' ')[0] switch
            {
                // метод відправки вітань та інструкцій
                "/start" => SendGreetings(_botClient, message, cancellationToken),
                // метод прийому videoID чи посилання на відео
                "/new_subtitles" => HandleNewSubtitles(_botClient, message, cancellationToken),
                // метод відправки вибору формату субтитрів
                "/subtitles_OK" => HandleFormat(_botClient, message, cancellationToken),
                // методи обробки вибору формату
                "/justText" => HandleJSON(_botClient, message, cancellationToken),
                "/withTime" => HandleSRT(_botClient, message, cancellationToken),
                // метод оновлення субтитрів (якщо те ж відео, але інша мова перекладу та/або формат)
                "/update_subtitles" => HandleUpdateSubtitles(_botClient, message, cancellationToken),
                // метод видалення історії користування ботом
                "/delete_history" => HandleDeleteHistory(_botClient, message, cancellationToken),
                //"/throw" => FailingHandler(_botClient, message, cancellationToken),
                "/help" => Usage(_botClient, message, cancellationToken),
                _ => DoNothing(_botClient, message, cancellationToken),
            };
            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

            // Відправлення вітань та інструкцій
            static async Task<Message> SendGreetings(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Hi. " +
                    "\r\nHere is the list of available commands: " +
                    "\r\n/start - Greeting and showing initially available commands." +
                    "\r\n/help - Showing a list of all commands with descriptions." +
                    "\r\n/new_subtitles - Asking for video ID, checking video availability and asking to confirm video choice." +
                    "\r\n/delete_history - Deleting all of your usage history from database." +
                    "\r\n/cancel - Cancelling current process (history deletion is not cancellable) and deleting saved parameters.",
                    cancellationToken: cancellationToken);
            }

            // обробка скасування задачі
            static async Task<Message> HandleCancel(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                _userStates[message.Chat.Id] = null;                                             // зміна стану
                Console.WriteLine($"The task for {message.Chat.Id} was cancelled.");
                CaptionsRequestParameters thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.VideoID = "";
                thisUserParams.Lang = "";
                thisUserParams.TargetLang = "";
                thisUserParams.Format = "";
                Console.WriteLine($"Parameters for {message.Chat.Id} were deleted.");
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "The task was canceled.",
                    cancellationToken: cancellationToken);
            }

            // запит videoID чи посилання на відео після отримання команди /new_subtitles
            static async Task<Message> HandleNewSubtitles(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                _userStates[message.Chat.Id] = "waiting for video ID";                          // зміна стану
                CaptionsRequestParameters thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.VideoID = "";
                thisUserParams.Lang = "";
                thisUserParams.TargetLang = "";
                thisUserParams.Format = "";
                thisUserParams.isUpdate = false;
                Console.WriteLine($"User {message.Chat.Id} is waiting for video ID.");
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Please enter the video ID or link to the video.",
                        cancellationToken: cancellationToken);
            }

            // перевірка ІД
            static async Task<Message> HandleVideoID(ITelegramBotClient botClient, string videoID, Message message, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    chatId: message.Chat.Id,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);

                Console.WriteLine($"Received a '{message.Text}' message in chat {message.Chat.Id} from {message.Chat.Id} ({message.From.Username}).");

                Console.WriteLine("In method HandleVideoID");

                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($"{_apiUrl}/Subtitles/GetVideoName/?videoId={videoID}"),
                    };
                    Console.WriteLine("Sent GET request.");

                    string response = "";
                    using (var responseMessage = await _httpClient.SendAsync(request, cancellationToken))
                    {
                        responseMessage.EnsureSuccessStatusCode();

                        response = await responseMessage.Content.ReadAsStringAsync();
                    }
                    Console.WriteLine("Got GET response.");

                    if (response == "ID_Error: Invalid ID. Video is not available.")
                    {
                        return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: response + "\nPlease enter correct link/ID or choose another video with /new_subtitles.",
                            cancellationToken: cancellationToken);
                    }

                    if (response.StartsWith("Error: Something wrong happened."))
                    {
                        response.Remove(response.Length - 1);
                        return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: response,
                            cancellationToken: cancellationToken);
                    }

                    return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Video information:\n" + response + "\nEnter /subtitles_OK to continue\n\nor /new_subtitles to choose another video.",
                            cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Some error occurred. Please try again later.",
                            cancellationToken: cancellationToken);
                }
            }

            // вибір формату
            static async Task<Message> HandleFormat(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                Console.WriteLine($"{message.Chat.Id} is choosing format.");
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Choose an option with a command:" +
                        "\n/justText - Get only text from subtitles." +
                        "\n/withTime - Get subtitles with timestamps.",
                        cancellationToken: cancellationToken);
            }

            // збереження вибраного формату
            static async Task<Message> HandleJSON(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                _userStates[message.Chat.Id] = "waiting for language";               // зміна стану
                var thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.Format = "json";
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "You chose \"justText\" - Get only text from subtitles." +
                        "\nType \"OK\" to continue.",
                        cancellationToken: cancellationToken);
            }

            static async Task<Message> HandleSRT(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                _userStates[message.Chat.Id] = "waiting for language";               // зміна стану
                var thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                thisUserParams.Format = "srt";
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "You chose \"withTime\" - Get subtitles with timestamps." +
                        "\nType \"OK\" to continue.",
                        cancellationToken: cancellationToken);
            }

            // отримання субтитрів з апі
            static async Task<Message> GetSubtitlesFromAPI(ITelegramBotClient botClient, Message message, CaptionsRequestParameters thisUserParams, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    chatId: message.Chat.Id,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);

                string format = thisUserParams.Format.ToUpper();
                string userId = thisUserParams.UserID.ToString();
                string videoId = thisUserParams.VideoID;
                string lang = thisUserParams.Lang;
                string targetLang = thisUserParams.TargetLang;
                string response = "";

                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri($"{_apiUrl}/Subtitles/PostSubtitles{format}/" +
                        $"?userId={userId}&videoId={videoId}&lang={lang}&targetLang={targetLang}"),
                    };
                    Console.WriteLine("Sent POST request.");

                    using (var responseMessage = await _httpClient.SendAsync(request, cancellationToken))
                    {
                        responseMessage.EnsureSuccessStatusCode();
                        response = await responseMessage.Content.ReadAsStringAsync();
                    }
                    Console.WriteLine("Got POST response.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Some error occurred. Please try again later using /new_subtitles.",
                            cancellationToken: cancellationToken);
                }

                _userStates[message.Chat.Id] = "waiting for file";                            // зміна стану

                /*return await botClient.SendTextMessageAsync( // на випадок проблеми з файлами
                    chatId: message.Chat.Id,
                    text: response,
                    cancellationToken: cancellationToken);*/

                // створення файлу з субтитрами
                string subsFormat = (format == "JSON") ? "just_text" : "with_timestamps";
                string fileName = $"subtitles_{videoId}_{lang}_{targetLang}_{subsFormat}.txt"; // шлях до файлу у константи, коли ясно буде, чи на хості, чи без нього
                string filePath = $"YTCaptionsFiles/{message.Chat.Id}/{fileName}"; // %userprofile%\Documents\GitHub\CaptionsBot\bin\Debug\net6.0\YTCaptionsFiles
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Creating file...",
                    cancellationToken: cancellationToken);
                try
                {
                    System.IO.Directory.CreateDirectory($"YTCaptionsFiles/{message.Chat.Id}");
                    System.IO.File.WriteAllText(filePath, response);

                    using (Stream stream = System.IO.File.OpenRead(filePath))
                    {
                        return await botClient.SendDocumentAsync(
                            chatId: message.Chat.Id,
                            document: InputFile.FromStream(stream, fileName),
                            caption: $"Subtitles for video {videoId}." +
                            $"\nEnter /update_subtitles if you want to change format and/or language.",
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Error creating file. Please try again with /new_subtitles.",
                        cancellationToken: cancellationToken);
                }
            }

            // обробка запиту на оновлення субтитрів
            static async Task<Message> HandleUpdateSubtitles(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                _userParameters.Find(x => x.UserID == message.Chat.Id.ToString()).isUpdate = true;
                return await HandleFormat(botClient, message, cancellationToken);
            }

            // оновлення субтитрів
            static async Task<Message> UpdateSubtitles(ITelegramBotClient botClient, Message message, CaptionsRequestParameters thisUserParams, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    chatId: message.Chat.Id,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);

                string format = thisUserParams.Format;
                string userId = thisUserParams.UserID.ToString();
                string videoId = thisUserParams.VideoID;
                string lang = thisUserParams.Lang;
                string targetLang = thisUserParams.TargetLang;
                string response = "";

                try
                {
                    // PUT: Subtitles/PutSubtitles/?userId=userId&videoId=videoId&lang=lang&targetLang=targetLang&fileType=fileType
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri($"{_apiUrl}/Subtitles/PutSubtitles/" +
                        $"?userId={userId}&videoId={videoId}&lang={lang}&targetLang={targetLang}&fileType={format}"),
                    };
                    Console.WriteLine("Sent PUT request.");

                    using (var responseMessage = await _httpClient.SendAsync(request, cancellationToken))
                    {
                        responseMessage.EnsureSuccessStatusCode();
                        response = await responseMessage.Content.ReadAsStringAsync();
                    }
                    Console.WriteLine("Got PUT response.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    return await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Some error occurred. Please try again later using /update_subtitles or /new_subtitles.",
                            cancellationToken: cancellationToken);
                }
                _userStates[message.Chat.Id] = "waiting for file";                            // зміна стану

                /*return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: response,
                    cancellationToken: cancellationToken);*/

                // створення файлу з субтитрами
                string subsFormat = (format == "json") ? "just_text" : "with_timestamps";
                string fileName = $"subtitles_{videoId}_{lang}_{targetLang}_{subsFormat}.txt";
                string filePath = $"YTCaptionsFiles/{message.Chat.Id}/{fileName}";                      // змінити шлях
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Creating file...",
                    cancellationToken: cancellationToken);
                try
                {
                    System.IO.Directory.CreateDirectory($"YTCaptionsFiles/{message.Chat.Id}");
                    System.IO.File.WriteAllText(filePath, response);

                    _userStates[message.Chat.Id] = "waiting for subs update status";                   // зміна стану
                    using (Stream stream = System.IO.File.OpenRead(filePath))
                    {
                        return await botClient.SendDocumentAsync(
                            chatId: message.Chat.Id,
                            document: InputFile.FromStream(stream, fileName), //new InputOnlineFile(fileStream, filePath),
                            caption: $"Subtitles for video {videoId}." +
                            $"\nEnter /update_subtitles if you want to change format and/or language.",
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Error creating file. Please try again with /new_subtitles.",
                        cancellationToken: cancellationToken);
                }
            }

            // видалення історії користування ботом
            static async Task<Message> HandleDeleteHistory(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    chatId: message.Chat.Id,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);

                try
                {
                    long id = message.Chat.Id;
                    string? username = message.From.Username;
                    if (username == null)
                    {
                        username = message.From.FirstName + message.From.LastName;
                    }
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri($"{_apiUrl}/Subtitles/DeleteUsageHistory/?userID={id}&username={username}"), // message.Chat.Id   message.Chat.Id
                    };
                    Console.WriteLine("Sent DELETE request.");

                    string response = "";
                    using (var responseMessage = await _httpClient.SendAsync(request, cancellationToken))
                    {
                        responseMessage.EnsureSuccessStatusCode();
                        response = await responseMessage.Content.ReadAsStringAsync();
                    }
                    Console.WriteLine("Got DELETE response.");

                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: response,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType() + ex.Message);
                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Some error occurred. Please try again later.",
                        cancellationToken: cancellationToken);
                }
            }

            // всі команди з описами
            static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                const string usage = "All commands:" +
                    "\r\n/start - Greeting and showing initially available commands." +
                    "\r\n/help - Showing a list with all commands with descriptions." +
                    "\r\n/cancel - Cancelling current process and deleting saved parameters." +
                    "\r\n/new_subtitles - Asking for video ID or link (5 available formats), checking video availability and asking to confirm video choice." +
                    "\r\n/subtitles_OK - Start of requesting other parameters (use only when asked)" +
                    "\r\n/justText and /withTime - Saving chosen format parameter (use only when asked)." +
                    "\r\n/update_subtitles - Asking for parameters and getting new subtitles file for the last chosen video, if video ID is still saved (use only when asked)." +
                    "\r\n/delete_history - Deleting all of your usage history from database.";

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    //replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            // для обробки тексту параметрів чи непідтримуваних команд
            static async Task<Message> DoNothing(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Ready for answer.",
                    cancellationToken: cancellationToken);
            }

            // обробка вибору мов
            static async Task<Message> HandleLanguageChoice(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                var thisUserParams = _userParameters.Find(x => x.UserID == message.Chat.Id.ToString());
                _userStates[message.Chat.Id] = (thisUserParams.isUpdate == true) ? "UPDATE waiting for subs from api" : "waiting for subs from api";          // зміна стану

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Enter the translation language tag (required) " +
                          $"\nand the language tag of the locale (optional) " +
                          $"\nin [IETF language tag format](https://en.wikipedia.org/wiki/IETF_language_tag#List_of_common_primary_language_subtags) " +
                          $"\nVideo will be searched from that locale. Default locale - USA." +
                          $"\n*Between language tags exactly 1 space is required.*" +
                          $"\nIf the chosen language is unsupported, you will receive text in original language.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
            static Task<Message> FailingHandler(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                throw new IndexOutOfRangeException();
            }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
        }



#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
        private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }

    }
}
