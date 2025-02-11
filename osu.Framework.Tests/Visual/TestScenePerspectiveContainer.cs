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
        private readonly Container target;
        private readonly Sprite sprite;

        private float xRotation;
        private float yRotation;
        private float zRotation;

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

            AddSliderStep("X Rotation", -MathF.PI, MathF.PI, 0, v =>
            {
                xRotation = v;
                updateRotation();
            });

            AddSliderStep("Y Rotation", -MathF.PI, MathF.PI, 0, v =>
            {
                yRotation = v;
                updateRotation();
            });

            AddSliderStep("Z Rotation", -MathF.PI, MathF.PI, 0, v =>
            {
                zRotation = v;
                updateRotation();
            });

            AddSliderStep("fov", 0, 90f, 45f, updateFov);
        }

        private void updateFov(float fov)
        {
            target.Camera = new Camera(fov, new Vector2(0.5f));
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            sprite.Texture = textures.Get(@"sample-texture");
        }

        private void updateRotation()
        {
            target.TransformTo(nameof(Rotation3D), new Quaternion(xRotation, yRotation, zRotation), 500, Easing.OutQuint);
        }
    }
}
