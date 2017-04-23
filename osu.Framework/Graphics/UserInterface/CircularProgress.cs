using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.UserInterface
{

    public class CircularProgress : Container, IHasCurrentValue<double>
    {
        private List<Triangle> triangles = new List<Triangle>();
        private Container trianglesContainer;

        private float clockRadius = 0.5f;
        private float clockWidth = 1.0f;

        public Bindable<double> Current { get; } = new Bindable<double>();

        public CircularProgress()
        {
            Current.ValueChanged += updateTriangles;

            for (int i = 0; i < 8; i++)
            {
                triangles.Add(new Triangle
                {
                    Origin = Anchor.BottomRight,
                    Anchor = Anchor.Centre,

                    Width = clockWidth,
                    Height = clockRadius,

                    Rotation = (45 * i) + 90,
                });
            }

            Children = new Drawable[] {
                trianglesContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,

                    Masking = true,

                    Width = clockWidth,
                    Height = clockWidth,
                    CornerRadius = clockRadius,

                    Children = triangles,
                },
            };
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            trianglesContainer.Scale = DrawSize;
        }

        internal void updateTriangles(double T)
        {
            int num_maxed = (int)Math.Floor(T * 8);
            for (int i = 0; i < num_maxed && i < 8; i++)
            {
                triangles[i].Height = clockRadius;
                triangles[i].Alpha = 1;
            }
            if (0 <= num_maxed && num_maxed < 8)
            {
                double T_here = T * 8 - num_maxed;
                triangles[num_maxed].Height = (float)Math.Tan(T_here * Math.PI / 4) * clockRadius;
                triangles[num_maxed].Alpha = 1;
            }
            for (int i = num_maxed + 1; i < 8; i++)
            {
                triangles[i].Height = 0;
                triangles[i].Alpha = 0;
            }
        }
    }
}
