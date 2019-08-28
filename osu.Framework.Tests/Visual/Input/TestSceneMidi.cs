// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
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

        protected override bool OnMidiDown(MidiDownEvent e)
        {
            Console.WriteLine(e);
            return base.OnMidiDown(e);
        }

        protected override bool OnMidiUp(MidiUpEvent e)
        {
            Console.WriteLine(e);
            return base.OnMidiUp(e);
        }

        protected override bool Handle(UIEvent e)
        {
            if (!(e is MouseEvent))
                Console.WriteLine("Event: " + e);
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

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override bool OnMidiUp(MidiUpEvent e)
            {
                if (e.Key != key)
                    return base.OnMidiUp(e);

                background.FadeOut(100);
                return true;
            }
        }
    }
}
