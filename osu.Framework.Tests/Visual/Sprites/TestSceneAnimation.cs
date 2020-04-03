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
        private SpriteText timeText;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TextureAnimation),
            typeof(Animation<>),
            typeof(AnimationClockComposite)
        };

        private ManualClock clock;

        private TestAnimation animation;
        private Container animationContainer;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("load container", () =>
            {
                Children = new Drawable[]
                {
                    animationContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Clock = new FramedClock(clock = new ManualClock()),
                    },
                    timeText = new SpriteText { Text = "Animation is loading..." }
                };
            });

            loadNewAnimation();

            AddStep("Reset clock", () => clock.CurrentTime = 0);
        }

        [Test]
        public void TestFrameSeeking()
        {
            AddAssert("frame count is correct", () => animation.FrameCount == TestAnimation.LOADABLE_FRAMES);
            AddUntilStep("wait for frames to pass", () => animation.CurrentFrameIndex > 10);
            AddStep("stop animation", () => animation.Stop());
            AddAssert("is stopped", () => !animation.IsPlaying);

            AddStep("goto frame 60", () => animation.GotoFrame(60));
            AddAssert("is at frame 60", () => animation.CurrentFrameIndex == 60);

            AddStep("goto frame 30", () => animation.GotoFrame(30));
            AddAssert("is at frame 30", () => animation.CurrentFrameIndex == 30);

            AddStep("goto frame 60", () => animation.GotoFrame(60));
            AddAssert("is at frame 60", () => animation.CurrentFrameIndex == 60);

            AddStep("start animation", () => animation.Play());
            AddUntilStep("continues to frame 70", () => animation.CurrentFrameIndex == 70);
        }

        [Test]
        public void TestStartFromCurrentTime()
        {
            AddAssert("Animation is near start", () => animation.PlaybackPosition < 1000);

            AddWaitStep("Wait some", 20);

            loadNewAnimation();

            AddAssert("Animation is near start", () => animation.PlaybackPosition < 1000);
        }

        [Test]
        public void TestStoppedAnimationIsAtZero()
        {
            loadNewAnimation(postLoadAction: a => a.Stop());
            AddAssert("Animation is at start", () => animation.PlaybackPosition == 0);
        }

        [Test]
        public void TestStoppedAnimationIsAtSpecifiedFrame()
        {
            loadNewAnimation(postLoadAction: a => a.GotoAndStop(2));
            AddAssert("Animation is at specific frame", () => animation.PlaybackPosition == 500);
        }

        [Test]
        public void TestStartFromOngoingTime()
        {
            AddWaitStep("Wait some", 20);

            loadNewAnimation(false);

            AddAssert("Animation is not near start", () => animation.PlaybackPosition > 1000);
        }

        [Test]
        public void TestSetCustomClockWithCurrentTime()
        {
            AddAssert("Animation is near start", () => animation.PlaybackPosition < 1000);

            AddUntilStep("Animation is not near start", () => animation.PlaybackPosition > 1000);

            double posBefore = 0;

            AddStep("store position", () => posBefore = animation.PlaybackPosition);

            AddStep("Set custom clock", () => animation.Clock = new FramedOffsetClock(null) { Offset = 10000 });

            AddAssert("Animation continued playing at current position", () => animation.PlaybackPosition - posBefore < 1000);
        }

        [Test]
        public void TestSetCustomClockWithOngoingTime()
        {
            loadNewAnimation(false);

            AddAssert("Animation is near start", () => animation.PlaybackPosition < 1000);

            AddUntilStep("Animation is not near start", () => animation.PlaybackPosition > 1000);

            AddStep("Set custom clock", () => animation.Clock = new FramedOffsetClock(null) { Offset = 10000 });

            AddAssert("Animation is not near start", () => animation.PlaybackPosition > 1000);
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
            AddStep("Set looping", () => animation.Loop = true);
            AddStep("Seek to end", () => clock.CurrentTime = animation.Duration - 2000);
            AddUntilStep("Animation seeked", () => animation.PlaybackPosition >= animation.Duration - 1000);

            AddWaitStep("Wait for playback", 10);
            AddUntilStep("Looped", () => animation.PlaybackPosition < animation.Duration - 1000);
        }

        [Test]
        public void TestTransformBeforeLoaded()
        {
            AddStep("set time to future", () => clock.CurrentTime = 10000);

            loadNewAnimation(postLoadAction: a =>
            {
                a.Alpha = 0;
                a.FadeInFromZero(10).Then().FadeOutFromOne(1000);
            });

            AddAssert("Is visible", () => animation.Alpha > 0);
        }

        [Test]
        public void TestStartFromFutureTimeWithInitialSeek()
        {
            AddStep("set time to future", () => clock.CurrentTime = 10000);

            loadNewAnimation(false, a =>
            {
                a.PlaybackPosition = -10000;
            });

            AddAssert("Animation is at beginning", () => animation.PlaybackPosition < 1000);
        }

        private void loadNewAnimation(bool startFromCurrent = true, Action<TestAnimation> postLoadAction = null)
        {
            AddStep("load animation", () =>
            {
                animationContainer.Child = animation = new TestAnimation(startFromCurrent)
                {
                    Loop = false,
                };

                postLoadAction?.Invoke(animation);
            });

            AddUntilStep("Wait for animation to load", () => animation.IsLoaded);
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
            public const int LOADABLE_FRAMES = 72;

            [Resolved]
            private FontStore fontStore { get; set; }

            public int FramesProcessed;

            public TestAnimation(bool startFromCurrent)
                : base(startFromCurrent)
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
                for (int i = 0; i < LOADABLE_FRAMES; i++)
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
