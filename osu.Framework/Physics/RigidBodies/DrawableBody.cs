// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using System;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Physics.RigidBodies
{
    /// <summary>
    /// Links a <see cref="RigidBody"/> with a <see cref="Drawable"/> such that their state
    /// is interconnected.
    /// </summary>
    public class DrawableBody : RigidBody
    {
        /// <summary>
        /// The <see cref="Drawable"/> this rigid body is associated with.
        /// </summary>
        private readonly Drawable drawable;

        public DrawableBody(Drawable d, RigidBodySimulation sim) : base(sim)
        {
            drawable = d;
        }

        protected override float ComputeI()
        {
            Matrix3 mat = drawable.DrawInfo.Matrix * drawable.Parent.DrawInfo.MatrixInverse;
            Vector2 size = drawable.DrawSize;

            // Inertial moment for a linearly transformed rectangle with a given size around its center.
            return (
                (mat.M11 * mat.M11 + mat.M12 * mat.M12) * size.X * size.X +
                (mat.M21 * mat.M21 + mat.M22 * mat.M22) * size.Y * size.Y
            ) * Mass / 12;
        }

        protected override void UpdateVertices()
        {
            Vertices.Clear();
            Normals.Clear();

            float cornerRadius = (drawable as IContainer)?.CornerRadius ?? 0;

            // Sides
            RectangleF rect = drawable.DrawRectangle;
            Vector2[] corners = { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };
            const int amount_side_steps = 2;

            for (int i = 0; i < 4; ++i)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                Vector2 diff = b - a;
                float length = diff.Length;
                Vector2 dir = diff / length;

                float usableLength = Math.Max(length - 2 * cornerRadius, 0);

                Vector2 normal = (b - a).PerpendicularRight.Normalized();
                for (int j = 0; j < amount_side_steps; ++j)
                {
                    Vertices.Add(a + dir * (cornerRadius + j * usableLength / (amount_side_steps - 1)));
                    Normals.Add(normal);
                }
            }

            const int amount_corner_steps = 10;
            if (cornerRadius > 0)
            {
                // Rounded corners
                Vector2[] offsets = {
                    new Vector2(cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, -cornerRadius),
                    new Vector2(cornerRadius, -cornerRadius),
                };

                for (int i = 0; i < 4; ++i)
                {
                    Vector2 a = corners[i];

                    float startTheta = (i - 1) * (float)Math.PI / 2;

                    for (int j = 0; j < amount_corner_steps; ++j)
                    {
                        float theta = startTheta + j * (float)Math.PI / (2 * (amount_corner_steps - 1));

                        Vector2 normal = new Vector2((float)Math.Sin(theta), (float)Math.Cos(theta));
                        Vertices.Add(a + offsets[i] + normal * cornerRadius);
                        Normals.Add(normal);
                    }
                }
            }

            // To simulation space
            Matrix3 mat = drawable.DrawInfo.Matrix * ScreenToSimulationSpace;
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
            Matrix3 mat = drawable.Parent.DrawInfo.Matrix * ScreenToSimulationSpace;
            Centre = drawable.BoundingBox.Centre * mat;
            Rotation = MathHelper.DegreesToRadians(drawable.Rotation); // TODO: Fix rotations

            base.ReadState();
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Matrix3 mat = SimulationToScreenSpace * drawable.Parent.DrawInfo.MatrixInverse;
            drawable.Position = Centre * mat + (drawable.Position - drawable.BoundingBox.Centre);
            drawable.Rotation = MathHelper.RadiansToDegrees(Rotation); // TODO: Fix rotations
        }

        public override RectangleI ScreenSpaceAABB => drawable.ScreenSpaceDrawQuad.AABB;

        public override bool Contains(Vector2 screenSpacePos) => drawable.Contains(screenSpacePos);
    }
}
