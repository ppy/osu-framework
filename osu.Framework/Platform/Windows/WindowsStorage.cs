// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Text.RegularExpressions;

namespace osu.Framework.Platform.Windows
{
    public class WindowsStorage : DesktopStorage
    {
        public WindowsStorage(string baseName)
            : base(baseName)
        {
            // allows traversal of long directory/filenames beyond the standard limitations (see https://stackoverflow.com/a/5188559)
            BasePath = Regex.Replace(BasePath, @"^([a-zA-Z]):\\", @"\\?\$1:\");
        }

        protected override string LocateBasePath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
