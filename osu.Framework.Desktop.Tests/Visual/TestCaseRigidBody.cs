// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Physics;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseRigidBody : TestCase
    {
        public override string Description => @"Rigid body simulation scenarios.";

        private readonly Container testContainer;
        private RigidBodySimulation sim;

        public TestCaseRigidBody()
        {
            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames =
            {
                @"Random Children",
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddStep(testNames[i], delegate { loadTest(test); });
            }

            loadTest(0);
        }

        private bool overlapsAny(Drawable d)
        {
            foreach (Drawable other in testContainer.Children)
                if (other.ScreenSpaceDrawQuad.AABB.IntersectsWith(d.ScreenSpaceDrawQuad.AABB))
                    return true;

            return false;
        }

        private void generateN(int n, Func<Drawable> generate)
        {
            for (int i = 0; i < n; i++)
            {
                Drawable d = generate();

                if (overlapsAny(d))
                {
                    --i;
                    continue;
                }

                testContainer.Add(d);
            }
        }

        private void loadTest(int testType)
        {
            testContainer.Clear();

            switch (testType)
            {
                case 0:
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

                    break;
            }

            sim = new RigidBodySimulation(testContainer);
        }

        protected override void UpdateAfterChildren()
        {
            sim.Update((float)Time.Elapsed / 100);
            base.UpdateAfterChildren();
        }
    }
}
