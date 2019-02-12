// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Tests
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool benchmark = args.Length > 0 && args[0] == @"-benchmark";

            using (GameHost host = Host.GetSuitableHost(@"visual-tests"))
            {
                if (benchmark)
                    host.Run(new AutomatedVisualTestGame());
                else
                    host.Run(new VisualTestGame());
            }
        }
    }
}
