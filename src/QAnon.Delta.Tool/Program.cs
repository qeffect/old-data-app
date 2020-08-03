using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Jint;
using Newtonsoft.Json;

namespace QAnon.Delta.Tool
{
    partial class Program
    {
        public static List<IPostSource> TrumpTweets { get; set; }
        
        public static List<IPostSource> JoeMTweets { get; set; }
        
        public static List<IPostSource> PrayingMedicTweets { get; set; }
        
        public static List<IPostSource> JordanTweets { get; set; }
        
        public static List<IPostSource> DustinTweets { get; set; }
        
        public static List<IPostSource> QPosts { get; set; }
        
        public static List<IPostSource> Dappergander { get; set; }
        
        public static Dictionary<string, List<IPostSource>> Anons = new Dictionary<string, List<IPostSource>>();
        
        static async Task<int> Main(string[] args)
        {
            LoadData();
            
            return Parser.Default.ParseArguments<DeltaOptions, DeltasByMinuteOptions, GetCaptionsOptions>(args)
                .MapResult(
                    (DeltaOptions opts) => RunDelta(opts),
                    (DeltasByMinuteOptions opts) => DeltasByMinute(opts),
                    (GetCaptionsOptions opts) => GetCaptions(opts).GetAwaiter().GetResult(),
                    errs => 1);
        }

        private static void LoadData()
        {
            QPosts = JsonConvert.DeserializeObject<List<QPost>>(File.ReadAllText(Path.GetFullPath("./data/q.json")))
                .Cast<IPostSource>()
                .ToList();

            var firstPostDate = QPosts.Last().DateCreated;

            List<IPostSource> GetTweets(string path, string userName)
            {
                return JsonConvert
                    .DeserializeObject<TweetDump>(File.ReadAllText(path)).Tweets
                    .Where(x => x.DateCreated >= firstPostDate)
                    .Select(x =>
                    {
                        x.UserName = userName;
                        return (IPostSource) x;
                    }).ToList();
            }

            TrumpTweets = GetTweets(Path.GetFullPath("./data/tweets/realDonaldTrump.json"), "realDonaldTrump");
            JordanTweets = GetTweets(Path.GetFullPath("./data/tweets/Jordan_Sather_.json"), "Jordan_Sather_");
            JoeMTweets = GetTweets(Path.GetFullPath("./data/tweets/StormIsUponUs.json"), "StormIsUponUs");
            PrayingMedicTweets = GetTweets(Path.GetFullPath("./data/tweets/prayingmedic.json"), "prayingmedic");
            DustinTweets = GetTweets(Path.GetFullPath("./data/tweets/DustinNemos.json"), "DustinNemos");
            Dappergander = GetTweets(Path.GetFullPath("./data/tweets/dappergander.json"), "dappergander");
            
            Anons = new Dictionary<string, List<IPostSource>>
            {
                {"Q", QPosts},
                {"dappergander", Dappergander},
                {"Jordan_Sather_", JordanTweets},
                {"prayingmedic", PrayingMedicTweets},
                {"DustinNemos", DustinTweets},
                {"StormIsUponUs", JoeMTweets}
            };
        }

        [Verb("delta")]
        class DeltaOptions
        {
            
        }

        private static int RunDelta(DeltaOptions options)
        {
            foreach (var anon in Anons)
            {
                if (anon.Key != "Q")
                {
                    continue;
                }
                Console.WriteLine($"Report for {anon.Key}:");
                PrintDeltas(TrumpTweets, anon.Value);
            }
            
            return 0;
        }

        private static void PrintDeltas(IList<IPostSource> trumpTweets, IList<IPostSource> anonPosts)
        {
            var deltas = new List<Delta>();
            foreach (var anonPost in anonPosts)
            {
                // Find a trump tweet that is within 1 minute of each other.
                foreach (var tweet in trumpTweets)
                {
                    var diffAbs = TimeSpan.FromTicks(Math.Abs(anonPost.DateCreated.Ticks - tweet.DateCreated.Ticks));
                    if (diffAbs < TimeSpan.FromMinutes(1))
                    {
                        deltas.Add(new Delta
                        {
                            TrumpTweet = tweet,
                            Anon = anonPost,
                            QPostDiff = anonPost.DateCreated - tweet.DateCreated
                        });
                    }
                }
            }

            var deltasBefore = deltas.Count(x => x.QPostDiff.Ticks <= 0);
            var deltasAfter = deltas.Count(x => x.QPostDiff.Ticks > 0);
            Console.WriteLine(
                $"Number of one minute deltas: {deltas.Count} (before: {deltasBefore}, after: {deltasAfter}))");

            Console.WriteLine("Total anon posts: " + anonPosts.Count);
            
            Console.WriteLine($"Accuracy (BEFORE and AFTER <1m deltas): {(deltas.Count / (double)anonPosts.Count) * 100}");
            Console.WriteLine($"Accuracy (BEFORE <1m deltas): {(deltasBefore / (double)anonPosts.Count) * 100}");

            for (var x = 0; x < deltas.Count; x++)
            {
                var delta = deltas[x];

                if (delta.QPostDiff.Ticks > 0)
                {
                    continue;
                }
                
                Console.WriteLine("----------------");
                Console.WriteLine($"Delta {x + 1}, diff {delta.QPostDiff}");
                Console.WriteLine($"\tSource tweet: {delta.TrumpTweet.Url}");
                Console.WriteLine("\tDate: " + delta.TrumpTweet.DateCreated);
                Console.WriteLine("\t\tContent: ");
                foreach (var line in delta.TrumpTweet.Content.Split(Environment.NewLine))
                {
                    Console.WriteLine("\t\t\t" + line);
                }
                Console.WriteLine($"\tSource anon: {delta.Anon.Url}");
                Console.WriteLine("\tDate: " + delta.Anon.DateCreated);
                Console.WriteLine("\t\tContent: ");
                foreach (var line in delta.Anon.Content.Split(Environment.NewLine))
                {
                    Console.WriteLine("\t\t\t" + line);
                }
            }
            Console.WriteLine("----------------");
        }

        [Verb("delta-by-minute")]
        class DeltasByMinuteOptions
        {
            
        }
        
        private static int DeltasByMinute(DeltasByMinuteOptions options)
        {
            var plots = new Dictionary<string, Dictionary<int, int>>();
            
            foreach (var anon in Anons)
            {
                Console.WriteLine($"Report for {anon.Key}:");
                plots[anon.Key] = PrintDeltasByMinute(TrumpTweets, anon.Value);
            }

            var plotFile = Path.GetFullPath("./plot.dat");
            if (File.Exists(plotFile))
            {
                File.Delete(plotFile);
            }
            using (var file = File.OpenWrite(plotFile))
            using (var streamWriter = new StreamWriter(file))
            {
                streamWriter.Write("User");
                foreach (var key in plots.Keys)
                {
                    streamWriter.Write($"\t{key}");
                }
                
                streamWriter.WriteLine();

                foreach (var minute in plots.Values.First().Keys)
                {
                    streamWriter.Write($"<{minute + 1} delta");

                    foreach (var plot in plots)
                    {
                        var count = plot.Value[minute];
                        var percentageOfTotalPosts = ((double) count / Anons[plot.Key].Count) * 100;
                        streamWriter.Write($"\t{percentageOfTotalPosts}");
                    }
                    
                    streamWriter.WriteLine();
                }
            }
            
            
            // # IMMIGRATION BY REGION AND SELECTED COUNTRY OF LAST RESIDENCE
            // #
            // Region	Austria	Hungary	Belgium	Czechoslovakia	Denmark	France	Germany	Greece	Ireland	Italy	Netherlands	Norway	Sweden	Poland	Portugal	Romania	Soviet_Union	Spain	Switzerland	United_Kingdom	Yugoslavia	Other_Europe	TOTAL	
            // 1891-1900	234081	181288	18167	-	50231	30770	505152	15979	388416	651893	26758	95015	226266	96720	27508	12750	505290	8731	31179	271538	-	282	3378014	
            // 1901-1910	668209	808511	41635	-	65285	73379	341498	167519	339065	2045877	48262	190505	249534	-	69149	53008	1597306	27935	34922	525950	-	39945	7387494	
            // 1911-1920	453649	442693	33746	3426	41983	61897	143945	184201	146181	1109524	43718	66395	95074	4813	89732	13311	921201	68611	23091	341408	1888	31400	4321887	
            // 1921-1930	32868	30680	15846	102194	32430	49610	412202	51084	211234	455315	26948	68531	97249	227734	29994	67646	61742	28958	29676	339570	49064	42619	2463194	
            // 1931-1940	3563	7861	4817	14393	2559	12623	144058	9119	10973	68028	7150	4740	3960	17026	3329	3871	1370	3258	5512	31572	5835	11949	377566	
            // 1941-1950	24860	3469	12189	8347	5393	38809	226578	8973	19789	57661	14860	10100	10665	7571	7423	1076	571	2898	10547	139306	1576	8486	621147	
            // 1951-1960	67106	36637	18575	918	10984	51121	477765	47608	43362	185491	52277	22935	21697	9985	19588	1039	671	7894	17675	202824	8225	16350	1325727	
            // 1961-1970	20621	5401	9192	3273	9201	45237	190796	85969	32966	214111	30606	15484	17116	53539	76065	3531	2465	44659	18453	213822	20381	11604	1124492	
            //
            return 0;
        }
        
        private static Dictionary<int, int> PrintDeltasByMinute(IList<IPostSource> trumpTweets, IList<IPostSource> anonPosts)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            counts[0] = 0;
            counts[1] = 0;
            counts[2] = 0;
            counts[3] = 0;
            counts[4] = 0;
            counts[5] = 0;
            counts[6] = 0;
            counts[7] = 0;
            counts[8] = 0;
            counts[9] = 0;
            counts[10] = 0;
            counts[11] = 0;
            counts[12] = 0;
            counts[13] = 0;
            counts[14] = 0;
            counts[15] = 0;
            counts[16] = 0;
            counts[17] = 0;
            counts[18] = 0;
            counts[19] = 0;
            
            foreach (var anonPost in anonPosts)
            {
                // Find a trump tweet that is within 1 minute of each other.
                foreach (var tweet in trumpTweets)
                {
                    var diff = anonPost.DateCreated - tweet.DateCreated;
                    if (diff.Ticks > 0)
                    {
                        continue;
                    }
                    if(diff > TimeSpan.FromMinutes(-1))
                    {
                        counts[0] = counts[0] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-2))
                    {
                        counts[1] = counts[1] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-3))
                    {
                        counts[2] = counts[2] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-4))
                    {
                        counts[3] = counts[3] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-5))
                    {
                        counts[4] = counts[4] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-6))
                    {
                        counts[5] = counts[5] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-7))
                    {
                        counts[6] = counts[6] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-8))
                    {
                        counts[7] = counts[7] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-9))
                    {
                        counts[8] = counts[8] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-10))
                    {
                        counts[9] = counts[9] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-11))
                    {
                        counts[10] = counts[10] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-12))
                    {
                        counts[11] = counts[11] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-13))
                    {
                        counts[12] = counts[12] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-14))
                    {
                        counts[13] = counts[13] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-15))
                    {
                        counts[14] = counts[14] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-16))
                    {
                        counts[15] = counts[15] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-17))
                    {
                        counts[16] = counts[16] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-18))
                    {
                        counts[17] = counts[17] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-19))
                    {
                        counts[18] = counts[18] + 1;
                    }
                    else if (diff > TimeSpan.FromMinutes(-20))
                    {
                        counts[19] = counts[19] + 1;
                    }
                }
            }

            foreach (var count in counts)
            {
                var percentageOfTotalPosts = ((double) count.Value / anonPosts.Count) * 100;
                Console.WriteLine($"\t{count.Key} minute(s): total: {count.Value}: percentage of total: {percentageOfTotalPosts}");
            }

            return counts;
        }
    }

    public class Delta
    {
        public IPostSource TrumpTweet { get; set; }
        
        public IPostSource Anon { get; set; }
        
        public TimeSpan QPostDiff { get; set; }
    }
}
