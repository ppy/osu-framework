// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Testing.Drawables.Sections
{
    public partial class ToolbarRunAllStepsSection : ToolbarSection
    {
        public ToolbarRunAllStepsSection()
        {
            AutoSizeAxes = Axes.X;
            Masking = false;
        }

        [BackgroundDependencyLoader]
        private void load(TestBrowser browser)
        {
            InternalChild = new FillFlowContainer
            {
                Spacing = new Vector2(5),
                Direction = FillDirection.Horizontal,
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Children = new Drawable[]
                {
                    new BasicCheckbox
                    {
                        LabelText = "Run all steps",
                        RightHandedCheckbox = true,
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Current = browser.RunAllSteps
                    },
                }
            };
        }
    }
}
