// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;

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
        private void load(FrameworkDebugConfigManager debugConfig, TextureStore store, IRenderer renderer)
        {
            var texture = store.Get(@"sample-texture");
            var repeatedTexture = store.Get(@"sample-texture", WrapMode.Repeat, WrapMode.Repeat);
            var edgeClampedTexture = store.Get(@"sample-texture", WrapMode.ClampToEdge, WrapMode.ClampToEdge);
            var borderClampedTexture = store.Get(@"sample-texture", WrapMode.ClampToBorder, WrapMode.ClampToBorder);

            AddStep("add sprites", () => addMoreDrawables(texture, new RectangleF(0, 0, 1, 1)));
            AddStep("add sprites with shrink", () => addMoreDrawables(texture, new RectangleF(0.25f, 0.25f, 0.5f, 0.5f)));
            AddStep("add sprites with repeat", () => addMoreDrawables(repeatedTexture, new RectangleF(0.25f, 0.25f, 0.5f, 0.5f)));
            AddStep("add sprites with edge clamp", () => addMoreDrawables(edgeClampedTexture, new RectangleF(0.25f, 0.25f, 0.5f, 0.5f)));
            AddStep("add sprites with border clamp", () => addMoreDrawables(borderClampedTexture, new RectangleF(0.25f, 0.25f, 0.5f, 0.5f)));
            AddStep("add boxes", () => addMoreDrawables(renderer.WhitePixel, new RectangleF(0, 0, 1, 1)));
            AddToggleStep("disable front to back", val =>
            {
                debugConfig.SetValue(DebugSetting.BypassFrontToBackPass, val);
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
                        Alpha = 0.8f,
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
                labelDrawables.Text = $"boxes: {Cell(1).Children.Count * cell_count:N0}";
                labelFrag.Text = $"samples ({nameof(DrawNode.Draw)}): {drawNode.DrawSamples:N0}";
                labelFrag2.Text = $"samples ({nameof(DrawNode.DrawOpaqueInteriorSubTree)}): {drawNode.DrawOpaqueInteriorSubTreeSamples:N0}";
            }
        }

        private void addMoreDrawables(Texture texture, RectangleF textureRect)
        {
            for (int i = 0; i < 100; i++)
            {
                Cell(i % cell_count).Add(new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                    RelativeSizeAxes = Axes.Both,
                    Scale = new Vector2(currentScale),
                    Texture = texture,
                    TextureRectangle = textureRect,
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

            internal override void DrawOpaqueInteriorSubTree(IRenderer renderer, DepthValue depthValue)
            {
                startQuery();
                base.DrawOpaqueInteriorSubTree(renderer, depthValue);
                DrawOpaqueInteriorSubTreeSamples = endQuery();
            }

            public override void ApplyState()
            {
                DrawSamples = 0;
                DrawOpaqueInteriorSubTreeSamples = 0;
                base.ApplyState();
            }

            public override void Draw(IRenderer renderer)
            {
                startQuery();
                base.Draw(renderer);
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
