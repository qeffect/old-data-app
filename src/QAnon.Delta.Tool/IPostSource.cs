using System;

namespace QAnon.Delta.Tool
{
    public interface IPostSource
    {
        public DateTimeOffset DateCreated { get; }
        
        public string Url { get; }
        
        public string Content { get; }
    }
}