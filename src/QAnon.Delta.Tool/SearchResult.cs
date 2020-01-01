using System.Collections.Generic;

namespace QAnon.Delta.Tool
{
    public class SearchResult
    {
        public SearchResult()
        {
            Tweets = new Dictionary<ulong, Tweet>();
            Users = new Dictionary<ulong, User>();
        }
        
        public Dictionary<ulong, Tweet> Tweets { get; }
        
        public Dictionary<ulong, User> Users { get; }
    }
}