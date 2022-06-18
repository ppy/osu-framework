// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicPopover : Popover
    {
        public BasicPopover()
        {
            Background.Colour = FrameworkColour.GreenDark;
            Content.Padding = new MarginPadding(10);
        }

        protected override Drawable CreateArrow() => new EquilateralTriangle
        {
            Colour = FrameworkColour.GreenDark,
            Origin = Anchor.TopCentre
        };

        protected override void AnchorUpdated(Anchor anchor)
        {
            base.AnchorUpdated(anchor);

            bool isCenteredAnchor = anchor.HasFlagFast(Anchor.x1) || anchor.HasFlagFast(Anchor.y1);
            Body.Margin = new MarginPadding(isCenteredAnchor ? 10 : 3);
            Arrow.Size = new Vector2(isCenteredAnchor ? 12 : 15);
        }
    }
}
