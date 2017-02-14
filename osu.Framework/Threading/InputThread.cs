// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Threading
{
    public class InputThread : GameThread
    {
        public void RunUpdate() => ProcessFrame();

        public InputThread(Action onNewFrame, string threadName)
            : base(onNewFrame, threadName)
        {
        }
    }
}