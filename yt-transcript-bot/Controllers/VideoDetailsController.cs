using Microsoft.AspNetCore.Mvc;

namespace yt_transcript_bot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoDetailsController : ControllerBase
    {
        // має бути логування, методи цієї апі (get, post...)
        private readonly ILogger<VideoDetailsController> _logger;

        public VideoDetailsController(ILogger<VideoDetailsController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetVideoDetails")]
        public string Get()
        {
            return "GetVideoDetails";
        }
    }
}
