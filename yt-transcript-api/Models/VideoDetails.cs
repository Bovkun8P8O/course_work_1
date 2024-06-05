namespace yt_transcript_api.Models
{
    public class VideoDetails
    {
        public bool status { get; set; }
        public string errorId { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public string title { get; set; }                   // назва відео
        public string description { get; set; }
        public Channel channel { get; set; }                // назва каналу
        public int lengthSeconds { get; set; }              // тривалість відео
        public int viewCount { get; set; }                  // кількість переглядів
        public string publishedTimeText { get; set; }
        public bool isLiveStream { get; set; }
        public bool isLiveNow { get; set; }
        public bool isRegionRestricted { get; set; }
        public Thumbnail1[] thumbnails { get; set; }
        public Videos videos { get; set; }
        public Audios audios { get; set; }
        public Subtitles subtitles { get; set; }
        public Related related { get; set; }

        public class Channel
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public bool isVerified { get; set; }
            public bool isVerifiedArtist { get; set; }
            public Avatar[] avatar { get; set; }
        }

        public class Avatar
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Videos
        {
            public bool status { get; set; }
            public string errorId { get; set; }
            public int expiration { get; set; }
            public Item[] items { get; set; }
        }

        public class Item
        {
            public string url { get; set; }
            public int lengthMs { get; set; }
            public string mimeType { get; set; }
            public string extension { get; set; }
            public long lastModified { get; set; }
            public int size { get; set; }
            public string sizeText { get; set; }
            public bool hasAudio { get; set; }
            public string quality { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Audios
        {
            public bool status { get; set; }
            public string errorId { get; set; }
            public int expiration { get; set; }
            public Item1[] items { get; set; }
        }

        public class Item1
        {
            public string url { get; set; }
            public int lengthMs { get; set; }
            public string mimeType { get; set; }
            public string extension { get; set; }
            public long lastModified { get; set; }
            public int size { get; set; }
            public string sizeText { get; set; }
        }

        public class Subtitles
        {
            public bool status { get; set; }
            public string errorId { get; set; }
            public int expiration { get; set; }
            public Subtitle[] items { get; set; }
        }

        public class Subtitle
        {
            public string url { get; set; }
            public string code { get; set; } // language code
            public string text { get; set; }
        }

        public class Related
        {
            public string nextToken { get; set; }
            public Item3[] items { get; set; }
        }

        public class Item3
        {
            public string type { get; set; }
            public string id { get; set; }
            public string title { get; set; }
            public Channel1 channel { get; set; }
            public bool isLiveNow { get; set; }
            public string lengthText { get; set; }
            public string viewCountText { get; set; }
            public string publishedTimeText { get; set; }
            public Thumbnail[] thumbnails { get; set; }
            public string videoCountText { get; set; }
        }

        public class Channel1
        {
            public string type { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public bool isVerified { get; set; }
            public bool isVerifiedArtist { get; set; }
            public Avatar1[] avatar { get; set; }
        }

        public class Avatar1
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Thumbnail
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public bool moving { get; set; }
        }

        public class Thumbnail1
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

    }
}
