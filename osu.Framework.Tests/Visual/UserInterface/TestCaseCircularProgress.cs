// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestCaseCircularProgress : TestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(CircularProgress), typeof(AnnularDrawNode) };

        private readonly CircularProgress clock;

        private int rotateMode;
        private const double period = 4000;
        private const double transition_period = 2000;

        public TestCaseCircularProgress()
        {
            Children = new Drawable[]
            {
                clock = new CircularProgress
                {
                    Width = 0.8f,
                    Height = 0.8f,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };

            AddStep("Forward", delegate { rotateMode = 1; });
            AddStep("Backward", delegate { rotateMode = 2; });
            AddStep("Transition Focus", delegate { rotateMode = 3; });
            AddStep("Transition Focus 2", delegate { rotateMode = 4; });
            AddStep("Forward/Backward", delegate { rotateMode = 0; });

            AddSliderStep("Fill", 0, 10, 10, fill => clock.InnerRadius = fill / 10f);
        }

        protected override void Update()
        {
            base.Update();
            switch (rotateMode)
            {
                case 0:
                    clock.Current.Value = Time.Current % (period * 2) / period - 1;
                    break;
                case 1:
                    clock.Current.Value = Time.Current % period / period;
                    break;
                case 2:
                    clock.Current.Value = Time.Current % period / period - 1;
                    break;
                case 3:
                    clock.Current.Value = Time.Current % transition_period / transition_period / 5 - 0.1f;
                    break;
                case 4:
                    clock.Current.Value = (Time.Current % transition_period / transition_period / 5 - 0.1f + 2) % 2 - 1;
                    break;
            }
        }
    }
}
