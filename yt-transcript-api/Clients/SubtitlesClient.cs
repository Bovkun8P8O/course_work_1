using yt_transcript_api.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web;

namespace yt_transcript_api.Clients
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
        public async Task<VideoDetails> GetVideoDetailsAsync(string videoId, string lang = Constants.DEFAULT_LOCALE)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_apiUrl}/v2/video/details?videoId={videoId}&lang={lang}&audios=false&subtitles=true&related=false"), // &audios=false&subtitles=true&related=false"
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
        //                                                         "" - мова оригіналу (непідтримувана мова - апі видасть оригінал))
        public async Task<SubtitleItem[]> GetSubtitleItemJSONAsync(string subtitleUrl, string targetLang = "") 
        {            
            subtitleUrl = HttpUtility.UrlEncode(subtitleUrl); // URL-кодування для правильної передачі посилання
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_apiUrl}/v2/video/subtitles?subtitleUrl={subtitleUrl}&format=json&fixOverlap=true&targetLang={targetLang}"),
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

        public async Task<string> GetSubtitleItemSRTAsync(string subtitleUrl, string targetLang = "")
        {
            subtitleUrl = HttpUtility.UrlEncode(subtitleUrl); // URL-кодування для правильної передачі посилання
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{_apiUrl}/v2/video/subtitles?subtitleUrl={subtitleUrl}&format=srt&fixOverlap=true&targetLang={targetLang}"),
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
                var result = body;
                return result;
            }
        }
    }
}
