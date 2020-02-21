// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FlappyDon.Game;
using osu.Framework;
using osu.Framework.Platform;

namespace FlappyDon.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost(@"FlappyDon"))
            using (osu.Framework.Game game = new FlappyDonGame())
            {
                host.Run(game);
            }
        }
    }
}
