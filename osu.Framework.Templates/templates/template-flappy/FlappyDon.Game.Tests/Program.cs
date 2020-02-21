// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using FlappyDon.Game.Testing;
using osu.Framework;
using osu.Framework.Platform;

namespace FlappyDon.Game.Tests
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost("visual-tests"))
            using (var game = new FlappyDonTestBrowser())
                host.Run(game);
        }
    }
}
