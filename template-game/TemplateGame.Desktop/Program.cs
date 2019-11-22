// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;
using osu.Framework;
using TemplateGame.Game;

namespace TemplateGame.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost(@"TemplateGame"))
            using (osu.Framework.Game game = new TemplateGameGame())
                host.Run(game);
        }
    }
}
