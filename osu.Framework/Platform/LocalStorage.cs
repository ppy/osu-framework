// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Platform;

namespace osu.Framework.Configuration
{
    public class LocalStorage : DesktopStorage
    {
        protected override string LocateBasePath()
        {
            return Environment.CurrentDirectory;
        }

        public LocalStorage()
            : base(string.Empty)
        {
        }
    }
}
