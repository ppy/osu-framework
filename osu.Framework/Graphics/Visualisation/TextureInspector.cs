// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    internal partial class TextureInspector : VisibilityContainer
    {
        private const float width = 600;
        private const float padding = 10;

        private readonly ChannelTabControl channelSelector;
        private readonly TexturePreview preview;
        private readonly Container previewContainer;
        private readonly InteractiveContainer interactiveContainer;
        private readonly TextureInfo textureInfo;
        private readonly PixelsOutline pixelsOutline;

        public TextureInspector()
        {
            RelativeSizeAxes = Axes.Y;
            Padding = new MarginPadding(padding);
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                Width = width,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(),
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, size: 1),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                            Margin = new MarginPadding { Bottom = 10 },
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Relative, size: 1),
                            },
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension()
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = "Channels: ",
                                        Font = FrameworkFont.Regular,
                                        Colour = FrameworkColour.Yellow
                                    },
                                    channelSelector = new ChannelTabControl
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        RelativeSizeAxes = Axes.Both
                                    }
                                }
                            }
                        }
                    },
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Checkerboard
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    TextureRelativeSizeAxes = Axes.None,
                                    TextureRectangle = new RectangleF(Vector2.Zero, new Vector2(300))
                                },
                                interactiveContainer = new InteractiveContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = previewContainer = new Container
                                    {
                                        Children = new Drawable[]
                                        {
                                            preview = new TexturePreview
                                            {
                                                RelativeSizeAxes = Axes.Both
                                            },
                                            pixelsOutline = new PixelsOutline(),
                                            new TextureBorder
                                            {
                                                RelativeSizeAxes = Axes.Both
                                            }
                                        }
                                    }
                                },
                                textureInfo = new TextureInfo
                                {
                                    Margin = new MarginPadding(10)
                                }
                            }
                        }
                    }
                }
            };
        }

        private enum Channel
        {
            All,
            R,
            G,
            B,
            A
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            channelSelector.Current.BindValueChanged(c => preview.Channel = c.NewValue, true);
        }

        public void Inspect(Texture texture)
        {
            previewContainer.Size = texture.Size;
            pixelsOutline.Size = texture.Size;
            preview.Texture = texture;
            interactiveContainer.Fit();

            textureInfo.UpdateInfo(texture);
        }

        protected override void Update()
        {
            base.Update();
            pixelsOutline.Alpha = Interpolation.ValueAt(Math.Clamp(interactiveContainer.Zoom, 10f, 15f), 0f, 1f, 10f, 15f);
        }

        protected override void PopIn() => this.ResizeWidthTo(width + padding * 2, 500, Easing.OutQuint);

        protected override void PopOut() => this.ResizeWidthTo(0, 500, Easing.OutQuint);

        private partial class PixelsOutline : Box
        {
            protected override DrawNode CreateDrawNode() => new PixelsOutlineDrawNode(this);

            private partial class PixelsOutlineDrawNode : SpriteDrawNode
            {
                public PixelsOutlineDrawNode(Sprite source)
                    : base(source)
                {
                }

                private Vector2 size;

                public override void ApplyState()
                {
                    base.ApplyState();
                    size = Source.Size;
                }

                protected override void Blit(IRenderer renderer)
                {
                    var col = DrawColourInfo.Colour;
                    col.ApplyChild(Color4.Gray);

                    float spanX = ScreenSpaceDrawQuad.Width / size.X;
                    float spanY = ScreenSpaceDrawQuad.Height / size.Y;

                    for (int i = 1; i < size.X; i++)
                        renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.TopLeft.X + spanX * i, ScreenSpaceDrawQuad.TopLeft.Y, 1, ScreenSpaceDrawQuad.Height), col);

                    for (int i = 1; i < size.Y; i++)
                        renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.TopLeft.X, ScreenSpaceDrawQuad.TopLeft.Y + spanY * i, ScreenSpaceDrawQuad.Width, 1), col);
                }
            }
        }

        private partial class TextureBorder : Box
        {
            protected override DrawNode CreateDrawNode() => new TextureBorderDrawNode(this);

            private partial class TextureBorderDrawNode : SpriteDrawNode
            {
                public TextureBorderDrawNode(Sprite source)
                    : base(source)
                {
                }

                protected override void Blit(IRenderer renderer)
                {
                    const float width = 1;
                    renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.TopLeft.X, ScreenSpaceDrawQuad.TopLeft.Y - width, ScreenSpaceDrawQuad.Width + width, width), DrawColourInfo.Colour);
                    renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.TopRight.X, ScreenSpaceDrawQuad.TopRight.Y, width, ScreenSpaceDrawQuad.Height + width), DrawColourInfo.Colour);
                    renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.BottomLeft.X - width, ScreenSpaceDrawQuad.BottomLeft.Y, ScreenSpaceDrawQuad.Width + width, width), DrawColourInfo.Colour);
                    renderer.DrawQuad(renderer.WhitePixel, new Quad(ScreenSpaceDrawQuad.TopLeft.X - width, ScreenSpaceDrawQuad.TopLeft.Y - width, width, ScreenSpaceDrawQuad.Height + width), DrawColourInfo.Colour);
                }
            }
        }

        private partial class TextureInfo : Container
        {
            private readonly SpriteText sizeInfo;
            private readonly SpriteText filteringInfo;
            private readonly SpriteText pixelFormatInfo;

            public TextureInfo()
            {
                AutoSizeAxes = Axes.Both;
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Padding = new MarginPadding(5),
                        Child = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 5),
                            Children = new Drawable[]
                            {
                                sizeInfo = new SpriteText
                                {
                                    Font = new FontUsage(size: 16),
                                    Shadow = true
                                },
                                filteringInfo = new SpriteText
                                {
                                    Font = new FontUsage(size: 16),
                                    Shadow = true
                                },
                                pixelFormatInfo = new SpriteText
                                {
                                    Font = new FontUsage(size: 16),
                                    Shadow = true
                                }
                            }
                        }
                    }
                };
            }

            public void UpdateInfo(Texture texture)
            {
                sizeInfo.Text = $"Size: {texture.Width}x{texture.Height}";
                filteringInfo.Text = $"Filtering mode: {texture.NativeTexture.FilteringMode}";
                pixelFormatInfo.Text = $"Pixel format: {texture.NativeTexture.PixelFormat}";
            }
        }

        private partial class ChannelTabControl : BasicTabControl<Channel>
        {
            public ChannelTabControl()
            {
                Items = Enum.GetValues<Channel>().ToList();
            }

            protected override TabFillFlowContainer CreateTabFlow() => new TabFillFlowContainer
            {
                Direction = FillDirection.Horizontal,
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Depth = -1,
                Masking = true
            };

            protected override TabItem<Channel> CreateTabItem(Channel value)
                => new ChannelTabItem(value);

            public partial class ChannelTabItem : TabItem<Channel>
            {
                private readonly Box highlight;

                public ChannelTabItem(Channel value)
                    : base(value)
                {
                    AutoSizeAxes = Axes.None;
                    Width = 100;
                    RelativeSizeAxes = Axes.Y;
                    Padding = new MarginPadding { Horizontal = 5 };

                    AddRange(new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = FrameworkColour.BlueGreen,
                        },
                        highlight = new Box
                        {
                            Alpha = 0,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White.Opacity(.2f),
                            Blending = BlendingParameters.Additive
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = value.ToString(),
                            Font = FrameworkFont.Regular,
                            Colour = FrameworkColour.Yellow
                        }
                    });
                }

                protected override void OnActivated() => updateState();

                protected override void OnDeactivated() => updateState();

                protected override bool OnHover(HoverEvent e)
                {
                    base.OnHover(e);
                    updateState();
                    return true;
                }

                protected override void OnHoverLost(HoverLostEvent e)
                {
                    updateState();
                    base.OnHoverLost(e);
                }

                private void updateState() => highlight.FadeTo(IsHovered ? 1 : Active.Value ? 0.5f : 0f, 200);
            }
        }

        private partial class TexturePreview : Sprite
        {
            private Channel channel;

            public Channel Channel
            {
                get => channel;
                set
                {
                    channel = value;
                    Invalidate(Invalidation.DrawNode);
                }
            }

            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "TextureChannels");
            }

            protected override DrawNode CreateDrawNode() => new TexturePreviewDrawNode(this);

            protected class TexturePreviewDrawNode : SpriteDrawNode
            {
                public new TexturePreview Source => (TexturePreview)base.Source;

                public TexturePreviewDrawNode(TexturePreview source)
                    : base(source)
                {
                }

                private Channel channel;

                public override void ApplyState()
                {
                    base.ApplyState();

                    channel = Source.channel;
                }

                private IUniformBuffer<TextureChannelParameters>? parametersBuffer;

                protected override void BindUniformResources(IShader shader, IRenderer renderer)
                {
                    base.BindUniformResources(shader, renderer);

                    parametersBuffer ??= renderer.CreateUniformBuffer<TextureChannelParameters>();
                    parametersBuffer.Data = new TextureChannelParameters
                    {
                        ChannelValue = getChannelFloatRepresentation(channel),
                    };

                    shader.BindUniformBlock("m_TextureChannelParameters", parametersBuffer);
                }

                private float getChannelFloatRepresentation(Channel c)
                {
                    switch (c)
                    {
                        default:
                        case Channel.All:
                            return 0.5f;

                        case Channel.R:
                            return 1.5f;

                        case Channel.G:
                            return 2.5f;

                        case Channel.B:
                            return 3.5f;

                        case Channel.A:
                            return 4.5f;
                    }
                }

                protected internal override bool CanDrawOpaqueInterior => false;

                protected override void Dispose(bool isDisposing)
                {
                    base.Dispose(isDisposing);
                    parametersBuffer?.Dispose();
                }

                [StructLayout(LayoutKind.Sequential, Pack = 1)]
                private record struct TextureChannelParameters
                {
                    public UniformFloat ChannelValue;
                    private UniformPadding12 pad;
                }
            }
        }

        private partial class Checkerboard : Sprite
        {
            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Texture = textures.Get("Checkerboard", WrapMode.Repeat, WrapMode.Repeat);
            }
        }

        private partial class InteractiveContainer : Container
        {
            protected override Container<Drawable> Content => ScalableContent;

            protected readonly Container ScalableContent;

            public InteractiveContainer()
            {
                RelativeSizeAxes = Axes.Both;
                AddInternal(ScalableContent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both
                });
            }

            public float Zoom { get; private set; } = 1f;

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

                Zoom = Math.Clamp(Zoom + e.ScrollDelta.Y * Zoom * 0.2f, 0.1f, 25);
                ScalableContent.ScaleTo(Zoom);
                isFit = false;

                return true;
            }

            protected void SetZoom(float newZoom, Vector2? mousePosition = null, double duration = 0)
            {
                ScalableContent.ClearTransforms();

                Zoom = newZoom;

                if (mousePosition.HasValue)
                {
                    ScalableContent.OriginPosition = ToSpaceOfOtherDrawable(mousePosition.Value, ScalableContent);
                    ScalableContent.Anchor = Anchor.TopLeft;
                    ScalableContent.Position = mousePosition.Value;
                }

                ScalableContent.ScaleTo(Zoom, duration, Easing.OutQuint);
                isFit = false;
            }

            public void Fit(double duration = 0)
            {
                ResetPosition();
                SetZoom(Math.Min(DrawSize.X / ScalableContent.Child.DrawWidth, DrawSize.Y / ScalableContent.Child.DrawHeight), null, duration);
                isFit = true;
            }

            protected override bool OnClick(ClickEvent e)
            {
                base.OnClick(e);
                return true;
            }

            private bool isFit = true;

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                base.OnDoubleClick(e);

                if (isFit)
                    SetZoom(1f, e.MousePosition, 250);
                else
                    Fit();

                return true;
            }
        }
    }
}
