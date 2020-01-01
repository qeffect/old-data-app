using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Logic;
using Tweetinvi.Parameters;

namespace QAnon.Delta.Tool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tweets = Twitter.SearchTweets("(from:realDonaldTrump) since:2008-09-15");
            Console.WriteLine(tweets.Tweets);
        }
    }
}
