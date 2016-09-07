//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Timing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseClocks : TestCase
    {
        private ClockDisplay audioTrackClock;
        internal override string Name => @"Clocks";
        internal override string Description => @"Timing mechanisms";

        internal CustomClockContainer Container;

        internal override void Reset()
        {
            base.Reset();

            if (BeatmapManager.Current == null)
            {
                Debug.Print("No audio available for this test!");
                return;
            }

            Game.Audio.LoadAudio(BeatmapManager.Current);
            Game.Audio.AllowRandomSong = false;

            FlowContainer flow = new FlowContainer(FlowDirection.HorizontalOnly)
            {
                Padding = new Vector2(5)
            };
            Add(flow);

            flow.Add(new ClockDisplay(Game.Clock, @"Game.Clock"));
            flow.Add(audioTrackClock = new ClockDisplay(Game.Audio.AudioTrack, @"Audio.AudioTrack"));
            flow.Add(new ClockDisplay(Game.Audio.Clock.Source, @"Audio.Clock.Decoupled"));
            flow.Add(new ClockDisplay(Game.Audio.Clock, @"Audio.Clock.Offset"));

            SliderBar audioAdjustSlider = new SliderBar(0, -2, 2, 1, new Vector2(0, 100), 300)
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre
            };
            audioAdjustSlider.OnValueChanged += delegate
            {
                Game.Audio.AudioTrack.GraduallyChangeFrequency((float)audioAdjustSlider.Current);
            };

            Add(audioAdjustSlider);

            Game.Audio.OnNewAudioTrack += delegate
            {
                if (audioTrackClock != null)
                    flow.Remove(audioTrackClock);
                flow.Add(audioTrackClock = new ClockDisplay(Game.Audio.AudioTrack, @"Audio.AudioTrack"));
            };

            Game.Audio.AllowUncoupledSeeks = true;
            Game.Audio.Seek(-1000);

            Add(Container = new CustomClockContainer(Game.Audio.Clock));
        }

        class ClockDisplay : Container
        {
            public ClockDisplay(IClock clock, string name)
            {
                SizeMode = InheritMode.Fixed;
                Size = new Vector2(150, 150);

                Box box = new Box(Color4.Aqua)
                {
                    SizeMode = InheritMode.Inherit,
                    Alpha = 0.2f
                };
                Add(box);

                FlowContainer flow = new FlowContainer(FlowDirection.VerticalOnly);
                Add(flow);

                flow.Add(new SpriteText(name, 12));

                SpriteText current = new SpriteText(string.Empty);
                flow.Add(current);
                SpriteText elapsed = new SpriteText(string.Empty);
                flow.Add(elapsed);
                SpriteText drift = new SpriteText(string.Empty);
                flow.Add(drift);

                CircularProgress circular = new CircularProgress()
                {
                    Alpha = 0.1f,
                    SizeMode = InheritMode.Inherit,
                    Additive = true
                };
                Add(circular);

                TextAwesome icon = new TextAwesome()
                {
                    TextSize = 20,
                    Alpha = 0.5f,
                    Origin = Anchor.BottomRight,
                    Anchor = Anchor.BottomRight
                };
                Add(icon);

                OnUpdate += delegate
                {
                    icon.Icon = clock.IsRunning ? FontAwesome.play : FontAwesome.pause;
                    circular.Progress = (float)(clock.CurrentTime % 1000) / 1000;
                    current.Text = @"current:" + clock.CurrentTime.ToString(@"#,0");
                    elapsed.Text = @"elapsed:" + (clock as IFrameBasedClock)?.ElapsedFrameTime.ToString(@"#,0.##");

                    InterpolatingFramedClock interpolating = clock as InterpolatingFramedClock;
                    OffsetClock offset = clock as OffsetClock;
                    if (interpolating != null)
                    {
                        drift.Text = @"drift:" + interpolating.Drift.ToString(@"#,0.##");
                    }

                    if (offset != null)
                    {
                        drift.Text = @" offset:" + (clock as OffsetClock)?.Offset.ToString(@"#,0.##");
                    }
                };
            }
        }
    }
}
