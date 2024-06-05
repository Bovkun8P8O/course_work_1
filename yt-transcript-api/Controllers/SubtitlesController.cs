using Microsoft.AspNetCore.Mvc;
using yt_transcript_api.Models;
using yt_transcript_api.Clients;
using yt_transcript_api.Database;
using System.Net;

namespace yt_transcript_api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SubtitlesController : ControllerBase
    {
        private readonly ILogger<SubtitlesController> _logger;

        public SubtitlesController(ILogger<SubtitlesController> logger)
        {
            _logger = logger;
        }

        // GET: Subtitles/GetVideoName?videoId=videoId
        [HttpGet]
        [ActionName("GetVideoName")] // отримати назву відео з об'єкту VideoDetails
                                     //(використати перед завантаженням субтитрів для перевірки правильності введення коду користувачем)
        public string GetVideoName(string videoId/*, string lang = "en-US"*/)
        {
            bool isVideoIdValid = false;
            /* варіанти посилань:
            https://www.youtube.com/watch?v=videoId
            https://www.m.youtube.com/watch?v=videoId
            https://youtu.be/videoId
            https://www.youtube.com/v/videoId
            https://www.youtube.com/embed/videoId */
            
            string VideoID = "";
            if (videoId.Length == 11)
            {
                VideoID = videoId;
            } // поки у всіх ідентифікаторів відео на Ютуб довжина = 11
            else
            {
                if (videoId.Contains("youtube.com/watch?v="))
                {
                    VideoID = videoId.Substring(videoId.IndexOf("youtube.com/watch?v") + 19, 11);
                }
                else if (videoId.Contains("youtu.be/"))
                {
                    VideoID = videoId.Substring(videoId.IndexOf("youtu.be/") + 9, 11);
                }
                else if (videoId.Contains("youtube.com/v/"))
                {
                    VideoID = videoId.Substring(videoId.IndexOf("youtube.com/v/") + 13, 11);
                }
                else if (videoId.Contains("youtube.com/embed/"))
                {
                    VideoID = videoId.Substring(videoId.IndexOf("youtube.com/embed/") + 17, 11);
                }
            }
            
            // перевірка відповіді на запит з отриманим videoId
            var url = $"https://www.youtube.com/watch?v={VideoID}";
            //var url = $"http://gdata.youtube.com/feeds/api/videos/{VideoID}"; // не працює, бо api v2 застаріле
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = client.SendAsync(request);
                if (response.Result.StatusCode == HttpStatusCode.OK)
                {
                    isVideoIdValid = true;
                }
                Console.WriteLine("\n" + response.Result.ToString());
            }

            // отримання даних про відео
            if (isVideoIdValid)
            {
                try
                {
                    //string responseString = "ID is valid."; // для економної перевірки зв'язку з ботом
                    VideoDetails videoDetails = new VideoDetails();
                    SubtitlesClient subtitlesClient = new SubtitlesClient();
                    videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId).Result;
                    string duration = TimeSpan.FromSeconds(videoDetails.lengthSeconds).ToString(@"hh\:mm\:ss");
                    string responseString = $"Title: {videoDetails.title}\nChannel: {videoDetails.channel.name}\nDuration: {duration}\nViews: {videoDetails.viewCount}";
                    return responseString;
                }
                catch (Exception e)
                {
                    return "Error: Something wrong happened.\nTry requesting later or change request.\n\n" + e.GetType() + e.Message;
                }
            }
            else return "ID_Error: Invalid ID. Video is not available.";
        }


        // POST: Subtitles/PostSubtitlesSRT?userId=userId&videoId=videoId&lang=lang&targetLang=targetLang
        [HttpPost]
        [ActionName("PostSubtitlesSRT")] // варіант "з відмітками часу"
        public string PostSubtitlesWithTimestamps(long userId, string videoId, string lang = Constants.DEFAULT_LOCALE, string targetLang = "en-US") 
        {
            VideoDetails videoDetails = new VideoDetails(); // потрібні для отримання масиву посилань на субтитри
            SubtitlesClient subtitlesClient = new SubtitlesClient();
            videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId, lang).Result; 
            string text = "";
            for (int i = 0; i < videoDetails.subtitles.items.Length; i++) // список посилань, кожне з яких містить список субтитрів
            {
                string subtitleUrl = videoDetails.subtitles.items[i].url;
                string subtitleItem = subtitlesClient.GetSubtitleItemSRTAsync(subtitleUrl, targetLang).Result;
                text += subtitleItem;
            }

            Captions captions = new Captions();
            if (text.Length == 0) text = $"No subtitles found for ID {videoId}."; 
            DateTime date = DateTime.Now;
            captions.InsertCaptions(userId.ToString(), videoId, lang, targetLang, text, date); 
            return text;
        }


        // POST: Subtitles/PostSubtitlesJSON?userId=userId&videoId=videoId&lang=lang&targetLang=targetLang
        [HttpPost]
        [ActionName("PostSubtitlesJSON")] // варіант "просто текст"
        public string PostSubtitlesText(long userId, string videoId, string lang = Constants.DEFAULT_LOCALE, string targetLang = "en-US")
        {
            VideoDetails videoDetails = new VideoDetails(); // потрібні для отримання посилань на субтитри
            SubtitlesClient subtitlesClient = new SubtitlesClient();
            videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId, lang).Result;
            List<Subtitles> subtitles = new List<Subtitles>(); 
            for (int i = 0; i < videoDetails.subtitles.items.Length; i++)
            {
                string subtitleUrl = videoDetails.subtitles.items[i].url;
                var subtitleItem = new Subtitles();
                subtitleItem.Properties = subtitlesClient.GetSubtitleItemJSONAsync(subtitleUrl, targetLang).Result;
                subtitles.Add(subtitleItem);
            }
            var text = "";
            for (int i = 0; i < subtitles.Count; i++) // нема субтитрів - пустий рядок
            {
                for (int j = 0; j < subtitles[i].Properties.Length; j++) // список посилань, кожне з яких містить список субтитрів
                {
                    text += subtitles[i].Properties[j].text + "\n";
                }
                text += "\n";
            }
            if (text.Length > 1) text.Remove(text.Length - 1);
            if (text.Length == 0) text = $"No subtitles found for ID {videoId}.";
            Captions captions = new Captions();
            DateTime date = DateTime.Now;
            captions.InsertCaptions(userId.ToString(), videoId, lang, targetLang, text, date); 
            return text;
        }


        // PUT: Subtitles/PutSubtitles/?userId=userId&videoId=videoId&lang=lang&targetLang=targetLang&fileType=fileType
        [HttpPut]
        [ActionName("PutSubtitles")] // заміна останніх субтитрів з обраного відео на нові
        public string PutSubtitles(long userId, string videoId, string lang = Constants.DEFAULT_LOCALE, string targetLang = "en-US", string fileType = "json") 
        {
            VideoDetails videoDetails = new VideoDetails(); // потрібні для отримання посилань на субтитри
            SubtitlesClient subtitlesClient = new SubtitlesClient();
            videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId, lang).Result;
            string text = "";

            if (fileType == "srt")
            {
                for (int i = 0; i < videoDetails.subtitles.items.Length; i++) // список посилань, кожне з яких містить список субтитрів
                {
                    string subtitleUrl = videoDetails.subtitles.items[i].url;
                    string subtitleItem = subtitlesClient.GetSubtitleItemSRTAsync(subtitleUrl, targetLang).Result;
                    text += subtitleItem;
                }

                if (text.Length == 0) text = $"No subtitles found for ID {videoId}.";
            }
            else if (fileType == "json")
            {
                List<Subtitles> subtitles = new List<Subtitles>(); 
                for (int i = 0; i < videoDetails.subtitles.items.Length; i++) // список посилань, кожне з яких містить список субтитрів
                {
                    string subtitleUrl = videoDetails.subtitles.items[i].url;
                    var subtitleItem = new Subtitles();
                    subtitleItem.Properties = subtitlesClient.GetSubtitleItemJSONAsync(subtitleUrl, targetLang).Result;
                    subtitles.Add(subtitleItem);
                }
                text = "";
                for (int i = 0; i < subtitles.Count; i++) // нема субтитрів - пустий рядок
                {
                    for (int j = 0; j < subtitles[i].Properties.Length; j++)
                    {
                        text += subtitles[i].Properties[j].text + "\n";
                    }
                    text += "\n";
                }
                if (text.Length > 1) text.Remove(text.Length - 1);
                if (text.Length == 0) text = $"No subtitles found for ID {videoId}.";
            }

            Captions captions = new Captions();
            DateTime date = DateTime.Now;
            captions.UpdateCaptions(userId.ToString(), videoId, lang, targetLang, text, date); 
            return text; // "Put method: " + 
        }


        // DELETE: Subtitles/DeleteUsageHistory?userID=userID&username=username 
        [HttpDelete]
        [ActionName("DeleteUsageHistory")]
        public string DeleteUsageHistory(string userId, string username) // видалення всієї історії користувача
        {
            Captions captions = new Captions();
            captions.DeleteUsageHistory(userId);
            return $"Deleted all history for {username} ({userId}).";
        }

    }
}
