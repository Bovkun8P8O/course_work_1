namespace CaptionsBot
{
    internal class CaptionsRequestParameters
    {
        public string UserID { get; set; }
        public string VideoID { get; set; }
        public string Lang { get; set; }
        public string TargetLang { get; set; }
        public string Format { get; set; }
        public bool isUpdate { get; set; }

        public CaptionsRequestParameters(string userID, string videoID, string lang, string targetLang, string format)
        {
            UserID = userID;
            VideoID = videoID;
            Lang = lang;
            TargetLang = targetLang;
            Format = format;
            isUpdate = false;
        }
    }
}
