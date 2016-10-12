using System;
namespace osu.Framework.Platform
{
    public class IpcMessage
    {
        public string Type { get; set; }
        public object Value { get; set; }
    }
}