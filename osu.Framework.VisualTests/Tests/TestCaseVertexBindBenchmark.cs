// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseVertexBindBenchmark : TestCase
    {
        public override string Description => "Testing lots of VertexBuffer binds";

        private ScrollContainer scroll;
        private FlowContainer<SpriteText> flow;

        private BindableInt iterationsBindable;
        private SpriteText iterationsText;

        public override void Reset()
        {
            base.Reset();

            iterationsBindable = new BindableInt(2000)
            {
                MinValue = 50,
                MaxValue = 20000,
            };

            SliderBar<int> iterations;
            Add(iterations = new BasicSliderBar<int>
            {
                Size = new Vector2(200, 20),
                SelectionColor = Color4.Pink,
                KeyboardStep = 100
            });

            Add(iterationsText = new SpriteText
            {
                X = 210,
                TextSize = 16
            });

            iterations.Current.BindTo(iterationsBindable);
            iterations.Current.ValueChanged += v => Invalidate(Invalidation.DrawNode, shallPropagate: false);

            Add(scroll = new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Y = 25,
                Children = new[]
                {
                    flow = new FillFlowContainer<SpriteText>
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical
                    }
                }
            });
        }

        private readonly TestDrawNodeSharedData sharedData = new TestDrawNodeSharedData();
        protected override DrawNode CreateDrawNode() => new TestDrawNode();
        protected override void ApplyDrawNode(DrawNode node)
        {
            base.ApplyDrawNode(node);

            var testNode = (TestDrawNode)node;
            testNode.TestSharedData = sharedData;
            testNode.Iterations = iterationsBindable.Value;
        }

        private int lastDrawIteration;

        protected override void Update()
        {
            base.Update();

            iterationsText.Text = iterationsBindable.Value.ToString();

            if (sharedData.DrawIteration == lastDrawIteration)
                return;

            flow.Add(new SpriteText
            {
                Text = $"Iteration: {sharedData.DrawIteration.ToString().PadRight(8)} | TotalTime: {Math.Round(sharedData.ElapsedTime, 3)}",
                TextSize = 22
            });

            scroll.ScrollToEnd();

            lastDrawIteration = sharedData.DrawIteration;
        }

        private class TestDrawNodeSharedData
        {
            public VertexBuffer<Vertex2D> Batch1;
            public VertexBuffer<TexturedVertex2D> Batch2;

            public double ElapsedTime;
            public int DrawIteration;
        }

        private class TestDrawNode : ContainerDrawNode
        {
            public TestDrawNodeSharedData TestSharedData;

            public int Iterations;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                if (TestSharedData.Batch1 == null)
                    TestSharedData.Batch1 = new QuadVertexBuffer<Vertex2D>(4, BufferUsageHint.DynamicDraw);
                if (TestSharedData.Batch2 == null)
                    TestSharedData.Batch2 = new LinearVertexBuffer<TexturedVertex2D>(3, PrimitiveType.Triangles, BufferUsageHint.DynamicDraw);

                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < Iterations; i++)
                {
                    TestSharedData.Batch1.Bind(false);
                    TestSharedData.Batch2.Bind(false);
                }

                stopwatch.Stop();

                TestSharedData.ElapsedTime = stopwatch.Elapsed.TotalMilliseconds;
                TestSharedData.DrawIteration++;

                base.Draw(vertexAction);
            }
        }
    }
}
