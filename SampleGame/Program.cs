// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Desktop;
using osu.Framework.OS;
using osu.Framework;

namespace SampleGame
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (Game game = new osu.Desktop.SampleGame())
            using (BasicGameHost host = Host.GetSuitableHost())
            {
                host.Load(game);
                host.Run();
            }
        }
    }
}
