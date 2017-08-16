// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
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
        /// The default value of <see cref="RigidBody.Restitution"/> for newly added children.
        /// </summary>
        public float DefaultRestitution = 1;

        /// <summary>
        /// The default value of <see cref="RigidBody.FrictionCoefficient"/> for newly added children.
        /// </summary>
        public float DefaultFrictionCoefficient;

        /// <summary>
        /// The default value of <see cref="RigidBody.Mass"/> for newly added children.
        /// </summary>
        public float DefaultMass = 1;

        /// <summary>
        /// Sets the <see cref="RigidBody.Restitution"/> of all current and future rigid bodies.
        /// </summary>
        /// <param name="value">The value to set the <see cref="RigidBody.Restitution"/> to.</param>
        public void SetRestitution(float value)
        {
            DefaultRestitution = value;

            SetRestitution(this, value);
            foreach (var c in InternalChildren)
                SetRestitution(c, value);
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.FrictionCoefficient"/> of all current and future rigid bodies.
        /// </summary>
        /// <param name="value">The value to set the <see cref="RigidBody.FrictionCoefficient"/> to.</param>
        public void SetFrictionCoefficient(float value)
        {
            DefaultFrictionCoefficient = value;

            SetFrictionCoefficient(this, value);
            foreach (var c in InternalChildren)
                SetFrictionCoefficient(c, value);
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.Mass"/> of all current and future rigid bodies.
        /// </summary>
        /// <param name="value">The value to set the <see cref="RigidBody.Mass"/> to.</param>
        public void SetMass(float value)
        {
            DefaultMass = value;

            foreach (var c in InternalChildren)
                SetMass(c, value);
        }

        /// <summary>
        /// Sets the <see cref="RigidBody.Restitution"/> of a <see cref="RigidBody"/> corresponding to
        /// a <paramref name="child"/> of this <see cref="RigidBodyContainer"/>.
        /// </summary>
        /// <param name="child">The child of which to set the <see cref="RigidBody.Restitution"/>.</param>
        /// <param name="value">The value to set the <see cref="RigidBody.Restitution"/> to.</param>
        public void SetRestitution(Drawable child, float value) => GetRigidBody(child).Restitution = value;

        /// <summary>
        /// Sets the <see cref="RigidBody.FrictionCoefficient"/> of a <see cref="RigidBody"/> corresponding to
        /// a <paramref name="child"/> of this <see cref="RigidBodyContainer"/>.
        /// </summary>
        /// <param name="child">The child of which to set the <see cref="RigidBody.FrictionCoefficient"/>.</param>
        /// <param name="value">The value to set the <see cref="RigidBody.FrictionCoefficient"/> to.</param>
        public void SetFrictionCoefficient(Drawable child, float value) => GetRigidBody(child).FrictionCoefficient = value;

        /// <summary>
        /// Sets the <see cref="RigidBody.Mass"/> of a <see cref="RigidBody"/> corresponding to
        /// a <paramref name="child"/> of this <see cref="RigidBodyContainer"/>.
        /// </summary>
        /// <param name="child">The child of which to set the <see cref="RigidBody.Mass"/>.</param>
        /// <param name="value">The value to set the <see cref="RigidBody.Mass"/> to.</param>
        public void SetMass(Drawable child, float value) => GetRigidBody(child).Mass = value;

        private readonly Dictionary<Drawable, RigidBody> states = new Dictionary<Drawable, RigidBody>();

        /// <summary>
        /// Obtains the <see cref="RigidBody"/> state associated with a given <see cref="Drawable"/>.
        /// </summary>
        protected RigidBody GetRigidBody(Drawable d)
        {
            if (d != this && !Contains(d))
                throw new InvalidOperationException(
                    $"Can not obtain a {nameof(RigidBody)} of a {nameof(Drawable)} which is not part of this {nameof(RigidBodyContainer)}.");

            RigidBody body;
            if (!states.TryGetValue(d, out body))
            {
                states[d] = body = d == this ? new ContainerBody(d, this) : new DrawableBody(d, this);

                body.Restitution = DefaultRestitution;
                body.FrictionCoefficient = DefaultFrictionCoefficient;

                if (d != this)
                    body.Mass = DefaultMass;
            }

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
