// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneFrontToBack : GridTestScene
    {
        private readonly SpriteText labelDrawables;
        private QueryingCompositeDrawableDrawNode drawNode;
        private readonly SpriteText labelFrag;
        private readonly SpriteText labelFrag2;
        public float CurrentScale = 1;

        public const int CELL_COUNT = 4;

        protected override DrawNode CreateDrawNode() => drawNode = new QueryingCompositeDrawableDrawNode(this);

        public TestSceneFrontToBack()
            : base(CELL_COUNT / 2, CELL_COUNT / 2)
        {
            Add(new Container
            {
                AutoSizeAxes = Axes.Both,
                Depth = float.NegativeInfinity,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,

                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Black,
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.8f
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            labelDrawables = new SpriteText { Font = FrameworkFont.Condensed },
                            labelFrag = new SpriteText { Font = FrameworkFont.Condensed },
                            labelFrag2 = new SpriteText { Font = FrameworkFont.Condensed },
                        }
                    },
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            if (drawNode != null)
            {
                labelDrawables.Text = $"boxes: {Cell(1).Children.Count * CELL_COUNT:N0}";
                labelFrag.Text = $"samples ({nameof(DrawNode.Draw)}): {drawNode.DrawSamples:N0}";
                labelFrag2.Text = $"samples ({nameof(DrawNode.DrawOpaqueInteriorSubTree)}): {drawNode.DrawOpaqueInteriorSubTreeSamples:N0}";
            }
        }

        private class QueryingCompositeDrawableDrawNode : CompositeDrawableDrawNode
        {
            private int queryObject = -1;

            public int DrawSamples { get; private set; }
            public int DrawOpaqueInteriorSubTreeSamples { get; private set; }

            public QueryingCompositeDrawableDrawNode(CompositeDrawable source)
                : base(source)
            {
            }

            internal override void DrawOpaqueInteriorSubTree(DepthValue depthValue, Action<TexturedVertex2D> vertexAction)
            {
                startQuery();
                base.DrawOpaqueInteriorSubTree(depthValue, vertexAction);
                DrawOpaqueInteriorSubTreeSamples = endQuery();
            }

            public override void ApplyState()
            {
                DrawSamples = 0;
                DrawOpaqueInteriorSubTreeSamples = 0;
                base.ApplyState();
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                startQuery();
                base.Draw(vertexAction);
                DrawSamples = endQuery();
            }

            private int endQuery()
            {
                GL.EndQuery(QueryTarget.SamplesPassed);
                GL.GetQueryObject(queryObject, GetQueryObjectParam.QueryResult, out int result);

                return result;
            }

            private void startQuery()
            {
                if (queryObject == -1)
                    queryObject = GL.GenQuery();

                GL.BeginQuery(QueryTarget.SamplesPassed, queryObject);
            }
        }
    }
}
