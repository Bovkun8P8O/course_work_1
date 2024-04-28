using Newtonsoft.Json.Linq;

namespace yt_transcript_bot.Models
{
    public class Subtitles
    {
        public SubtitleItem[] Properties { get; set; }
    }

    public class SubtitleItem
    {
        public int startMs { get; set; }
        public int durMs { get; set; }
        public string text { get; set; }
    }
}
