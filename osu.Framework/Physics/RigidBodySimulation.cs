// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;
using System.Collections.Generic;
using osu.Framework.Physics.RigidBodies;

namespace osu.Framework.Physics
{
    /// <summary>
    /// Applies rigid body simulation to the <see cref="Drawable"/>s within a given <see cref="Container"/>.
    /// Currently, the simulation only supports <see cref="Drawable"/>s with centre origin and absolute
    /// positioning.
    /// </summary>
    public class RigidBodyContainer : Container
    {
        private readonly Dictionary<Drawable, RigidBody> states = new Dictionary<Drawable, RigidBody>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            foreach (Drawable d in InternalChildren)
            {
                RigidBody body = getRigidBody(d);
                body.ApplyImpulse(new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100, body.Centre + new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100);
            }
        }

        /// <summary>
        /// Obtains the <see cref="RigidBody"/> state associated with a given <see cref="Drawable"/>.
        /// </summary>
        private RigidBody getRigidBody(Drawable d)
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
                RigidBody body = getRigidBody(d);
                body.ReadState();
            }

            // Handle collisions between each pair of bodies.
            foreach (Drawable d in toSimulate)
            {
                d.Colour = Color4.White;
                RigidBody body = getRigidBody(d);

                foreach (Drawable other in toSimulate)
                {
                    if (other == d)
                        continue;

                    if (d != this && body.CheckAndHandleCollisionWith(getRigidBody(other)))
                        d.Colour = Color4.Red;
                }
            }

            // Advance the simulation by the given time step for each body and
            // apply the state to each drawable in question.
            foreach (Drawable d in toSimulate)
            {
                RigidBody body = getRigidBody(d);
                body.Integrate(new Vector2(0, 9.81f * body.Mass), 0, dt);
                body.ApplyState();
            }
        }

        protected override void UpdateAfterChildren()
        {
            //integrate((float)Time.Elapsed / 100);
            base.UpdateAfterChildren();
        }

        internal Matrix3 ScreenToSimulationSpace => DrawInfo.MatrixInverse;

        internal Matrix3 SimulationToScreenSpace => DrawInfo.Matrix;
    }
}
