// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneDrawablePath : FrameworkTestScene
    {
        private const int texture_width = 20;

        private Texture gradientTexture;
        private InteractiveContainer content;

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            var image = new Image<Rgba32>(texture_width, 1);

            for (int i = 0; i < texture_width; ++i)
            {
                byte brightnessByte = (byte)((float)i / (texture_width - 1) * 255);
                image[i, 0] = new Rgba32(255, 255, 255, brightnessByte);
            }

            gradientTexture = renderer.CreateTexture(texture_width, 1, true);
            gradientTexture.SetData(new TextureUpload(image));

            Child = content = new InteractiveContainer();
        }

        [Test]
        public void TestSimplePath()
        {
            AddStep("create path", () =>
            {
                content.Child = new TexturedPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2> { Vector2.Zero, new Vector2(300, 300) },
                    Texture = gradientTexture,
                };
            });
        }

        [Test]
        public void TestMultiplePointPath()
        {
            AddStep("create path", () =>
            {
                content.Child = new TexturedPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 50),
                        new Vector2(50, 50),
                    },
                    Texture = gradientTexture,
                };
            });
        }

        [Test]
        public void TestSelfOverlappingPath()
        {
            AddStep("create path", () =>
            {
                content.Child = new OverlapTestPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 100),
                        new Vector2(20, 100),
                    }
                };
            });
        }

        [Test]
        public void TestThinStripesPath()
        {
            AddStep("create path", () =>
            {
                content.Child = new ThinStripesPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    PathRadius = 20,
                    Vertices = new List<Vector2>
                    {
                        new Vector2(50, 50),
                        new Vector2(50, 150),
                        new Vector2(150, 150),
                        new Vector2(150, 100),
                        new Vector2(20, 100),
                    }
                };
            });
        }

        [Test]
        public void TestSmoothPath()
        {
            AddStep("create path", () =>
            {
                content.Child = new SmoothPath
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    PathRadius = 10,
                    Vertices = new List<Vector2>
                    {
                        Vector2.Zero,
                        new Vector2(200)
                    },
                };
            });
        }

        [Test]
        public void TestUnsmoothPath()
        {
            AddStep("create path", () =>
            {
                content.Child = new Path
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    PathRadius = 10,
                    Vertices = new List<Vector2>
                    {
                        Vector2.Zero,
                        new Vector2(200)
                    },
                };
            });
        }

        [Test]
        public void TestPathBlending()
        {
            AddStep("create path", () =>
            {
                content.Children = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(200)
                    },
                    new TexturedPath
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        Vertices = new List<Vector2>
                        {
                            new Vector2(50, 50),
                            new Vector2(50, 150),
                            new Vector2(150, 150),
                            new Vector2(150, 100),
                            new Vector2(20, 100),
                        },
                        Texture = gradientTexture,
                    }
                };
            });
        }

        [Test]
        public void TestSizing()
        {
            Path path = null;

            AddStep("create autosize path", () =>
            {
                content.Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    Child = path = new Path
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        PathRadius = 10,
                        Vertices = new List<Vector2>
                        {
                            Vector2.Zero,
                            new Vector2(100, 0)
                        },
                    }
                };
            });

            AddAssert("size = (120, 20)", () => Precision.AlmostEquals(new Vector2(120, 20), path.DrawSize));

            AddStep("make path relative-sized", () =>
            {
                path.AutoSizeAxes = Axes.None;
                path.RelativeSizeAxes = Axes.Both;
                path.Size = Vector2.One;
            });

            AddAssert("size = (200, 200)", () => Precision.AlmostEquals(new Vector2(200), path.DrawSize));

            AddStep("make path absolute-sized", () =>
            {
                path.RelativeSizeAxes = Axes.None;
                path.Size = new Vector2(100);
            });

            AddAssert("size = (100, 100)", () => Precision.AlmostEquals(new Vector2(100), path.DrawSize));
        }

        private partial class OverlapTestPath : SmoothPath
        {
            protected override Color4 ColourAt(float position)
            {
                return Interpolation.ValueAt(position, Color4.Red, Color4.Blue, 0f, 1f);
            }
        }

        private partial class ThinStripesPath : SmoothPath
        {
            protected override Color4 ColourAt(float position)
            {
                return position % 0.1f < 0.05f ? Color4.Red : Color4.Blue;
            }
        }

        private partial class InteractiveContainer : Container
        {
            protected override Container<Drawable> Content => ScalableContent;

            protected readonly Container ScalableContent;
            private readonly bool smooth;

            public InteractiveContainer(bool smooth = true)
            {
                this.smooth = smooth;

                RelativeSizeAxes = Axes.Both;
                AddInternal(ScalableContent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Reset();
            }

            private float zoom = 1f;

            public void Reset()
            {
                ResetPosition();
            }

            public void ResetPosition()
            {
                ScalableContent.Anchor = Anchor.Centre;
                ScalableContent.Origin = Anchor.Centre;
                ScalableContent.Position = Vector2.Zero;
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                base.OnDrag(e);
                ScalableContent.Position += e.Delta;
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                base.OnScroll(e);

                ScalableContent.OriginPosition = ToSpaceOfOtherDrawable(e.MousePosition, ScalableContent);
                ScalableContent.Anchor = Anchor.TopLeft;
                ScalableContent.Position = e.MousePosition;

                zoom += (e.ScrollDelta.Y > 0 ? 1 : -1) * zoom * 0.1f;
                ScalableContent.ScaleTo(zoom, smooth ? 150 : 0, Easing.OutQuint);

                return true;
            }
        }
    }
}
