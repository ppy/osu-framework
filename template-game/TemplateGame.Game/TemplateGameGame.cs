// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace TemplateGame.Game
{
    public class TemplateGameGame : osu.Framework.Game
    {
        private Box box;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Add your game components here.
            // The rotating box can be removed.

            Child = box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = Color4.Orange,
                Size = new Vector2(200),
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            box.Loop(b => b.RotateTo(0).RotateTo(360, 2500));
        }
    }
}
