// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics.Primitives;
using System;
using System.Collections.Generic;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Contains physical state and methods necessary for rigid body simulation.
    /// </summary>
    public abstract class RigidBody
    {
        private readonly RigidBodySimulation simulation;

        /// <summary>
        /// Controls how elastic the material is. A value of 1 means perfect elasticity
        /// (kinetic energy is fully preserved). A value of 0 means all energy is absorbed
        /// on collision, i.e. no rebound occurs at all.
        /// </summary>
        public float Restitution = 1.0f;

        /// <summary>
        /// How much friction happens between objects.
        /// </summary>
        public float FrictionCoefficient = 0f;

        public Vector2 Centre;

        public float Rotation;

        public float Mass;

        public Vector2 Velocity
        {
            get { return Momentum / Mass; }
            set { Momentum = value * Mass; }
        }

        public Vector2 Momentum;

        public float AngularVelocity
        {
            get { return AngularMomentum / MomentOfInertia; }
            set { AngularMomentum = value * MomentOfInertia; }
        }

        public float AngularMomentum;

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

        protected RigidBody(RigidBodySimulation sim)
        {
            simulation = sim;
            Mass = 1f; // Arbitrarily 1 kg for now

            // Initially no moments
            Momentum = Vector2.Zero;
            AngularMomentum = 0;
        }

        protected Matrix3 ScreenToSimulationSpace => simulation.ScreenToSimulationSpace;

        protected Matrix3 SimulationToScreenSpace => simulation.SimulationToScreenSpace;

        /// <summary>
        /// Computes the moment of inertia.
        /// </summary>
        protected virtual float ComputeI()
        {
            return 1;
        }

        /// <summary>
        /// Populates <see cref="Vertices"/> and <see cref="Normals"/>.
        /// </summary>
        protected virtual void UpdateVertices()
        {
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
        /// Helper function for code brevity in <see cref="computeImpulse(RigidBody, Vector2, Vector2)"/>.
        /// Can be moved into the function as a nested method once C# 7 is out.
        /// </summary>
        private float impulseDenominator(Vector2 pos, Vector2 normal)
        {
            Vector2 diff = pos - Centre;
            float perpDot = Vector2.Dot(normal, diff.PerpendicularRight);
            return 1.0f / Mass + perpDot * perpDot / MomentOfInertia;
        }

        /// <summary>
        /// Computes the impulse of a collision of 2 rigid bodies, given the other body, the impact position,
        /// and the surface normal of this body at the impact position.
        /// </summary>
        private Vector2 computeImpulse(RigidBody other, Vector2 pos, Vector2 normal)
        {
            Vector2 vrel = VelocityAt(pos) - other.VelocityAt(pos);
            float vrelOrtho = -Vector2.Dot(vrel, normal);

            // We don't want to consider collisions where objects move away from each other.
            // (Or with negligible velocity. Let repulsive forces handle these.)
            if (vrelOrtho > -0.001f)
                return Vector2.Zero;

            float impulseMagnitude = -(1.0f + Restitution) * vrelOrtho;
            impulseMagnitude /= impulseDenominator(pos, normal) + other.impulseDenominator(pos, normal);

            //impulseMagnitude = Math.Max(impulseMagnitude - 0.01f, 0.0f);

            Vector2 impulse = -normal * impulseMagnitude;

            // Add "friction" to the impulse. We arbitrarily reduce the planar velocity relative to the impulse magnitude.
            Vector2 vrelPlanar = vrel + vrelOrtho * normal;
            float vrelPlanarLength = vrelPlanar.Length;
            if (vrelPlanarLength > 0)
                impulse -= vrelPlanar * Math.Min(impulseMagnitude * 0.05f * FrictionCoefficient * other.FrictionCoefficient / vrelPlanarLength, Mass);

            return impulse;
        }

        /// <summary>
        /// Checks for and records all collisions with another body. If collisions were found,
        /// their aggregate is handled.
        /// </summary>
        public bool CheckAndHandleCollisionWith(RigidBody other)
        {
            if (!other.ScreenSpaceAABB.IntersectsWith(ScreenSpaceAABB))
                return false;

            bool didCollide = false;
            for (int i = 0; i < Vertices.Count; ++i)
            {
                if (other.Contains(Vertices[i] * SimulationToScreenSpace))
                {
                    // Compute both impulse responses _before_ applying them, such that
                    // they do not influence each other.
                    Vector2 impulse = computeImpulse(other, Vertices[i], Normals[i]);
                    Vector2 impulseOther = other.computeImpulse(this, Vertices[i], -Normals[i]);

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
            Rotation += dt * wPrev;
        }

        /// <summary>
        /// Reads the positional and rotational state of this rigid body from its source.
        /// </summary>
        public virtual void ReadState()
        {
            MomentOfInertia = ComputeI();
            UpdateVertices();
        }

        /// <summary>
        /// Applies the positional and rotational state of this rigid body to its source.
        /// </summary>
        public virtual void ApplyState()
        {
        }

        /// <summary>
        /// Axis-aligned bounding box of this body.
        /// </summary>
        public abstract RectangleI ScreenSpaceAABB { get; }

        /// <summary>
        /// Whether a parent-space position is contained within this body.
        /// </summary>
        public abstract bool Contains(Vector2 screenSpacePos);
    }
}
