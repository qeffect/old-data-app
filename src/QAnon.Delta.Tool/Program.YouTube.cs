using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace QAnon.Delta.Tool
{
    partial class Program
    {
        [Verb("get-captions")]
        class GetCaptionsOptions
        {
            
        }
        
        private static async Task<int> GetCaptions(GetCaptionsOptions options)
        {
            UserCredential credential = null;
            using (var stream = new FileStream("/home/pknopf/git/qanon-delta/src/QAnon.Delta.Tool/client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                var d = GoogleClientSecrets.Load(stream);
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    d.Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeForceSsl },
                    "user",
                    CancellationToken.None
                );
            }
            
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "test"
            });
            
            await OutputCaptionsToDirectory(youtubeService, "UCMVTRzCXvIbdK0Y1ZxD-BlA", Path.GetFullPath("./captions/"));
            //await OutputCaptionsToDirectory(youtubeService, "UCQ7VgW7XgJQjDEPnOR-Q0Qw", Path.GetFullPath("./captions/"));
            //await OutputCaptionsToDirectory(youtubeService, "UCm5CkXzGXb-A2XX0nuTctMQ", Path.GetFullPath("./captions"));
            //await OutputCaptionsToDirectory(youtubeService, "UCSio3E7kYvPeHKhfuYZWriA", Path.GetFullPath("./captions/"));
            //await OutputCaptionsToDirectory(youtubeService, "UCpwXjOAwWDuWlmA2gTjjBwg", Path.GetFullPath("./captions/"));
            // await OutputCaptionsToDirectory(youtubeService, "UCQ1h0i1ksKlvPI7zI6t9XoA",
            //     Path.GetFullPath("./captions/DanielLee"));
            
            // await OutputCaptionsToDirectory(youtubeService, "UC98Zwfvjq12M1oi99Yqd78w",
            //     Path.GetFullPath("./captions/Tracy"));
            
            // await OutputCaptionsToDirectory(youtubeService, "UCRVpj-n5kyVfDNtcuwN6KkA",
            //     Path.GetFullPath("./captions/BillSmith"));
            //
            // await OutputCaptionsToDirectory(youtubeService, "UC8VYbOH2Z_swlgSSQ-RwaUg",
            //     Path.GetFullPath("./captions/CitizensInvestigativeReport"));
            //
            // await OutputCaptionsToDirectory(youtubeService, "UCAyrKoW31y5UcsRjh2ItvxQ",
            //     Path.GetFullPath("./captions/IPOT"));
            //
            // await OutputCaptionsToDirectory(youtubeService, "UCAHCehFYe02Ihviho8D_ZcQ",
            //     Path.GetFullPath("./captions/TruthandArtTV"));
            //
            await OutputCaptionsToDirectory(youtubeService, "UCB1o7_gbFp2PLsamWxFenBg",
                Path.GetFullPath("./captions/X22Report"));
            
            return 0;
        }

        private static async Task OutputCaptionsToDirectory(YouTubeService youTubeService, string channelId, string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var channelVideos = await GetChannelVideos(channelId, youTubeService);
            
            foreach (var video in channelVideos)
            {
                var videoUrl = $"https://www.youtube.com/watch?v={video}";

                try
                {
                    var timedTextPath = Path.Combine(directory, $"{video}.xml");
                    if (File.Exists(timedTextPath))
                    {
                        continue;
                    }
                    
                    var videoHtml = GetRequestBody(videoUrl);

                    var timedTextUrl = Regex.Match(videoHtml,
                            @"captionTracks\\"":\[\{\\\""baseUrl\\\""\:\\\""((.*))\\\"",\\\""name\\\""")
                        .Groups[2].Value;

                    timedTextUrl = timedTextUrl.Replace("\\/", "/")
                        .Replace("\\\\u0026", "&");

                    var timedTextXml = GetRequestBody(timedTextUrl);

                    File.WriteAllText(timedTextPath, timedTextXml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing video " + video);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private static async Task<List<string>> GetChannelVideos(string channelId, YouTubeService youTubeService)
        {
            var channelRequest = youTubeService.Channels.List("contentDetails,topicDetails,snippet");
            channelRequest.Id = channelId;
            var channelResponse = (await channelRequest.ExecuteAsync());
            var response = (await channelRequest.ExecuteAsync()).Items.FirstOrDefault();
            var uploadPlaylist = response.ContentDetails.RelatedPlaylists.Uploads;

            var videosRequest = youTubeService.PlaylistItems.List("snippet");
            videosRequest.PlaylistId = uploadPlaylist;
            videosRequest.MaxResults = 50;

            var videos = new List<string>();
            var videosResponse = await videosRequest.ExecuteAsync();

            while (videosResponse.Items.Count > 0)
            {
                videos.AddRange(videosResponse.Items.Select(x => x.Snippet.ResourceId.VideoId));

                if (!string.IsNullOrEmpty(videosResponse.NextPageToken))
                {
                    videosRequest.PageToken = videosResponse.NextPageToken;
                    videosResponse = await videosRequest.ExecuteAsync();
                }
                else
                {
                    videosResponse.Items.Clear();
                }
            }

            return videos;
        }

        private static string GetRequestBody(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
    }
}