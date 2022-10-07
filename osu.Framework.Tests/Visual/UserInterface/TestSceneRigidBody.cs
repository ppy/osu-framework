// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osu.Framework.Physics;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneRigidBody : FrameworkTestScene
    {
        private readonly TestRigidBodySimulation sim;

        private float restitutionBacking;

        private float restitution
        {
            get => restitutionBacking;
            set
            {
                restitutionBacking = value;

                if (sim == null)
                    return;

                foreach (var d in sim.Children)
                    d.Restitution = value;
                sim.Restitution = value;
            }
        }

        private float frictionBacking;

        private float friction
        {
            get => frictionBacking;
            set
            {
                frictionBacking = value;

                if (sim == null)
                    return;

                foreach (var d in sim.Children)
                    d.FrictionCoefficient = value;
                sim.FrictionCoefficient = value;
            }
        }

        public TestSceneRigidBody()
        {
            Child = sim = new TestRigidBodySimulation { RelativeSizeAxes = Axes.Both };

            AddStep("Reset bodies", reset);

            AddSliderStep("Simulation speed", 0f, 1f, 0.5f, v => sim.SimulationSpeed = v);
            AddSliderStep("Restitution", -1f, 1f, 1f, v => restitution = v);
            AddSliderStep("Friction", -1f, 5f, 0f, v => friction = v);

            reset();
        }

        private bool overlapsAny(Drawable d)
        {
            foreach (var other in sim.Children)
            {
                if (other.ScreenSpaceDrawQuad.AABB.IntersectsWith(d.ScreenSpaceDrawQuad.AABB))
                    return true;
            }

            return false;
        }

        private void generateN(int n, Func<RigidBodyContainer<Drawable>> generate)
        {
            for (int i = 0; i < n; i++)
            {
                RigidBodyContainer<Drawable> d;

                do
                {
                    d = generate();
                } while (overlapsAny(d));

                sim.Add(d);
            }
        }

        private void reset()
        {
            sim.Clear();

            Random random = new Random(1337);

            // Add a textbox... because we can.
            generateN(3, () => new RigidBodyContainer<Drawable>
            {
                Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                Size = new Vector2(1, 0.1f + 0.2f * (float)random.NextDouble()) * (150 + 150 * (float)random.NextDouble()),
                Rotation = (float)random.NextDouble() * 360,
                Child = new BasicTextBox
                {
                    RelativeSizeAxes = Axes.Both,
                    PlaceholderText = "Text box fun!",
                },
            });

            // Boxes
            generateN(10, () => new TestRigidBody
            {
                Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                Size = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 200,
                Rotation = (float)random.NextDouble() * 360,
                Colour = new Color4(253, 253, 253, 255),
            });

            // Circles
            generateN(5, () =>
            {
                Vector2 size = new Vector2((float)random.NextDouble()) * 200;
                return new TestRigidBody
                {
                    Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                    Size = size,
                    Rotation = (float)random.NextDouble() * 360,
                    CornerRadius = size.X / 2,
                    Colour = new Color4(253, 253, 253, 255),
                    Masking = true,
                };
            });

            // Totally random stuff
            generateN(10, () =>
            {
                Vector2 size = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 200;
                return new TestRigidBody
                {
                    Position = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 1000,
                    Size = size,
                    Rotation = (float)random.NextDouble() * 360,
                    Shear = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 2 - new Vector2(1),
                    CornerRadius = (float)random.NextDouble() * Math.Min(size.X, size.Y) / 2,
                    Colour = new Color4(253, 253, 253, 255),
                    Masking = true,
                };
            });

            // Set appropriate properties
            foreach (var d in sim.Children)
            {
                d.Mass = Math.Max(0.01f, d.ScreenSpaceDrawQuad.Area);
                d.FrictionCoefficient = friction;
                d.Restitution = restitution;
            }
        }

        private class TestRigidBody : RigidBodyContainer<Drawable>
        {
            public TestRigidBody()
            {
                Child = new Box { RelativeSizeAxes = Axes.Both };
            }
        }

        private class TestRigidBodySimulation : RigidBodySimulation
        {
            protected override void LoadComplete()
            {
                base.LoadComplete();

                foreach (var d in Children)
                    d.ApplyImpulse(new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100, d.Centre + new Vector2(RNG.NextSingle() - 0.5f, RNG.NextSingle() - 0.5f) * 100);
            }
        }
    }
}
