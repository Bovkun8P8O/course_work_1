using yt_transcript_bot.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web;

namespace yt_transcript_bot.Clients
{
    public class SubtitlesClient
    {
        private HttpClient _httpClient;
        private static string _apiKey;
        private static string _apiUrl;
        private static string _apiHost;

        public SubtitlesClient()
        {
            _apiKey = Constants.YT_RAPID_API_KEY;
            _apiUrl = Constants.YT_RAPID_API_URL;
            _apiHost = Constants.YT_RAPID_API_HOST;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_apiUrl);
        }

        //                                                                                 IETF language tag  
        public async Task<VideoDetails> GetVideoDetailsAsync(string videoId, string lang = "en-US")
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_apiUrl}/v2/video/details?videoId={videoId}&lang={lang}&subtitles=true"), // &audios=false&subtitles=true&related=false"
                Headers =
                {
                    { "X-RapidAPI-Key", _apiKey },
                    { "X-RapidAPI-Host", _apiHost },
                },
            };
            using (var response = await _httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n==================== Video details ====================\n\n" + body);
                var result = JsonConvert.DeserializeObject<VideoDetails>(body);
                return result;
            }
        }

        // отримати субтитри за посиланням з об'єкту VideoDetails (subtitleUrl = VideoDetails.Subtitles.items[i].url;
        //                                                         targetLang - мова перекладу (IETF language tag);
        //                                                         "" - мова оригіналу (непідтримувана мова - видасть оригінал))
        public async Task<SubtitleItem[]> GetSubtitleItemAsync(string subtitleUrl, string targetLang = "", string format = "json") 
        {            
            subtitleUrl = HttpUtility.UrlEncode(subtitleUrl); // URL-кодування для правильної передачі посилання
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_apiUrl}/v2/video/subtitles?subtitleUrl={subtitleUrl}&format={format}&fixOverlap=true&targetLang={targetLang}"),
                Headers =
                {
                    { "X-RapidAPI-Key", _apiKey },
                    { "X-RapidAPI-Host", _apiHost },
                },
            };
            using (var response = await _httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n==================== Subtitles ====================\n\n" + body);
                var result = JsonConvert.DeserializeObject<SubtitleItem[]>(body);
                return result;
            }
        }
    }
}
