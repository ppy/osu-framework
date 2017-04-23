// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.Physics.RigidBodies
{
    /// <summary>
    /// Links a <see cref="RigidBody"/> with a <see cref="Container"/> such that their state
    /// is interconnected.
    /// </summary>
    class ContainerBody : DrawableBody
    {
        public ContainerBody(Drawable d, RigidBodySimulation sim) : base(d, sim)
        {
            m = float.MaxValue;
        }

        protected override void UpdateVertices()
        {
            base.UpdateVertices();

            // We want to behave like a hollow box, so all normals need to point inward.
            for (int i = 0; i < Normals.Count; ++i)
                Normals[i] = -Normals[i];
        }

        // For hollow-box behavior we want to be contained whenever we are _not_ inside
        public override bool Contains(Vector2 screenSpacePos) => !base.Contains(screenSpacePos);

        public override void ApplyImpulse(Vector2 impulse, Vector2 pos)
        {
            // Do nothing. We want to be immovable.
        }

        public override void ApplyState()
        {
            base.ApplyState();

            p = Vector2.Zero;
            L = 0;
        }
    }
}
