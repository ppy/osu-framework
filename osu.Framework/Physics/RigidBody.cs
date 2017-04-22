// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using System;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Encapsulates a <see cref="Drawable"/> with additional physical state and methods
    /// necessary for rigid body simulation.
    /// </summary>
    class RigidBody
    {
        /// <summary>
        /// The <see cref="Drawable"/> this rigid body is associated with.
        /// </summary>
        public Drawable Drawable;

        /// <summary>
        /// Controls how elastic the material is. A value of 1 means perfect elasticity
        /// (kinetic energy is fully preserved). A value of 0 means all energy is absorbed
        /// on collision, i.e. no rebound occurs at all.
        /// </summary>
        public float Restitution = 0.25f;

        /// <summary>
        /// How much friction happens between objects.
        /// </summary>
        public float FrictionCoefficient = 1;

        /// <summary>
        /// Centre of mass.
        /// </summary>
        public Vector2 c;

        /// <summary>
        /// Rotation.
        /// </summary>
        public float r;

        /// <summary>
        /// Mass.
        /// </summary>
        public float m;

        /// <summary>
        /// Velocity.
        /// </summary>
        public Vector2 v
        {
            get { return p / m; }
            set { p = value * m; }
        }

        /// <summary>
        /// Momentum.
        /// </summary>
        public Vector2 p;

        /// <summary>
        /// Angular velocity.
        /// </summary>
        public float w
        {
            get { return L / I; }
            set { L = value * I; }
        }

        /// <summary>
        /// Angular momentum.
        /// </summary>
        public float L;

        /// <summary>
        /// Moment of inertia.
        /// </summary>
        public float I { get; private set; }

        /// <summary>
        /// Total velocity at a given location. Includes angular velocity.
        /// </summary>
        public Vector2 VelocityAt(Vector2 pos)
        {
            Vector2 diff = pos - c;

            // Add orthogonal direction to rotation, scaled by distance from centre
            // to the velocity of our centre of mass.
            return v + diff.PerpendicularLeft * w;
        }

        /// <summary>
        /// Contains discrete positions on the surface of this shape used for collision detection.
        /// In the future this can be potentially replaced by closed-form solutions.
        /// </summary>
        private Vector2[] vertices = new Vector2[80];

        /// <summary>
        /// Normals corresponding to the positions inside <see cref="vertices"/>.
        /// </summary>
        private Vector2[] normals = new Vector2[80];

        public RigidBody(Drawable d)
        {
            if (d.Origin != Anchor.Centre)
                throw new InvalidOperationException($@"Non-centre origin drawables (was {d.Origin}) can currently not be rigid bodies.");

            Drawable = d;
            m = 1f; // Arbitrarily 1 kg for now

            // Initially no momenta
            p = Vector2.Zero;
            L = 0;
        }

        private void computeI()
        {
            Matrix3 A = Drawable.DrawInfo.Matrix * Drawable.Parent.DrawInfo.MatrixInverse;
            Vector2 size = Drawable.DrawSize;

            // Inertial moment for a linearly transformed rectangle with a given size around its center.
            I = (
                ((A.M11 * A.M11) + (A.M12 * A.M12)) * size.X * size.X +
                ((A.M21 * A.M21) + (A.M22 * A.M22)) * size.Y * size.Y
            ) * m / 12;
        }

        /// <summary>
        /// Populates <see cref="vertices"/> and <see cref="normals"/>.
        /// </summary>
        private void updateVertices()
        {
            float cornerRadius = (Drawable as IContainer)?.CornerRadius ?? 0;

            // Sides
            RectangleF rect = Drawable.DrawRectangle;
            Vector2[] corners = new[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };

            for (int i = 0; i < 4; ++i)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                float length = (b - a).Length;
                float usableLength = Math.Max((b - a).Length - 2 * cornerRadius, 0);

                Vector2 diff = (b - a);
                Vector2 normal = (b - a).PerpendicularRight.Normalized();

                for (int j = 0; j < 10; ++j)
                {
                    int idx = i * 10 + j;
                    vertices[idx] = a + ((b - a) / length) * (cornerRadius + j * usableLength / 9);
                    normals[idx] = normal;
                }
            }

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

                float startTheta = (i-1) * (float)Math.PI / 2;

                for (int j = 0; j < 10; ++j)
                {
                    float theta = startTheta + ((float)j / 9) * (float)Math.PI / 2;
                    int idx = 40 + i * 10 + j;

                    normals[idx] = new Vector2((float)Math.Sin(theta), (float)Math.Cos(theta));
                    vertices[idx] = a + offsets[i] + normals[idx] * cornerRadius;
                }
            }
        }

        /// <summary>
        /// Applies a given impulse attacking at a given position.
        /// </summary>
        public void ApplyImpulse(Vector2 impulse, Vector2 pos)
        {
            // Offset to our centre of mass. Required to obtain torque
            Vector2 diff = pos - c;

            p += impulse;

            // Cross product between impulse and offset to centre.
            // If they are orthogonal, then the effect on angular momentum is maximized.
            // Intuitively, think of hitting something head-on vs hitting it on the far edge.
            // The first case will not introduce any rotational movement, whereas the latter
            // will.
            L += diff.X * impulse.Y - diff.Y * impulse.X;
        }

        /// <summary>
        /// Helper function for code brevity in <see cref="handleCollision(RigidBody, Vector2, Vector2)"/>.
        /// Can be moved into the function as a nested method once C# 7 is out.
        /// </summary>
        private float impulseDenominator(Vector2 pos, Vector2 normal)
        {
            Vector2 diff = pos - c;
            float perpDot = Vector2.Dot(normal, diff.PerpendicularRight);
            return 1.0f / m + perpDot * perpDot / I;
        }

        /// <summary>
        /// Handles a collision of 2 rigid bodies, given the other body, the impact position,
        /// and the surface normal of this body at the impact position.
        /// This method applies the resulting impulse both to this body and to the other body.
        /// </summary>
        private void handleCollision(RigidBody other, Vector2 pos, Vector2 normal)
        {
            const float EPSILON = 0.001f;

            Vector2 vrel = VelocityAt(pos) - other.VelocityAt(pos);
            float vrelOrtho = -Vector2.Dot(vrel, normal);

            // We don't want to consider collisions where objects move away from each other.
            // (Or with negligible velocity. Let repulsive forces handle these.)
            if (vrelOrtho > -EPSILON)
                return;

            float impulseMagnitude = -(1.0f + Restitution) * vrelOrtho;
            impulseMagnitude /= impulseDenominator(pos, normal) + other.impulseDenominator(pos, normal);

            impulseMagnitude = Math.Max(impulseMagnitude - 0.01f, 0.0f);

            Vector2 impulse = -normal * impulseMagnitude;

            // Add "friction" to the impulse. We arbitrarily reduce the planar velocity relative to the impulse magnitude.
            Vector2 vrelPlanar = vrel + vrelOrtho * normal;
            float vrelPlanarLength = vrelPlanar.Length;
            if (vrelPlanarLength > 0)
                impulse -= vrelPlanar * Math.Min(impulseMagnitude * 0.05f * FrictionCoefficient * other.FrictionCoefficient / vrelPlanarLength, 1);

            ApplyImpulse(impulse, pos);
            other.ApplyImpulse(-impulse, pos);
        }

        /// <summary>
        /// Checks for and records all collisions with another body. If collisions were found,
        /// their aggregate is handled.
        /// </summary>
        public bool CheckAndHandleCollisionWith(RigidBody other)
        {
            if (!other.Drawable.ScreenSpaceDrawQuad.AABB.IntersectsWith(Drawable.ScreenSpaceDrawQuad.AABB))
                return false;

            int amountCollisions = 0;
            Vector2 pos = Vector2.Zero;
            Vector2 normal = Vector2.Zero;

            for (int i = 0; i < vertices.Length; ++i)
            {
                if (other.Drawable.Contains(Drawable.ToScreenSpace(vertices[i])))
                {
                    Matrix3 mat = Drawable.DrawInfo.Matrix * Drawable.Parent.DrawInfo.MatrixInverse;

                    pos += vertices[i] * mat;
                    mat = mat.Inverted();
                    mat.Transpose();
                    mat.M31 = mat.M32 = mat.M13 = mat.M23 = 0;

                    normal += (normals[i] * mat - Vector2.Zero * mat).Normalized();
                    ++amountCollisions;
                }
            }

            if (amountCollisions > 0)
            {
                pos /= amountCollisions;
                normal.Normalize();

                handleCollision(other, pos, normal);
                other.handleCollision(this, pos, -normal);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs an integration step over time. More precisely, updates the
        /// physical state as dependent on time according to the forces and torques
        /// acting on this body.
        /// </summary>
        public void Integrate(Vector2 force, float torque, float dt)
        {
            Vector2 vPrev = v;
            float wPrev = w;
            
            // Update momenta
            p += dt * force;
            L += dt * torque;

            // Update position and rotation given _previous_ velocities. This is a symplectic integration technique, which conserves energy.
            c += dt * vPrev;
            r += dt * wPrev;
        }

        /// <summary>
        /// Reads the positional and rotational state of this rigid body from its <see cref="Drawable"/>.
        /// </summary>
        public void ReadState()
        {
            c = Drawable.Position;
            r = MathHelper.DegreesToRadians(Drawable.Rotation);

            computeI();
            updateVertices();
        }

        /// <summary>
        /// Applies the positional and rotational state of this rigid body to its <see cref="Drawable"/>.
        /// </summary>
        public void ApplyState()
        {
            Drawable.Position = c;
            Drawable.Rotation = MathHelper.RadiansToDegrees(r);
        }
    }
}
