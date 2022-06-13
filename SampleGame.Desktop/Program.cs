// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework;
using osu.Framework.Platform;

namespace SampleGame.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"sample-game"))
            using (Game game = new SampleGameGame())
                host.Run(game);
        }
    }
}
