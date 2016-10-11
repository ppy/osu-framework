using System;
namespace osu.Framework.Platform
{
    public class IPCMessage
    {
        public string Type { get; set; }
        public object Value { get; set; }
    }
}