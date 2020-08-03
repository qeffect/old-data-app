using System.Collections.Generic;

namespace QAnon.Delta.Tool
{
    public class YouTubeVideos
    {
        public string ChannelId { get; set; }
        
        public string ChannelName { get; set; }
        
        public List<YouTubeVideo> Videos { get; set; }
    }
}