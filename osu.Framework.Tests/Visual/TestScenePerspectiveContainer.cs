// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public partial class TestScenePerspectiveContainer : FrameworkTestScene
    {
        private readonly Drawable target;
        private readonly Sprite sprite;

        private float xRotation;
        private float yRotation;

        public TestScenePerspectiveContainer()
        {
            Add(target = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Child = sprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both
                }
            });

            AddSliderStep("X Rotation", 0, MathF.PI, 0, v =>
            {
                xRotation = v;
                updateRotation();
            });

            AddSliderStep("Y Rotation", 0, MathF.PI, 0, v =>
            {
                yRotation = v;
                updateRotation();
            });
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            sprite.Texture = textures.Get(@"sample-texture");
        }

        private void updateRotation()
        {
            target.TransformTo(nameof(ExtraRotation), new Quaternion(xRotation, yRotation, 0), 500, Easing.OutQuint);
        }
    }
}
