// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Physics;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.MathUtils;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseRigidBody : TestCase
    {
        public override string Description => @"Rigid body simulation scenarios.";

        private readonly TestRigidBodyContainer sim;

        public TestCaseRigidBody()
        {
            Child = sim = new TestRigidBodyContainer { RelativeSizeAxes = Axes.Both };

            AddStep("Reset bodies", reset);

            AddSliderStep("Simulation speed", 0.0, 4.0, 1.0, v => sim.SimulationSpeed = (float)v);
            AddSliderStep("Restitution", -1.0, 1.0, 1.0, v => sim.SetRestitution((float)v));
            AddSliderStep("Friction", -1.0, 5.0, 0.0, v => sim.SetFrictionCoefficient((float)v));

            reset();
        }

        private bool overlapsAny(Drawable d)
        {
            foreach (Drawable other in sim.Children)
                if (other.ScreenSpaceDrawQuad.AABB.IntersectsWith(d.ScreenSpaceDrawQuad.AABB))
                    return true;

            return false;
        }

        private void generateN(int n, Func<Drawable> generate)
        {
            for (int i = 0; i < n; i++)
            {
                Drawable d;
                do d = generate();
                while (overlapsAny(d));

                sim.Add(d);
            }
        }

        private void reset()
        {
            sim.Clear();

            Random random = new Random(1337);

            // Boxes
            generateN(10, () => new InfofulBox
            {
                Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                Size = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 200,
                Rotation = (float)random.NextDouble() * 360,
                Colour = new Color4(253, 253, 253, 255),
                Origin = Anchor.Centre,
                Anchor = Anchor.TopLeft,
            });

            // Circles
            generateN(10, delegate
            {
                Vector2 size = new Vector2((float)random.NextDouble()) * 200;
                return new InfofulBox
                {
                    Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                    Size = size,
                    Rotation = (float)random.NextDouble() * 360,
                    CornerRadius = size.X / 2,
                    Colour = new Color4(253, 253, 253, 255),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.TopLeft,
                    Masking = true,
                };
            });

            // Totally random stuff
            generateN(10, delegate
            {
                Vector2 size = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 200;
                return new InfofulBox
                {
                    Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                    Size = size,
                    Rotation = (float)random.NextDouble() * 360,
                    Shear = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 2 - new Vector2(1),
                    CornerRadius = (float)random.NextDouble() * Math.Min(size.X, size.Y) / 2,
                    Colour = new Color4(253, 253, 253, 255),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.TopLeft,
                    Masking = true,
                };
            });

            foreach (var d in sim.Children)
                sim.SetMass(d, (float)d.ScreenSpaceDrawQuad.Area);
        }

        private class TestRigidBodyContainer : RigidBodyContainer
        {
            protected override void LoadComplete()
            {
                base.LoadComplete();

                foreach (Drawable d in InternalChildren)
                {
                    RigidBody body = GetRigidBody(d);
                    body.ApplyImpulse(new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100, body.Centre + new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100);
                }
            }
        }
    }
}
