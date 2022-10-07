// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osu.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Applies rigid body simulation to all children.
    /// </summary>
    public class RigidBodySimulation : RigidBodySimulation<Drawable>
    {
    }

    /// <summary>
    /// Applies rigid body simulation to all children.
    /// </summary>
    public class RigidBodySimulation<T> : RigidBodyContainer<RigidBodyContainer<T>>
        where T : Drawable
    {
        public RigidBodySimulation()
        {
            // For this special case of a rigid body container we don't ware about the origin
            // since no rotation can ever happen. Therefore, let's revert to the usual default.
            Origin = Anchor.TopLeft;
        }

        /// <summary>
        /// The relative speed at which the simulation runs. A value of 1 means it runs as fast
        /// as the rest of the game.
        /// </summary>
        public float SimulationSpeed = 1;

        private readonly List<IRigidBody> toSimulate = new List<IRigidBody>();

        /// <summary>
        /// Advances the simulation by a time step.
        /// </summary>
        /// <param name="dt">The time step to advance the simulation by.</param>
        private void integrate(float dt)
        {
            toSimulate.Clear();

            foreach (var d in Children)
                toSimulate.Add(d);
            toSimulate.Add(this);

            // Read the new state from each drawable in question
            foreach (var d in toSimulate)
            {
                d.Simulation = this;
                d.ReadState();
            }

            // Handle collisions between each pair of bodies.
            foreach (var d in toSimulate)
            {
                foreach (var other in toSimulate)
                {
                    if (other != d)
                        d.CheckAndHandleCollisionWith(other);
                }
            }

            // Advance the simulation by the given time step for each body and
            // apply the state to each drawable in question.
            foreach (var d in toSimulate)
            {
                d.Integrate(new Vector2(0, 981f * d.Mass), 0, dt);
                d.ApplyState();
            }
        }

        protected override void UpdateAfterChildren()
        {
            integrate(SimulationSpeed * (float)Time.Elapsed / 1000);
            base.UpdateAfterChildren();
        }

        public override float Mass
        {
            get => float.MaxValue;
            set => throw new InvalidOperationException($"May not set the {nameof(Mass)} of a {nameof(RigidBodySimulation<T>)}.");
        }

        protected override void UpdateVertices()
        {
            base.UpdateVertices();

            // We want to behave like a hollow box, so all normals need to point inward.
            for (int i = 0; i < Normals.Count; ++i)
                Normals[i] = -Normals[i];
        }

        // For hollow-box behavior we want to be contained whenever we are _not_ inside
        public override bool BodyContains(Vector2 screenSpacePos) => !base.BodyContains(screenSpacePos);

        public override void ApplyImpulse(Vector2 impulse, Vector2 pos)
        {
            // Do nothing. We want to be immovable.
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Momentum = Vector2.Zero;
            AngularMomentum = 0;
        }
    }
}
