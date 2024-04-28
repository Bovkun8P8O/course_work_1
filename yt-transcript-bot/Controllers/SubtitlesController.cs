using Microsoft.AspNetCore.Mvc;
using yt_transcript_bot.Models;
using yt_transcript_bot.Clients;

namespace yt_transcript_bot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubtitlesController : ControllerBase
    {
        private readonly ILogger<SubtitlesController> _logger;

        public SubtitlesController(ILogger<SubtitlesController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetSubtitles")]
        public List<Subtitles> GetSubtitles(string videoId, string lang = "en-US", string targetLang = "en-US")
        {
            VideoDetails videoDetails = new VideoDetails(); // потрібні для отримання посилань на субтитри
            List<Subtitles> subtitles = new List<Subtitles>(); // список посилань, кожне з яких містить список субтитрів
            SubtitlesClient subtitlesClient = new SubtitlesClient();
            videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId, lang).Result;
            for (int i = 0; i < videoDetails.subtitles.items.Length; i++)
            {
                string subtitleUrl = videoDetails.subtitles.items[i].url;
                var subtitleItem = new Subtitles();
                subtitleItem.Properties = subtitlesClient.GetSubtitleItemAsync(subtitleUrl, targetLang).Result;
                subtitles.Add(subtitleItem);
            }
            return subtitles;
        }

        [HttpPost(Name = "PostSubtitlesText")]
        public string PostSubtitlesText(string videoId, string lang = "en-US", string targetLang = "en-US")
        //[HttpGet(Name = "GetSubsText")] // конфлікт шляхів, мають бути різними
        //public string GetSubtitlesText(string videoId, string lang = "en-US", string targetLang = "en-US")
        {
            VideoDetails videoDetails = new VideoDetails(); // потрібні для отримання посилань на субтитри
            List<Subtitles> subtitles = new List<Subtitles>(); // список посилань, кожне з яких містить список субтитрів
            SubtitlesClient subtitlesClient = new SubtitlesClient();
            videoDetails = subtitlesClient.GetVideoDetailsAsync(videoId, lang).Result;
            for (int i = 0; i < videoDetails.subtitles.items.Length; i++)
            {
                string subtitleUrl = videoDetails.subtitles.items[i].url;
                var subtitleItem = new Subtitles();
                subtitleItem.Properties = subtitlesClient.GetSubtitleItemAsync(subtitleUrl, targetLang).Result;
                subtitles.Add(subtitleItem);
            }
            var text = "";
            for (int i = 0; i < subtitles.Count; i++)
            {
                for (int j = 0; j < subtitles[i].Properties.Length; j++)
                {
                    text += subtitles[i].Properties[j].text + "\n";
                }
                text += "\n";
            }
            text.Remove(text.Length - 1);
            return text;
        }

        [HttpPut]
        public string Put( string s)
        {
            return "Put method: " + s;
        }

        [HttpDelete]
        public string Delete(string s)
        {
            return "Delete method: " + s;
        }
    }
}
