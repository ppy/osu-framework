// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A circular progress circle.
    /// </summary>
    public class CircularProgress : Container, IHasCurrentValue<double>
    {
        /// <summary>
        /// Stores 8 triangles that we use to build up 8 different sectors.
        /// </summary>
        private Triangle[] triangles = new Triangle[8];

        /// <summary>
        /// The container that masks the triangles such that they look like sectors.
        /// We also Scale this to the right size,
        /// that way all out triangles can have a fixed size of 0.5.
        /// </summary>
        private Container trianglesContainer;

        private float radius = 0.5f;
        private float diameter = 1.0f;

        public Bindable<double> Current { get; } = new Bindable<double>();

        public CircularProgress()
        {
            Current.ValueChanged += updateTriangles;

            for (int i = 0; i < 8; i++)
            {
                triangles[i] = new Triangle
                {
                    Origin = Anchor.BottomRight,
                    Anchor = Anchor.Centre,

                    Width = diameter,
                    Height = 0,
                    Alpha = 0,

                    Rotation = (45 * i) + 90,
                };
            }

            Children = new Drawable[] {
                trianglesContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,

                    Masking = true,

                    Width = diameter,
                    Height = diameter,
                    CornerRadius = radius,

                    Children = triangles,
                },
            };
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            trianglesContainer.Scale = DrawSize;
        }

        /// <summary>
        /// Adjusts the height of each triangle such that the angle formed corresponds to the Current value.
        /// </summary>
        /// <param name="newValue">A double between 0.0 and 1.0 corresponding to empty to filled respectively.</param>
        internal void updateTriangles(double newValue)
        {
            // Number of sectors that are "maxed out"
            // Also the index of the sector that needs to be calculated.
            int num_maxed = (int)Math.Floor(newValue * 8);
            for (int i = 0; i < num_maxed && i < 8; i++)
            {
                triangles[i].Height = radius;
                triangles[i].Alpha = 1;
            }
            if (0 <= num_maxed && num_maxed < 8)
            {
                // T_here is the progress of this specific sector.
                double T_here = newValue * 8 - num_maxed;
                triangles[num_maxed].Height = (float)Math.Tan(T_here * Math.PI / 4) * radius;
                triangles[num_maxed].Alpha = 1;
            }
            for (int i = Math.Max(0, num_maxed + 1); i < 8; i++)
            {
                triangles[i].Height = 0;
                triangles[i].Alpha = 0;
            }
        }
    }
}
