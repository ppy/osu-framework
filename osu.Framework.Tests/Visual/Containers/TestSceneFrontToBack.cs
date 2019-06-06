// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneFrontToBack : GridTestScene
    {
        private SpriteText labelDrawables;
        private QueryingCompositeDrawableDrawNode drawNode;
        private SpriteText labelFrag;
        private SpriteText labelFrag2;
        private float currentScale = 1;

        private const int cell_count = 4;

        protected override DrawNode CreateDrawNode() => drawNode = new QueryingCompositeDrawableDrawNode(this);

        public TestSceneFrontToBack()
            : base(cell_count / 2, cell_count / 2)
        {
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkDebugConfigManager debugConfig)
        {
            AddStep("add more drawables", addMoreDrawables);
            AddToggleStep("disable front to back", val =>
            {
                debugConfig.Set(DebugSetting.BypassFrontToBackPass, val);
                Invalidate(Invalidation.DrawNode); // reset counts
            });

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
                            labelDrawables = new SpriteText { Font = new FontUsage("RobotoCondensed", weight: "Regular") },
                            labelFrag = new SpriteText { Font = new FontUsage("RobotoCondensed", weight: "Regular") },
                            labelFrag2 = new SpriteText { Font = new FontUsage("RobotoCondensed", weight: "Regular") },
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
                labelDrawables.Text = $"boxes: {Cell(1).Children.Count * cell_count:N0}";
                labelFrag.Text = $"samples ({nameof(DrawNode.Draw)}): {drawNode.DrawSamples:N0}";
                labelFrag2.Text = $"samples ({nameof(DrawNode.DrawOpaqueInteriorSubTree)}): {drawNode.DrawOpaqueInteriorSubTreeSamples:N0}";
            }
        }

        private void addMoreDrawables()
        {
            for (int i = 0; i < 100; i++)
            {
                Cell(i % cell_count).Add(new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                    RelativeSizeAxes = Axes.Both,
                    Scale = new Vector2(currentScale)
                });

                currentScale -= 0.001f;
                if (currentScale < 0)
                    currentScale = 1;
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
