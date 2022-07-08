// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osuTK;
using osu.Framework.Graphics;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Contains physical state and methods necessary for rigid body simulation.
    /// </summary>
    public interface IRigidBody : IDrawable
    {
        /// <summary>
        /// The <see cref="Drawable"/> which is currently performing a simulation on this <see cref="IRigidBody"/>.
        /// </summary>
        Drawable Simulation { get; set; }

        /// <summary>
        /// Controls how elastic the material is. A value of 1 means perfect elasticity
        /// (kinetic energy is fully preserved). A value of 0 means all energy is absorbed
        /// on collision, i.e. no rebound occurs at all.
        /// </summary>
        float Restitution { get; set; }

        /// <summary>
        /// How much friction happens between objects.
        /// </summary>
        float FrictionCoefficient { get; set; }

        Vector2 Centre { get; set; }

        float RotationRadians { get; set; }

        float Mass { get; set; }

        Vector2 Velocity { get; }

        Vector2 Momentum { get; set; }

        float AngularVelocity { get; }

        float AngularMomentum { get; set; }

        float MomentOfInertia { get; }

        /// <summary>
        /// Total velocity at a given location. Includes angular velocity.
        /// </summary>
        Vector2 VelocityAt(Vector2 pos);

        /// <summary>
        /// Applies a given impulse attacking at a given position.
        /// </summary>
        void ApplyImpulse(Vector2 impulse, Vector2 pos);

        /// <summary>
        /// Checks for and records all collisions with another body. If collisions were found,
        /// their aggregate is handled.
        /// </summary>
        bool CheckAndHandleCollisionWith(IRigidBody other);

        /// <summary>
        /// Performs an integration step over time. More precisely, updates the
        /// physical state as dependent on time according to the forces and torques
        /// acting on this body.
        /// </summary>
        void Integrate(Vector2 force, float torque, float dt);

        /// <summary>
        /// Reads the positional and rotational state of this rigid body from its source.
        /// </summary>
        void ReadState();

        /// <summary>
        /// Applies the positional and rotational state of this rigid body to its source.
        /// </summary>
        void ApplyState();

        /// <summary>
        /// Whether the given screen-space position is contained within the rigid body.
        /// </summary>
        bool BodyContains(Vector2 screenSpacePos);
    }

    /// <summary>
    /// Helper extension methods operating on <see cref="IRigidBody"/>.
    /// </summary>
    public static class RigidBodyExtensions
    {
        /// <summary>
        /// Helper function for code brevity in <see cref="ComputeImpulse(IRigidBody, IRigidBody, Vector2, Vector2)"/>.
        /// Can be moved into the function as a nested method once C# 7 is out.
        /// </summary>
        public static float ImpulseDenominator(this IRigidBody body, Vector2 pos, Vector2 normal)
        {
            Vector2 diff = pos - body.Centre;
            float perpDot = Vector2.Dot(normal, diff.PerpendicularRight);
            return 1.0f / body.Mass + perpDot * perpDot / body.MomentOfInertia;
        }

        /// <summary>
        /// Computes the impulse of a collision of 2 rigid bodies, given the other body, the impact position,
        /// and the surface normal of this body at the impact position.
        /// </summary>
        public static Vector2 ComputeImpulse(this IRigidBody body, IRigidBody other, Vector2 pos, Vector2 normal)
        {
            Vector2 vrel = body.VelocityAt(pos) - other.VelocityAt(pos);
            float vrelOrtho = -Vector2.Dot(vrel, normal);

            // We don't want to consider collisions where objects move away from each other.
            // (Or with negligible velocity. Let repulsive forces handle these.)
            if (vrelOrtho > -0.001f)
                return Vector2.Zero;

            float impulseMagnitude = -(1.0f + body.Restitution) * vrelOrtho;
            impulseMagnitude /= body.ImpulseDenominator(pos, normal) + other.ImpulseDenominator(pos, normal);

            //impulseMagnitude = Math.Max(impulseMagnitude - 0.01f, 0.0f);

            Vector2 impulse = -normal * impulseMagnitude;

            // Add "friction" to the impulse. We arbitrarily reduce the planar velocity relative to the impulse magnitude.
            Vector2 vrelPlanar = vrel + vrelOrtho * normal;
            float vrelPlanarLength = vrelPlanar.Length;
            if (vrelPlanarLength > 0)
                impulse -= vrelPlanar * Math.Min(impulseMagnitude * 0.05f * body.FrictionCoefficient * other.FrictionCoefficient / vrelPlanarLength, body.Mass);

            return impulse;
        }
    }
}
