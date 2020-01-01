using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Tweetinvi.Models;

namespace QAnon.Delta.Tool
{
    public static class Twitter
    {
        public static SearchResult SearchTweets(string query)
        {
            var urlTemplate =
                "https://api.twitter.com/2/search/adaptive.json?include_profile_interstitial_type=1&include_blocking=1&include_blocked_by=1&include_followed_by=1&include_want_retweets=1&include_mute_edge=1&include_can_dm=1&include_can_media_tag=1&skip_status=1&cards_platform=Web-12&include_cards=1&include_composer_source=true&include_ext_alt_text=true&include_reply_count=1&tweet_mode=extended&include_entities=true&include_user_entities=true&include_ext_media_color=true&include_ext_media_availability=true&send_error_codes=true&simple_quoted_tweets=true&q={query}&count=200&query_source=typed_query&cursor={cursor}&pc=1&spelling_corrections=1&ext=mediaStats%2ChighlightedLabel%2CcameraMoment";

            var url = urlTemplate.Replace("{query}", HttpUtility.UrlEncode(query));

            var result = GetTweets(
                url.Replace("{cursor}", HttpUtility.UrlEncode("scroll:thGAVUV0VFVBaCwL2lpsi_uSEWhvyc1ejcxNEhEhj0AxJjwusAAAH0P4BiTdLxqfwAAAA8ENHujVAXkAAQ0mCjPVaQARDSbUGnl5AEEMgRRdDXYAAQ0KoQapbABxDL2Sj5F2ABEMzqy_LWsAYQzAgvlRdQAhDNeL-hFOABEM11JLUUwAMQyBFFeNdgABDSO1hAlpABEMR22dBXkAEQ0of47laQAxDL4Ytg15AAENE5-q8XsAAQ0fD17xbAAhDOgeKoVpABENIgJs8XsAYQzNCDuhdgABDNhb81VOAAENHrlMAXUAAQzplqmxfQABDKsE8n1pABENIeS-TXkAEQzXWDcxTAARDM6svCl2ABEM1p10HU0AEQ0oTz0haQAxDSbngVl1AFENJdlLhWwAIQ0hR6OFbgABDMQiGZF9AAEM1y21SUUAAQxI-VPlagABDMAwvEl1ABEM5z2j9XsAYQxDrybxeQABDGyAk-l2AAENJDLTNXsAAQzMqeCFdQABDSUaNyFqABEMtravbWkAAQznIZY5ewARDRx7TlluAAEMwHbpkWwAAQx5uEFFawABDSUaav1sAAEM9Lrx_WsAAQ0olZelawABDSizU3FsAHENJVHH7XkAAQxCS5HBeQARDNMr7bF7ACEMzEKDSXsAAQ0jtbOZeQABDPoRNAFrACENJtRxUXsAAQz2agoBbAARDMCpQWF5AAFQIVABUIFQAVABEV1PJ5FYCJehgHREVGQVVMVBUEFQAA")),
                out var cursor);

            while (!string.IsNullOrEmpty(cursor))
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                var tmpResult = GetTweets(url.Replace("{cursor}", HttpUtility.UrlEncode(cursor)), out cursor);
                foreach (var tweet in tmpResult.Tweets)
                {
                    if (!result.Tweets.ContainsKey(tweet.Key))
                    {
                        result.Tweets.Add(tweet.Key, tweet.Value);
                    }
                }

                foreach (var user in tmpResult.Users)
                {
                    if (!result.Users.ContainsKey(user.Key))
                    {
                        result.Users.Add(user.Key, user.Value);
                    }
                }
            }

            return result;
        }

        private static SearchResult GetTweets(string url, out string nextCursor)
        {
            var result = new SearchResult();
            nextCursor = null;
            
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("authority", "api.twitter.com");
                http.DefaultRequestHeaders.Add("origin", "https://twitter.com");
                http.DefaultRequestHeaders.Add("x-twitter-client-language", "en");
                http.DefaultRequestHeaders.Add("x-csrf-token", "96d91b6117420639bde5e4c6e589faea");
                http.DefaultRequestHeaders.Add("authorization", "Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA");
                http.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36");
                http.DefaultRequestHeaders.Add("x-twitter-auth-type", "OAuth2Session");
                http.DefaultRequestHeaders.Add("x-twitter-active-user", "yes");
                http.DefaultRequestHeaders.Add("sec-fetch-site", "same-site");
                http.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
                http.DefaultRequestHeaders.Add("referer", "https://twitter.com/");
                http.DefaultRequestHeaders.Add("accept-encoding", "UTF-8");
                http.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.9,la;q=0.8");
                http.DefaultRequestHeaders.Add("cookie", "dnt=1; kdt=D2ZuwxUO5O3mHbg1sHx2K2Ix7wYM1JH9jK4MDlZR; remember_checked_on=1; csrf_same_site_set=1; rweb_optin=side_no_out; csrf_same_site=1; _ga=GA1.2.995291469.1572878948; tfw_exp=0; _gid=GA1.2.493100779.1577499190; lang=en; ads_prefs=\"HBISAAA=\"; auth_token=7367a55d9acc160329d62a77ad84470168502abb; personalization_id=\"v1_I+xBYZEZHhkY7zpwY1Pe5g==\"; guest_id=v1%3A157781109914544642; twid=u%3D46621029; ct0=96d91b6117420639bde5e4c6e589faea; _twitter_sess=BAh7BiIKZmxhc2hJQzonQWN0aW9uQ29udHJvbGxlcjo6Rmxhc2g6OkZsYXNo%250ASGFzaHsABjoKQHVzZWR7AA%253D%253D--1164b91ac812d853b877e93ddb612b7471bebc74");

                var response = http.GetAsync(url).GetAwaiter()
                    .GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(responseContent);
                }

                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "ddd MMM dd HH:mm:ss zzzz yyyy" };
                var deserializeSettings = new JsonSerializerSettings();
                deserializeSettings.Converters.Add(dateTimeConverter);
                var json = JsonConvert.DeserializeObject<dynamic>(responseContent);

                foreach (JProperty user in json.globalObjects.users)
                {
                    var userId = ulong.Parse(user.Name);
                    var value = (JObject)user.Value;
                    if (!result.Users.ContainsKey(userId))
                    {
                        result.Users.Add(userId, value.ToObject<User>());
                    }
                }
                
                foreach (var instruction in json.timeline.instructions)
                {
                    if (instruction.addEntries != null)
                    {
                        foreach (var entry in instruction.addEntries.entries)
                        {
                            if (((string) entry.entryId).StartsWith("sq-I-t"))
                            {
                                var tweetId = (string) entry.content.item.content.tweet.id;
                                var tweetData = (JObject)json.globalObjects.tweets[tweetId];
                                if (tweetData == null)
                                {
                                    // Deleted? Suspended account?
                                    continue;
                                }
                                result.Tweets.Add(ulong.Parse(tweetId), tweetData.ToObject<Tweet>(JsonSerializer.Create(deserializeSettings)));
                            }
                        }
                    }
                    else if (instruction.replaceEntry != null)
                    {
                        if (((string) instruction.replaceEntry.entryIdToReplace).StartsWith("sq-cursor-bottom"))
                        {
                            nextCursor = (string)instruction.replaceEntry.entry.content.operation.cursor.value;
                        }
                    }
                }

                return result;
            }
        }
    }
}