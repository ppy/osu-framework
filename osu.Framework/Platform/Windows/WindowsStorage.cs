// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.Windows
{
    public class WindowsStorage : DesktopStorage
    {
        public WindowsStorage(string baseName)
            : base(baseName)
        {
        }

        protected override string LocateBasePath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
