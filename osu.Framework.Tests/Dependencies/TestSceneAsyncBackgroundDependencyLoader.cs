// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osuTK;

namespace osu.Framework.Tests.Dependencies
{
    [HeadlessTest]
    public partial class TestSceneAsyncBackgroundDependencyLoader : FrameworkTestScene
    {
        public TestSceneAsyncBackgroundDependencyLoader()
        {
            Add(new MutatingDrawable
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100)
            });
        }

        [Test]
        public void TestMutationAfterAsyncOperation()
        {
            MutatingDrawable? drawable = null;

            AddStep("add drawable", () => Child = drawable = new MutatingDrawable());
            AddUntilStep("wait for load", () => drawable!.IsLoaded);
        }

#pragma warning disable OFSG001
        private partial class MutatingDrawable : CompositeDrawable
        {
            [BackgroundDependencyLoader]
            private async Task load()
            {
                await Task.Delay(1000).ConfigureAwait(true);
                InternalChild = new Box();
            }
        }
#pragma warning restore OFSG001
    }
}
