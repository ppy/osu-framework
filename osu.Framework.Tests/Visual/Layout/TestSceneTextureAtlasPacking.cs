// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using System.Collections.Generic;
using System;

namespace osu.Framework.Tests.Visual.Layout
{
    public partial class TestSceneTextureAtlasPacking : FrameworkTestScene
    {
        private const int atlas_size = 1024;

        private readonly Container placed;
        private readonly Box toPlace;

        public TestSceneTextureAtlasPacking()
        {
            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(atlas_size),
                Scale = new Vector2(0.5f),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray
                    },
                    placed = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    toPlace = new Box
                    {
                        Colour = Color4.Cyan
                    }
                }
            });
        }

        private int width;
        private int height;
        private Vector2I positionToPlace;
        private Vector2I lastPlacedTopRight;
        private readonly List<RectangleI> subTextureBounds = new List<RectangleI>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("Reset", reset);

            AddSliderStep("Width", 1, atlas_size, 100, w =>
            {
                width = w;
                updatePosition();
            });

            AddSliderStep("Height", 1, atlas_size, 100, h =>
            {
                height = h;
                updatePosition();
            });

            AddStep("Place", place);
        }

        private void reset()
        {
            placed.Clear();
            positionToPlace = Vector2I.Zero;
            lastPlacedTopRight = Vector2I.Zero;
            subTextureBounds.Clear();

            updatePosition();
        }

        private void place()
        {
            subTextureBounds.Add(new RectangleI(positionToPlace.X, positionToPlace.Y, width, height));
            lastPlacedTopRight = new Vector2I(positionToPlace.X + width, positionToPlace.Y);

            placed.Add(new Box
            {
                Size = new Vector2(width, height),
                Position = positionToPlace
            });

            updatePosition();
        }

        private void updatePosition()
        {
            var available = getAvailablePosition(lastPlacedTopRight, true);

            toPlace.FadeColour(available.HasValue ? Color4.Cyan : Color4.Red, 250, Easing.OutQuint);
            toPlace.Size = new Vector2(width, height);

            if (!available.HasValue)
                return;

            positionToPlace = available.Value;
            toPlace.MoveTo(positionToPlace, 250, Easing.OutQuint);
        }

        private Vector2I? getAvailablePosition(Vector2I currentPosition, bool checkBounds)
        {
            if (currentPosition.Y + height > atlas_size)
                return null;

            if (currentPosition.X + width > atlas_size)
            {
                if (checkBounds)
                {
                    int maxY = 0;

                    foreach (RectangleI bounds in subTextureBounds)
                        maxY = Math.Max(maxY, bounds.Bottom);

                    return getAvailablePosition(new Vector2I(0, maxY), false);
                }

                return null;
            }

            return currentPosition;
        }
    }
}
