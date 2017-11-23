// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description(@"Checking for bugged corner radius (dependent on FPS)")]
    internal class TestCaseCircularContainer : TestCase
    {
        public TestCaseCircularContainer()
        {
            var circularContainer = new TestCircularContainer
            {
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Blending = BlendingMode.Additive,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                BorderThickness = 5,
                BorderColour = new Color4(45, 45, 45, 255),
                Children = new[]
                {
                    new Box
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Width = 128f,
                        Height = 128f,
                        Colour = Color4.DimGray,
                    },
                },
            };

            AddToggleStep("Fade box in and out", clicked => circularContainer.DoFadeOnBox = clicked);

            Add(circularContainer);
        }

        private class TestCircularContainer : CircularContainer
        {
            public bool DoFadeOnBox;

            private int i;
            private bool isExpanded;
            public bool IsExpanded
            {
                get { return isExpanded; }
                set
                {
                    isExpanded = value;

                    this.ScaleTo(isExpanded ? 2f : 1);
                    if (DoFadeOnBox) Child.FadeTo(isExpanded ? 1f : 0);
                    else Child.FadeIn();
                }
            }

            protected override void Update()
            {
                base.Update();

                // Change the 250 here if you want it to scale and fade more often:
                // smaller number -> more actions and vice versa
                if (i++ % 250 == 0)
                    IsExpanded = !IsExpanded;
            }
        }
    }
}
