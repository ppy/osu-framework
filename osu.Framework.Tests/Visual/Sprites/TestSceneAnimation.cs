// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneAnimation : FrameworkTestScene
    {
        private readonly Container animationContainer;
        private readonly SpriteText timeText;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TextureAnimation),
            typeof(Animation<>)
        };

        private ManualClock clock;

        private TestAnimation animation;

        public TestSceneAnimation()
        {
            Children = new Drawable[]
            {
                animationContainer = new Container { RelativeSizeAxes = Axes.Both },
                timeText = new SpriteText { Text = "Animation is loading..." }
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("load video", () =>
            {
                animationContainer.Child = animation = new TestAnimation
                {
                    Repeat = false,
                    Clock = new FramedClock(clock = new ManualClock()),
                };
            });

            AddUntilStep("Wait for video to load", () => animation.IsLoaded);
            AddStep("Reset clock", () => clock.CurrentTime = 0);
        }

        [Test]
        public void TestJumpForward()
        {
            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10000);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition >= 10000);
        }

        [Test]
        public void TestJumpBack()
        {
            AddStep("Jump ahead by 10 seconds", () => clock.CurrentTime += 10000);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition >= 10000);

            AddStep("Jump back by 10 seconds", () => clock.CurrentTime -= 10000);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition < 10000);
        }

        [Test]
        public void TestAnimationDoesNotLoopIfDisabled()
        {
            AddStep("Seek to end", () => clock.CurrentTime = animation.Duration);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition >= animation.Duration - 1000);

            AddWaitStep("Wait for playback", 10);
            AddAssert("Not looped", () => animation.PlaybackPosition >= animation.Duration - 1000);
        }

        [Test]
        public void TestAnimationLoopsIfEnabled()
        {
            AddStep("Set looping", () => animation.Repeat = true);
            AddStep("Seek to end", () => clock.CurrentTime = animation.Duration - 2000);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition >= animation.Duration - 1000);

            AddWaitStep("Wait for playback", 10);
            AddUntilStep("Looped", () => animation.PlaybackPosition < animation.Duration - 1000);
        }

        protected override void Update()
        {
            base.Update();

            if (clock != null)
                clock.CurrentTime += Clock.ElapsedFrameTime;

            if (animation != null)
            {
                timeText.Text = $"playback: {animation.PlaybackPosition:N0} current frame: {animation.CurrentFrameIndex} total frames: {animation.FramesProcessed}";
            }
        }

        private class TestAnimation : TextureAnimation
        {
            [Resolved]
            private FontStore fontStore { get; set; }

            public int FramesProcessed;

            public TestAnimation()
                : base(false)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            protected override void DisplayFrame(Texture content)
            {
                FramesProcessed++;
                base.DisplayFrame(content);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                for (int i = 0; i <= 72; i++)
                {
                    AddFrame(new Texture(fontStore.Get(null, (char)('0' + i)).Texture.TextureGL)
                    {
                        ScaleAdjust = 1 + i / 40f,
                    }, 250);
                }
            }
        }
    }
}
