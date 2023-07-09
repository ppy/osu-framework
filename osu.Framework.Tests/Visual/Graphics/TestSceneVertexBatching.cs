// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneVertexBatching : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Test]
        public void TestBatchUntilOverflow()
        {
            AddStep("load boxes", () =>
            {
                Clear();

                int boxesPerBatch = host.Renderer.DefaultQuadBatch.Size;

                Add(new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding(25f),
                    Spacing = new Vector2(10f),
                    ChildrenEnumerable = Enumerable.Range(0, boxesPerBatch * 2).Select(i => new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                        Size = new Vector2(50f),
                    })
                });
            });
        }

        [Test]
        public void TestBatchWithFlushes()
        {
            AddStep("load boxes", () =>
            {
                Clear();

                int boxesPerBatch = host.Renderer.DefaultQuadBatch.Size;

                Add(new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding(25f),
                    Spacing = new Vector2(10f),
                    ChildrenEnumerable = Enumerable.Range(0, boxesPerBatch * 2).Select(i => new BoxWithFlush
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1),
                        Size = new Vector2(50f),
                    })
                });
            });
        }

        private partial class BoxWithFlush : Box
        {
            protected override DrawNode CreateDrawNode() => new BoxWithFlushDrawNode(this);

            private class BoxWithFlushDrawNode : SpriteDrawNode
            {
                protected new BoxWithFlush Source => (BoxWithFlush)base.Source;

                public BoxWithFlushDrawNode(BoxWithFlush source)
                    : base(source)
                {
                }

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);
                    renderer.FlushCurrentBatch(null);
                }
            }
        }
    }
}
