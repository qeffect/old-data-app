using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QAnon.Delta.Tool
{
    public class TweetDump
    {
        public TweetDump()
        {
            Tweets = new List<Tweet>();
            Users = new List<User>();
        }
        
        [JsonProperty("query")]
        public string Query { get; set; }
        
        [JsonProperty("tweets")]
        public List<Tweet> Tweets { get; }
        
        [JsonProperty("users")]
        public List<User> Users { get; }
    }

    public class Tweet : IPostSource
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        
        [JsonProperty("full_text")]
        public string FullText { get; set; }

        [JsonProperty("user_id")]
        public ulong UserId { get; set; }
        
        [JsonIgnore]
        public string UserName { get; set; }
        
        [JsonIgnore]
        public DateTimeOffset DateCreated => CreatedAt;
        
        public string Url => $"https://twitter.com/{UserName}/status/{Id}";

        public string Content => FullText;
    }

    public class User
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }
    }
}