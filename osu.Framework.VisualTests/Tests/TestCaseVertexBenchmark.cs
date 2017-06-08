using System;
using System.Diagnostics;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseVertexBenchmark : TestCase
    {
        public override string Description => "Stress testing lots of vertex types";

        private FlowContainer<SpriteText> flow;

        public override void Reset()
        {
            base.Reset();

            Add(new ScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
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
        }

        private int lastDrawIteration;

        protected override void Update()
        {
            base.Update();

            if (sharedData.DrawIteration == lastDrawIteration)
                return;

            flow.Add(new SpriteText
            {
                Text = $"Iteration: {sharedData.DrawIteration.ToString().PadRight(8)} | TotalTime: {Math.Round(sharedData.ElapsedTime, 3)}",
                TextSize = 22
            });

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
            public const int ITERATIONS = 500;

            public TestDrawNodeSharedData TestSharedData;

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                if (TestSharedData.Batch1 == null)
                    TestSharedData.Batch1 = new QuadVertexBuffer<Vertex2D>(4, BufferUsageHint.DynamicDraw);
                if (TestSharedData.Batch2 == null)
                    TestSharedData.Batch2 = new LinearVertexBuffer<TexturedVertex2D>(3, PrimitiveType.Triangles, BufferUsageHint.DynamicDraw);

                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < ITERATIONS; i++)
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