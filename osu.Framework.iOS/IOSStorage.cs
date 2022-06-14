// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Platform;

namespace osu.Framework.iOS
{
    public class IOSStorage : NativeStorage
    {
        public IOSStorage(string path, IOSGameHost host = null)
            : base(path, host)
        {
        }
    }
}
