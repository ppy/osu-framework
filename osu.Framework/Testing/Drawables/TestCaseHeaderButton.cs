// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestCaseHeaderButton : TestCaseButton
    {
        private readonly SpriteText headerSprite;

        public TestCaseHeaderButton(string header)
            : base(header)
        {
            Add(headerSprite = new SpriteText
            {
                TextSize = 20,
                Colour = Color4.LightGray,
                Padding = new MarginPadding { Right = 5 },
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
            });
        }

        public override void Hide() => headerSprite.Text = "...";

        public override void Show() => headerSprite.Text = string.Empty;
    }
}