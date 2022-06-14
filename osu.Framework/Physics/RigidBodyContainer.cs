// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Contains physical state and methods necessary for rigid body simulation.
    /// </summary>
    public class RigidBodyContainer<T> : Container<T>, IRigidBody
        where T : Drawable
    {
        public RigidBodyContainer()
        {
            // The code for rigid body simulation requires that the centre of rotation
            // equals the centre of mass, which is the geometric centre in this case.
            Origin = Anchor.Centre;
        }

        public Drawable Simulation { get; set; }

        /// <summary>
        /// Controls how elastic the material is. A value of 1 means perfect elasticity
        /// (kinetic energy is fully preserved). A value of 0 means all energy is absorbed
        /// on collision, i.e. no rebound occurs at all.
        /// </summary>
        public float Restitution { get; set; } = 0.7f;

        /// <summary>
        /// How much friction happens between objects.
        /// </summary>
        public float FrictionCoefficient { get; set; } = 0.2f;

        public Vector2 Centre { get; set; }

        public float RotationRadians { get; set; } = 1;

        public virtual float Mass { get; set; } = 1;

        public Vector2 Velocity
        {
            get => Momentum / Mass;
            set => Momentum = value * Mass;
        }

        public Vector2 Momentum { get; set; }

        public float AngularVelocity
        {
            get => AngularMomentum / MomentOfInertia;
            set => AngularMomentum = value * MomentOfInertia;
        }

        public float AngularMomentum { get; set; }

        public float MomentOfInertia { get; private set; }

        /// <summary>
        /// Total velocity at a given location. Includes angular velocity.
        /// </summary>
        public Vector2 VelocityAt(Vector2 pos)
        {
            Vector2 diff = pos - Centre;

            // Add orthogonal direction to rotation, scaled by distance from centre
            // to the velocity of our centre of mass.
            return Velocity + diff.PerpendicularLeft * AngularVelocity;
        }

        /// <summary>
        /// Contains discrete positions on the surface of this shape used for collision detection.
        /// In the future this can be potentially replaced by closed-form solutions.
        /// </summary>
        protected List<Vector2> Vertices = new List<Vector2>();

        /// <summary>
        /// Normals corresponding to the positions inside <see cref="Vertices"/>.
        /// </summary>
        protected List<Vector2> Normals = new List<Vector2>();

        protected Matrix3 ScreenToSimulationSpace => Simulation.DrawInfo.MatrixInverse;

        protected Matrix3 SimulationToScreenSpace => Simulation.DrawInfo.Matrix;

        /// <summary>
        /// Computes the moment of inertia.
        /// </summary>
        protected float ComputeI()
        {
            Matrix3 mat = DrawInfo.Matrix * Parent.DrawInfo.MatrixInverse;
            Vector2 size = DrawSize;

            // Inertial moment for a linearly transformed rectangle with a given size around its center.
            return ((mat.M11 * mat.M11 + mat.M12 * mat.M12) * size.X * size.X +
                    (mat.M21 * mat.M21 + mat.M22 * mat.M22) * size.Y * size.Y) * Mass / 12;
        }

        /// <summary>
        /// Populates <see cref="Vertices"/> and <see cref="Normals"/>.
        /// </summary>
        protected virtual void UpdateVertices()
        {
            Vertices.Clear();
            Normals.Clear();

            float cornerRadius = CornerRadius;

            // Sides
            RectangleF rect = DrawRectangle;
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
                Vector2[] offsets =
                {
                    new Vector2(cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, cornerRadius),
                    new Vector2(-cornerRadius, -cornerRadius),
                    new Vector2(cornerRadius, -cornerRadius),
                };

                for (int i = 0; i < 4; ++i)
                {
                    Vector2 a = corners[i];

                    float startTheta = (i - 1) * MathF.PI / 2;

                    for (int j = 0; j < amount_corner_steps; ++j)
                    {
                        float theta = startTheta + j * MathF.PI / (2 * (amount_corner_steps - 1));

                        Vector2 normal = new Vector2(MathF.Sin(theta), MathF.Cos(theta));
                        Vertices.Add(a + offsets[i] + normal * cornerRadius);
                        Normals.Add(normal);
                    }
                }
            }

            // To simulation space
            Matrix3 mat = DrawInfo.Matrix * ScreenToSimulationSpace;
            Matrix3 normMat = mat.Inverted();
            normMat.Transpose();

            // Remove translation
            normMat.M31 = normMat.M32 = normMat.M13 = normMat.M23 = 0;
            Vector2 translation = Vector2Extensions.Transform(Vector2.Zero, normMat);

            for (int i = 0; i < Vertices.Count; ++i)
            {
                Vertices[i] = Vector2Extensions.Transform(Vertices[i], mat);
                Normals[i] = (Vector2Extensions.Transform(Normals[i], normMat) - translation).Normalized();
            }
        }

        /// <summary>
        /// Applies a given impulse attacking at a given position.
        /// </summary>
        public virtual void ApplyImpulse(Vector2 impulse, Vector2 pos)
        {
            // Offset to our centre of mass. Required to obtain torque
            Vector2 diff = pos - Centre;

            Momentum += impulse;

            // Cross product between impulse and offset to centre.
            // If they are orthogonal, then the effect on angular momentum is maximized.
            // Intuitively, think of hitting something head-on vs hitting it on the far edge.
            // The first case will not introduce any rotational movement, whereas the latter
            // will.
            AngularMomentum += diff.X * impulse.Y - diff.Y * impulse.X;
        }

        /// <summary>
        /// Checks for and records all collisions with another body. If collisions were found,
        /// their aggregate is handled.
        /// </summary>
        public bool CheckAndHandleCollisionWith(IRigidBody other)
        {
            if (!other.ScreenSpaceDrawQuad.AABB.IntersectsWith(ScreenSpaceDrawQuad.AABB))
                return false;

            bool didCollide = false;

            for (int i = 0; i < Vertices.Count; ++i)
            {
                if (other.BodyContains(Vector2Extensions.Transform(Vertices[i], SimulationToScreenSpace)))
                {
                    // Compute both impulse responses _before_ applying them, such that
                    // they do not influence each other.
                    Vector2 impulse = this.ComputeImpulse(other, Vertices[i], Normals[i]);
                    Vector2 impulseOther = other.ComputeImpulse(this, Vertices[i], -Normals[i]);

                    ApplyImpulse(impulse, Vertices[i]);
                    other.ApplyImpulse(impulseOther, Vertices[i]);

                    didCollide = true;
                }
            }

            return didCollide;
        }

        /// <summary>
        /// Performs an integration step over time. More precisely, updates the
        /// physical state as dependent on time according to the forces and torques
        /// acting on this body.
        /// </summary>
        public void Integrate(Vector2 force, float torque, float dt)
        {
            Vector2 vPrev = Velocity;
            float wPrev = AngularVelocity;

            // Update momenta
            Momentum += dt * force;
            AngularMomentum += dt * torque;

            // Update position and rotation given _previous_ velocities. This is a symplectic integration technique, which conserves energy.
            Centre += dt * vPrev;
            RotationRadians += dt * wPrev;
        }

        /// <summary>
        /// Reads the positional and rotational state of this rigid body from its source.
        /// </summary>
        public void ReadState()
        {
            Matrix3 mat = Parent.DrawInfo.Matrix * ScreenToSimulationSpace;
            Centre = Vector2Extensions.Transform(BoundingBox.Centre, mat);
            RotationRadians = MathUtils.DegreesToRadians(Rotation); // TODO: Fix rotations

            MomentOfInertia = ComputeI();
            UpdateVertices();
        }

        /// <summary>
        /// Applies the positional and rotational state of this rigid body to its source.
        /// </summary>
        public virtual void ApplyState()
        {
            Matrix3 mat = SimulationToScreenSpace * Parent.DrawInfo.MatrixInverse;
            Position = Vector2Extensions.Transform(Centre, mat) + (Position - BoundingBox.Centre);
            Rotation = MathUtils.RadiansToDegrees(RotationRadians); // TODO: Fix rotations
        }

        /// <summary>
        /// Whether the given screen-space position is contained within the rigid body.
        /// </summary>
        public virtual bool BodyContains(Vector2 screenSpacePos) => Contains(screenSpacePos);
    }
}
