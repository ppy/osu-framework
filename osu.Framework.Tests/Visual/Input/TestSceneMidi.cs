// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneMidi : FrameworkTestScene
    {
        public TestSceneMidi()
        {
            var keyFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            for (MidiKey k = MidiKey.A0; k < MidiKey.C8; k++)
                keyFlow.Add(new MidiKeyHandler(k));

            Child = keyFlow;
        }

        [Resolved]
        private GameHost host { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddToggleStep("toggle midi handler", enabled =>
            {
                var midiHandler = host.AvailableInputHandlers.OfType<MidiHandler>().FirstOrDefault();

                if (midiHandler != null)
                    midiHandler.Enabled.Value = enabled;
            });
        }

        protected override bool OnMidiDown(MidiDownEvent e)
        {
            Logger.Log(e.ToString());
            return base.OnMidiDown(e);
        }

        protected override void OnMidiUp(MidiUpEvent e)
        {
            Logger.Log(e.ToString());
            base.OnMidiUp(e);
        }

        protected override bool Handle(UIEvent e)
        {
            if (!(e is MouseEvent))
                Logger.Log("Event: " + e);
            return base.Handle(e);
        }

        private class MidiKeyHandler : CompositeDrawable
        {
            private readonly Drawable background;

            public override bool HandleNonPositionalInput => true;

            private readonly MidiKey key;

            public MidiKeyHandler(MidiKey key)
            {
                this.key = key;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = key.ToString().Replace("Sharp", "#")
                    }
                };
            }

            protected override bool OnMidiDown(MidiDownEvent e)
            {
                if (e.Key != key)
                    return base.OnMidiDown(e);

                const float base_opacity = 0.25f; // to make a velocity of 1 not completely invisible

                background.FadeTo(base_opacity + e.Velocity / 128f * (1 - base_opacity), 100, Easing.OutQuint);
                return true;
            }

            protected override void OnMidiUp(MidiUpEvent e)
            {
                if (e.Key != key)
                    base.OnMidiUp(e);
                else
                    background.FadeOut(100);
            }
        }
    }
}
