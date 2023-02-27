// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
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
            GraphicsSurfaceType? surfaceType = Enum.TryParse(args.GetNext("--surface"), true, out GraphicsSurfaceType type) ? type : null;

            HostOptions options = new HostOptions
            {
                PortableInstallation = portable,
                PreferredGraphicsSurface = surfaceType
            };

            using (GameHost host = Host.GetSuitableDesktopHost(@"visual-tests", options))
            {
                if (benchmark)
                    host.Run(new AutomatedVisualTestGame());
                else
                    host.Run(new VisualTestGame());
            }
        }
    }
}
