// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.IO.Stores;
using osu.Framework.Text;

namespace osu.Framework.Tests
{
    [Cached]
    internal class TestGame : Game
    {
        public readonly Bindable<bool> BlockExit = new Bindable<bool>();

        private FontStore testFonts;

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(TestGame).Assembly), "Resources"));

            // nested store for framework test fonts to not interfere with framework's default (roboto) in test scenes.
            Fonts.AddStore(testFonts = new FontStore(useAtlas: false));

            AddFont(Resources, "Fonts/Noto/Noto-Basic", metrics: new FontMetrics(1160, 320, 1000), target: testFonts);
        }

        protected override bool OnExiting() => BlockExit.Value;
    }
}
