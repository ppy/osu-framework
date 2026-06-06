// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("内容无关的全局毛玻璃：透出下层任意已渲染内容")]
    public partial class TestSceneAcrylicBackdrop : FrameworkTestScene
    {
        private AcrylicBackdropDrawable acrylic;
        private Container card;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Container movingContent;

            // 承载缓冲：全屏、原点对齐、scale=1 —— 满足 AcrylicBackdropDrawable 的几何前提。
            Child = new BufferedContainer(pixelSnapping: true)
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    movingContent = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    // 卡片（毛玻璃 + 边框 + 文本）放在最上层，绘制时其下层内容已写入承载缓冲。
                    card = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(360, 220),
                        Masking = true,
                        CornerRadius = 24,
                        BorderThickness = 2,
                        BorderColour = new Color4(1f, 1f, 1f, 0.4f),
                        Children = new Drawable[]
                        {
                            acrylic = new AcrylicBackdropDrawable
                            {
                                RelativeSizeAxes = Axes.Both,
                                BlurSigma = new Vector2(16),
                            },
                            // 轻微白色叠加，强化"毛玻璃"质感。
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = new Color4(1f, 1f, 1f, 0.12f),
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Acrylic Card",
                                Font = FrameworkFont.Regular.With(size: 28),
                            },
                        },
                    },
                },
            };

            // 移动的彩色方块作为"下层任意内容"，验证逐帧一致、无需知道下层是什么。
            for (int i = 0; i < 6; i++)
            {
                var box = new Box
                {
                    Size = new Vector2(180),
                    Colour = colourFor(i),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };

                movingContent.Add(box);

                float radius = 220;
                box.Spin(4000 + i * 600, i % 2 == 0 ? RotationDirection.Clockwise : RotationDirection.Counterclockwise);
                box.Loop(b => b
                    .MoveTo(new Vector2(MathF.Cos(i) * radius, MathF.Sin(i) * radius), 0)
                    .MoveTo(new Vector2(MathF.Cos(i + 3) * radius, MathF.Sin(i + 3) * radius), 3000, Easing.InOutSine)
                    .Then()
                    .MoveTo(new Vector2(MathF.Cos(i) * radius, MathF.Sin(i) * radius), 3000, Easing.InOutSine));
            }
        });

        [Test]
        public void TestToggleAndTune()
        {
            AddToggleStep("effect enabled", v => acrylic.EffectEnabled = v);
            AddSliderStep("blur sigma", 0f, 40f, 16f, v => acrylic.BlurSigma = new Vector2(v));
            AddSliderStep("fb scale", 0.1f, 1f, 0.5f, v => acrylic.FrameBufferScale = new Vector2(v));
            AddStep("move card", () => card.MoveTo(new Vector2(150, 0), 800, Easing.OutQuint));
            AddStep("center card", () => card.MoveTo(Vector2.Zero, 800, Easing.OutQuint));
        }

        private static Color4 colourFor(int i)
        {
            switch (i % 6)
            {
                case 0: return Color4.Tomato;
                case 1: return Color4.SkyBlue;
                case 2: return Color4.YellowGreen;
                case 3: return Color4.Orange;
                case 4: return Color4.MediumPurple;
                default: return Color4.HotPink;
            }
        }
    }
}
