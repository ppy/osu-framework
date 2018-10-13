// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using OpenTK;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameWindow : DesktopGameWindow
    {
        private bool isSDL;

        public bool IsSDL => isSDL;

        public LinuxGameWindow()
        {
            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            var implementationField = typeof(NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");

            var windowImpl = implementationField.GetValue(Implementation);

            isSDL = windowImpl.GetType().Name == "Sdl2NativeWindow";
        }
    }
}
