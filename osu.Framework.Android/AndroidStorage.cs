// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Android.App;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    public class AndroidStorage : DesktopStorage
    {
        public AndroidStorage(string baseName, GameHost host)
            : base(baseName, host)
        {
        }

        protected override string LocateBasePath()
        {
            return Application.Context.GetExternalFilesDir(string.Empty).ToString();
        }

        public override void OpenInNativeExplorer()
        {
            //Not needed now.
            throw new NotImplementedException();
        }
    }
}
