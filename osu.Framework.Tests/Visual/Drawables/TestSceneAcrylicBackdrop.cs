// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("内容无关的全局毛玻璃：可拖动卡片透出下层完整图片的模糊")]
    public partial class TestSceneAcrylicBackdrop : FrameworkTestScene
    {
        private AcrylicBackdropDrawable acrylic = null!;
        private DraggableCard card = null!;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Child = new BufferedContainer(pixelSnapping: true)
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // 下层："完整图片"作为被透视的内容。
                    new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Texture = textures.Get("sample-texture"),
                    },
                    // 上层：可用鼠标拖动的小毛玻璃卡片。
                    card = new DraggableCard
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(260, 160),
                        Masking = true,
                        CornerRadius = 20,
                        BorderThickness = 2,
                        BorderColour = new Color4(1f, 1f, 1f, 0.5f),
                        Children = new Drawable[]
                        {
                            acrylic = new AcrylicBackdropDrawable
                            {
                                RelativeSizeAxes = Axes.Both,
                                BlurSigma = new Vector2(16),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = new Color4(1f, 1f, 1f, 0.12f),
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "拖动我",
                                Font = FrameworkFont.Regular.With(size: 26),
                            },
                        },
                    },
                },
            };
        }

        [Test]
        public void TestDragAndTune()
        {
            AddToggleStep("effect enabled", v => acrylic.EffectEnabled = v);
            AddSliderStep("blur sigma", 0f, 40f, 16f, v => acrylic.BlurSigma = new Vector2(v));
            AddSliderStep("fb scale", 0.1f, 1f, 0.5f, v => acrylic.FrameBufferScale = new Vector2(v));
            AddStep("center card", () => card.MoveTo(Vector2.Zero, 400, Easing.OutQuint));
        }

        /// <summary>
        /// 鼠标拖动移动自身的卡片容器。
        /// </summary>
        private partial class DraggableCard : Container
        {
            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                Position += e.Delta;
            }
        }
    }
}
