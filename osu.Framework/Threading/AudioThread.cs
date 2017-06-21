// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        public AudioThread(Action onNewFrame, string threadName)
            : base(onNewFrame, threadName)
        {
        }

        public override void Exit()
        {
            base.Exit();

            ManagedBass.Bass.Free();
        }
    }
}
