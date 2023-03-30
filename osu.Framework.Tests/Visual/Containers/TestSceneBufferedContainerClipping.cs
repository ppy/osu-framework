// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers;

public partial class TestSceneBufferedContainerClipping : FrameworkGridTestScene
{
    public TestSceneBufferedContainerClipping()
        : base(1, 2)
    {
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        Cell(0).Child = new Container
        {
            Size = new Vector2(300),
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Children = createContents()
        };

        Cell(1).Child = new BufferedContainer
        {
            Size = new Vector2(300),
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Children = createContents()
        };

        Add(new TextFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            TextAnchor = Anchor.TopCentre,
            Margin = new MarginPadding { Top = 10 },
            Text = "If clipping within buffered containers is working properly, both images below should be identical."
        });
    }

    private Drawable[] createContents() => new Drawable[]
    {
        new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = Colour4.Red
        },
        new CircularContainer
        {
            Size = new Vector2(200),
            RelativePositionAxes = Axes.Both,
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre,
            Masking = true,
            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.Blue
            }
        }
    };
}
