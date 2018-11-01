// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using osuTK;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameWindow : DesktopGameWindow
    {
        private bool isSdl;

        public bool IsSdl => isSdl;

        public LinuxGameWindow()
        {
            Load += OnLoad;
        }

        protected void OnLoad(object sender, EventArgs e)
        {
            var implementationField = typeof(NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");

            var windowImpl = implementationField.GetValue(Implementation);

            isSdl = windowImpl.GetType().Name == "Sdl2NativeWindow";
        }
    }
}
