using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QAnon.Delta.Tool
{
    public class QPost : IPostSource
    {
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset Timestamp { get; set; }
        
        [JsonProperty("text")]
        public string Text { get; set; }
        
        [JsonProperty("link")]
        public string Link { get; set; }
        
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("references")]
        public List<QPost> References { get; set; }
        
        public DateTimeOffset DateCreated => Timestamp;
        
        public string Url => Link;

        public string Content => ToContentString("");

        public string ToContentString(string prefix)
        {
            var result = new StringBuilder();

            void Visit(QPost post, int depth)
            {
                var p = prefix;
                for (var x = 0; x < depth; x++)
                {
                    p += "\t";
                }
                
                foreach (var line in post.Text.Split(Environment.NewLine))
                {
                    result.AppendLine($"{p}{line}");
                }

                if (post.References != null)
                {
                    foreach(var reference in post.References)
                    {
                        Visit(reference, depth + 1);
                    }
                }
            }
            
            Visit(this, 0);

            return result.ToString();
        }
    }
}