// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;
using System.Collections.Generic;
using osu.Framework.Physics.RigidBodies;
using System;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Applies rigid body simulation to the <see cref="Drawable"/>s within a given <see cref="Container"/>.
    /// Currently, the simulation only supports <see cref="Drawable"/>s with centre origin and absolute
    /// positioning.
    /// </summary>
    public class RigidBodyContainer : Container
    {
        /// <summary>
        /// The relative speed at which the simulation runs. A value of 1 means it runs as fast
        /// as the rest of the game.
        /// </summary>
        public float SimulationSpeed = 1;

        /// <summary>
        /// Sets the <see cref="RigidBody.Restitution"/> of all rigid bodies.
        /// </summary>
        /// <param name="value">The value to set the <see cref="RigidBody.Restitution"/> to.</param>
        public void SetRestitution(float value)
        {
            foreach (var c in InternalChildren)
                SetRestitution(c, value);
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.FrictionCoefficient"/> of all rigid bodies.
        /// </summary>
        /// <param name="value">The value to set the <see cref="RigidBody.FrictionCoefficient"/> to.</param>
        public void SetFrictionCoefficient(float value)
        {
            foreach (var c in InternalChildren)
                SetFrictionCoefficient(c, value);
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.Restitution"/> of a <see cref="RigidBody"/> corresponding to
        /// a <paramref name="child"/> of this <see cref="RigidBodyContainer"/>.
        /// </summary>
        /// <param name="child">The child of which to set the <see cref="RigidBody.Restitution"/>.</param>
        /// <param name="value">The value to set the <see cref="RigidBody.Restitution"/> to.</param>
        public void SetRestitution(Drawable child, float value)
        {
            if (!Contains(child))
                throw new InvalidOperationException(
                    $"Can not set the {nameof(RigidBody.Restitution)} of a {nameof(Drawable)} which is not contained within this {nameof(RigidBodyContainer)}.");

            GetRigidBody(child).Restitution = value;
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.FrictionCoefficient"/> of a <see cref="RigidBody"/> corresponding to
        /// a <paramref name="child"/> of this <see cref="RigidBodyContainer"/>.
        /// </summary>
        /// <param name="child">The child of which to set the <see cref="RigidBody.FrictionCoefficient"/>.</param>
        /// <param name="value">The value to set the <see cref="RigidBody.FrictionCoefficient"/> to.</param>
        public void SetFrictionCoefficient(Drawable child, float value)
        {
            if (!Contains(child))
                throw new InvalidOperationException(
                    $"Can not set the {nameof(RigidBody.FrictionCoefficient)} of a {nameof(Drawable)} which is not contained within this {nameof(RigidBodyContainer)}.");

            GetRigidBody(child).FrictionCoefficient = value;
        }

        private readonly Dictionary<Drawable, RigidBody> states = new Dictionary<Drawable, RigidBody>();

        /// <summary>
        /// Obtains the <see cref="RigidBody"/> state associated with a given <see cref="Drawable"/>.
        /// </summary>
        protected RigidBody GetRigidBody(Drawable d)
        {
            RigidBody body;
            if (!states.TryGetValue(d, out body))
                states[d] = body = d == this ? new ContainerBody(d, this) : new DrawableBody(d, this);

            return body;
        }

        private readonly List<Drawable> toSimulate = new List<Drawable>();

        /// <summary>
        /// Advances the simulation by a time step.
        /// </summary>
        /// <param name="dt">The time step to advance the simulation by.</param>
        private void integrate(float dt)
        {
            toSimulate.Clear();

            foreach (Drawable d in InternalChildren)
                toSimulate.Add(d);
            toSimulate.Add(this);

            // Read the new state from each drawable in question
            foreach (Drawable d in toSimulate)
            {
                RigidBody body = GetRigidBody(d);
                body.ReadState();
            }

            // Handle collisions between each pair of bodies.
            foreach (Drawable d in toSimulate)
            {
                RigidBody body = GetRigidBody(d);
                foreach (Drawable other in toSimulate)
                    if (other != d)
                        body.CheckAndHandleCollisionWith(GetRigidBody(other));
            }

            // Advance the simulation by the given time step for each body and
            // apply the state to each drawable in question.
            foreach (Drawable d in toSimulate)
            {
                RigidBody body = GetRigidBody(d);
                body.Integrate(new Vector2(0, 981f * body.Mass), 0, dt);
                body.ApplyState();
            }
        }

        protected override void UpdateAfterChildren()
        {
            integrate(SimulationSpeed * (float)Time.Elapsed / 1000);
            base.UpdateAfterChildren();
        }
    }
}
