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
        /// The number of triangles used.
        /// </summary>
        private const int num_triangles = 8;

        /// <summary>
        /// Stores 8 triangles that we use to build up 8 different sectors.
        /// </summary>
        private readonly Triangle[] triangles = new Triangle[num_triangles];

        public Bindable<double> Current { get; } = new Bindable<double>();

        public CircularProgress()
        {
            Current.ValueChanged += updateTriangles;

            for (int i = 0; i < num_triangles; i++)
            {
                triangles[i] = new Triangle
                {
                    Origin = Anchor.BottomRight,
                    Anchor = Anchor.Centre,

                    RelativeSizeAxes = Axes.Both,
                    Height = 0,
                    Alpha = 0,

                    Rotation = 45 * i + 90,
                };
            }

            Children = new Drawable[] {
                new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,

                    Masking = true,

                    RelativeSizeAxes = Axes.Both,

                    Children = triangles,
                },
            };
        }

        /// <summary>
        /// Adjusts the height of each triangle such that the angle formed corresponds to the Current value.
        /// </summary>
        /// <param name="newValue">A double between 0.0 and 1.0 corresponding to empty to filled respectively.</param>
        private void updateTriangles(double newValue)
        {
            // Number of sectors that are "maxed out"
            // Also the index of the sector that needs to be calculated.
            int numMaxed = (int)Math.Floor(newValue * num_triangles);
            for (int i = 0; i < numMaxed && i < num_triangles; i++)
            {
                triangles[i].Height = 0.5f;
                triangles[i].Alpha = 1;
            }
            if (0 <= numMaxed && numMaxed < num_triangles)
            {
                // valueHere is the progress of this specific sector.
                double valueHere = newValue * num_triangles - numMaxed;
                triangles[numMaxed].Height = (float)Math.Tan(valueHere * Math.PI / 4) * 0.5f;
                triangles[numMaxed].Alpha = 1;
            }
            for (int i = Math.Max(0, numMaxed + 1); i < num_triangles; i++)
            {
                triangles[i].Height = 0;
                triangles[i].Alpha = 0;
            }
        }
    }
}
