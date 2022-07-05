// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkDrawableAudioWrapper : GameBenchmark
    {
        [Test]
        [Benchmark]
        public void TransferBetweenParentAdjustmentContainers()
        {
            ((TestGame)Game).TransferBetween = true;
            RunSingleFrame();
        }

        [Test]
        [Benchmark]
        public void TransferToSameParentAdjustmentContainer()
        {
            ((TestGame)Game).TransferBetween = false;
            RunSingleFrame();
        }

        protected override Game CreateGame() => new TestGame();

        private class TestGame : Game
        {
            public readonly AudioContainer Container1 = new AudioContainer { RelativeSizeAxes = Axes.Both };
            public readonly AudioContainer Container2 = new AudioContainer { RelativeSizeAxes = Axes.Both };

            public readonly AudioContainer Sample = new AudioContainer(); // usually a sample, but we're just testing the base class here

            public bool TransferBetween { get; set; }

            private AudioContainer? lastContainer;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                InternalChildren = new Drawable[]
                {
                    Container1,
                    Container2,
                };

                transferTo(Container1);
            }

            private void transferTo(AudioContainer target)
            {
                lastContainer?.Remove(Sample);
                target.Add(Sample);
                lastContainer = target;
            }

            protected override void Update()
            {
                base.Update();

                if (TransferBetween)
                {
                    transferTo(lastContainer == Container1 ? Container2 : Container1);
                }
                else
                {
                    // simulates the case of pooling, where a sample is usually transferred to the same parent.
                    transferTo(Container1);
                }
            }
        }
    }
}
