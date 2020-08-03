using System;

namespace QAnon.Delta.Tool
{
    public class YouTubeVideo : IEquatable<YouTubeVideo>
    {
        public string Id { get; set; }
        
        public DateTimeOffset UploadDate { get; set; }

        public bool Equals(YouTubeVideo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((YouTubeVideo) obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}