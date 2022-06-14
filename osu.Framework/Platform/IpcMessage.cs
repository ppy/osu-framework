// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Platform
{
    public class IpcMessage
    {
        public string Type { get; set; }
        public object Value { get; set; }
    }
}
