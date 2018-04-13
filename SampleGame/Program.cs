// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Platform;
using osu.Framework;

namespace SampleGame
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (Game game = new SampleGame())
            using (GameHost host = Host.GetSuitableHost(@"sample-game"))
                host.Run(game);
        }
    }
}
