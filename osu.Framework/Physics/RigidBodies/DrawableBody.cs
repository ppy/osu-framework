// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System;
using System.Drawing;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Physics.RigidBodies
{
    /// <summary>
    /// Links a <see cref="RigidBody"/> with a <see cref="Drawable"/> such that their state
    /// is interconnected.
    /// </summary>
    class DrawableBody : RigidBody
    {
        /// <summary>
        /// The <see cref="Drawable"/> this rigid body is associated with.
        /// </summary>
        public Drawable Drawable;

        public DrawableBody(Drawable d, RigidBodySimulation sim) : base(sim)
        {
            Drawable = d;
        }

        protected override float ComputeI()
        {
            Matrix3 A = Drawable.DrawInfo.Matrix * Drawable.Parent.DrawInfo.MatrixInverse;
            Vector2 size = Drawable.DrawSize;

            // Inertial moment for a linearly transformed rectangle with a given size around its center.
            return (
                ((A.M11 * A.M11) + (A.M12 * A.M12)) * size.X * size.X +
                ((A.M21 * A.M21) + (A.M22 * A.M22)) * size.Y * size.Y
            ) * m / 12;
        }

        protected override void UpdateVertices()
        {
            Vertices.Clear();
            Normals.Clear();

            float cornerRadius = (Drawable as IContainer)?.CornerRadius ?? 0;
            
            // Sides
            RectangleF rect = Drawable.DrawRectangle;
            Vector2[] corners = new[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };
            const int AMOUNT_SIDE_STEPS = 2;

            for (int i = 0; i < 4; ++i)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                float length = (b - a).Length;
                float usableLength = Math.Max((b - a).Length - 2 * cornerRadius, 0);

                Vector2 diff = (b - a);
                Vector2 normal = (b - a).PerpendicularRight.Normalized();

                for (int j = 0; j < AMOUNT_SIDE_STEPS; ++j)
                {
                    Vertices.Add(a + ((b - a) / length) * (cornerRadius + j * usableLength / (AMOUNT_SIDE_STEPS - 1)));
                    Normals.Add(normal);
                }
            }

            const int AMOUNT_CORNER_STEPS = 10;
            if (cornerRadius > 0)
            {
                // Rounded corners
                Vector2[] offsets = new[] {
                    new Vector2(cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, -cornerRadius),
                    new Vector2(cornerRadius, -cornerRadius),
                };

                for (int i = 0; i < 4; ++i)
                {
                    Vector2 a = corners[i];

                    float startTheta = (i - 1) * (float)Math.PI / 2;

                    for (int j = 0; j < AMOUNT_CORNER_STEPS; ++j)
                    {
                        float theta = startTheta + ((float)j / (AMOUNT_CORNER_STEPS - 1)) * (float)Math.PI / 2;

                        Vector2 normal = new Vector2((float)Math.Sin(theta), (float)Math.Cos(theta));
                        Vertices.Add(a + offsets[i] + normal * cornerRadius);
                        Normals.Add(normal);
                    }
                }
            }

            // To simulation space
            Matrix3 mat = Drawable.DrawInfo.Matrix * ScreenToSimulationSpace;
            Matrix3 normMat = mat.Inverted();
            normMat.Transpose();

            // Remove translation
            normMat.M31 = normMat.M32 = normMat.M13 = normMat.M23 = 0;
            Vector2 translation = Vector2.Zero * normMat;

            for (int i = 0; i < Vertices.Count; ++i)
            {
                Vertices[i] *= mat;
                Normals[i] = (Normals[i] * normMat - translation).Normalized();
            }
        }

        public override void ReadState()
        {
            Matrix3 mat = Drawable.Parent.DrawInfo.Matrix * ScreenToSimulationSpace;
            c = Drawable.BoundingBox.Centre * mat;
            r = MathHelper.DegreesToRadians(Drawable.Rotation); // TODO: Fix rotations

            base.ReadState();
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Matrix3 mat = SimulationToScreenSpace * Drawable.Parent.DrawInfo.MatrixInverse;
            Drawable.Position = c * mat + (Drawable.Position - Drawable.BoundingBox.Centre);
            Drawable.Rotation = MathHelper.RadiansToDegrees(r); // TODO: Fix rotations
        }

        public override Rectangle ScreenSpaceAABB => Drawable.ScreenSpaceDrawQuad.AABB;

        public override bool Contains(Vector2 screenSpacePos) => Drawable.Contains(screenSpacePos);
    }
}
