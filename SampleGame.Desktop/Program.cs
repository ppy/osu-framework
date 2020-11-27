// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework;
using osu.Framework.Platform;

namespace SampleGame.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool useOsuTK = args.Contains(@"--tk");

            using (GameHost host = Host.GetSuitableHost(@"sample-game", useOsuTK: useOsuTK))
            using (Game game = new SampleGameGame())
                host.Run(game);
        }
    }
}
