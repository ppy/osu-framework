﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Platform;

namespace osu.Framework.Tests
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool benchmark = args.Contains(@"--benchmark");
            bool portable = args.Contains(@"--portable");
            bool useSdl = args.Contains(@"--sdl");

            using (GameHost host = Host.GetSuitableHost(@"visual-tests", portableInstallation: portable, useSdl: useSdl))
            {
                if (benchmark)
                    host.Run(new AutomatedVisualTestGame());
                else
                    host.Run(new VisualTestGame());
            }
        }
    }
}
