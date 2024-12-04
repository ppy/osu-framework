// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Platform;

namespace osu.Framework.Tests
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Marshal.OffsetOf<DepthWrappingVertex<TexturedVertex2D>>(nameof(DepthWrappingVertex<TexturedVertex2D>.Vertex));
            Marshal.OffsetOf<TexturedVertex2D>(nameof(TexturedVertex2D.Position));

            bool benchmark = args.Contains(@"--benchmark");
            bool portable = args.Contains(@"--portable");

            using (GameHost host = Host.GetSuitableDesktopHost(@"visual-tests", new HostOptions { PortableInstallation = portable }))
            {
                if (benchmark)
                    host.Run(new AutomatedVisualTestGame());
                else
                    host.Run(new VisualTestGame());
            }
        }
    }
}
