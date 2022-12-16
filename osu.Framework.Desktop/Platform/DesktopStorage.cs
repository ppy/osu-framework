// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopStorage : NativeStorage
    {
        public DesktopStorage(string path, DesktopGameHost host)
            : base(path, host)
        {
        }
    }
}
