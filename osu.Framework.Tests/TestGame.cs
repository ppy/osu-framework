// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests
{
    [Cached]
    internal class TestGame : Game
    {
        public readonly Bindable<bool> BlockExit = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(TestGame).Assembly), "Resources"));
        }

        protected override bool OnExiting() => !BlockExit.Value;
    }
}
