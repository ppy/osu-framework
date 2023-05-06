// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Dependencies
{
    [HeadlessTest]
    public partial class TestSceneAsyncBackgroundDependencyLoader : FrameworkTestScene
    {
        [Test]
        public void TestLoadOnUpdateThread()
        {
            MutatingDrawable? drawable = null;

            AddStep("add drawable", () => Child = drawable = new MutatingDrawable());
            AddUntilStep("wait to be ready", () => drawable!.IsReadyToVerify);
            AddStep("verify", () => drawable!.Verify());
        }

        [Test]
        public void TestLoadOnAsyncThread()
        {
            MutatingDrawable? drawable = null;

            AddStep("add drawable", () => LoadComponentAsync(drawable = new MutatingDrawable(), Add));
            AddUntilStep("wait to be ready", () => drawable!.IsReadyToVerify);
            AddStep("verify", () => drawable!.Verify());
        }

#pragma warning disable OFSG001
        private partial class MutatingDrawable : Drawable
        {
            private int asyncLoadStarted;
            private int asyncTaskFinished;
            private int asyncLoadFinished;
            private int loadCompleteStarted;
            private int counter;

            [BackgroundDependencyLoader]
            private async Task load()
            {
                asyncLoadStarted = ++counter;

                await Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(true);
                    asyncTaskFinished = ++counter;
                }).ConfigureAwait(true);

                EnsureMutationAllowed("Mutate");

                asyncLoadFinished = ++counter;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                loadCompleteStarted = ++counter;
            }

            public bool IsReadyToVerify => asyncLoadStarted > 0 && asyncTaskFinished > 0 && asyncLoadFinished > 0 && loadCompleteStarted > 0;

            public void Verify()
            {
                Assert.Multiple(() =>
                {
                    Assert.That(asyncLoadStarted, Is.EqualTo(1));
                    Assert.That(asyncTaskFinished, Is.EqualTo(2));
                    Assert.That(asyncLoadFinished, Is.EqualTo(3));
                    Assert.That(loadCompleteStarted, Is.EqualTo(4));
                });
            }
        }
#pragma warning restore OFSG001
    }
}
