// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;
using osu.Framework;

namespace SampleGame.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (Game game = new SampleGameGame())
            using (GameHost host = Host.GetSuitableHost(@"sample-game"))
                host.Run(game);
        }
    }
}
