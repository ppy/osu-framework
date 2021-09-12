// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Text;

namespace osu.Framework.Tests.Visual
{
    public class FrameworkTestSceneTestRunner : TestSceneTestRunner
    {
        private FontStore testFonts;

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(FrameworkTestScene).Assembly), "Resources"));

            // nested store for framework test fonts to not interfere with framework's default (roboto) in test scenes.
            Fonts.AddStore(testFonts = new FontStore(useAtlas: false));

            AddFont(Resources, "Fonts/Noto/Noto-Basic", metrics: new FontMetrics(1160, 320, 1000), target: testFonts);
        }
    }
}
