// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform;

namespace osu.Framework.iOS
{
    public class IOSStorage : NativeStorage
    {
        protected override string LocateBasePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "..", "Library", "Documents");

        public IOSStorage(string baseName, IOSGameHost host = null)
            : base(baseName, host)
        {
        }
    }
}
