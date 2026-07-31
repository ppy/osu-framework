// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    internal partial class TextureInspector : VisibilityContainer
    {
        private const float width = 600;

        private readonly BasicCheckbox rCheckbox;
        private readonly BasicCheckbox gCheckbox;
        private readonly BasicCheckbox bCheckbox;
        private readonly BasicCheckbox aCheckbox;

        private readonly TexturePreview preview;
        private readonly Checkerboard checkerboard;
        private readonly Container previewContainer;
        private readonly InteractiveContainer interactiveContainer;

        public TextureInspector()
        {
            RelativeSizeAxes = Axes.Y;
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                Width = width,
                RowDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize),
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, size: 1),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Child = interactiveContainer = new InteractiveContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = previewContainer = new Container
                                {
                                    Children = new Drawable[]
                                    {
                                        checkerboard = new Checkerboard(),
                                        preview = new TexturePreview()
                                    }
                                }
                            }
                        }
                    },
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10, 0),
                            Margin = new MarginPadding { Vertical = 10 },
                            Children = new Drawable[]
                            {
                                rCheckbox = new BasicCheckbox
                                {
                                    LabelText = "R",
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Current = { Value = true }
                                },
                                gCheckbox = new BasicCheckbox
                                {
                                    LabelText = "G",
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Current = { Value = true }
                                },
                                bCheckbox = new BasicCheckbox
                                {
                                    LabelText = "B",
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Current = { Value = true }
                                },
                                aCheckbox = new BasicCheckbox
                                {
                                    LabelText = "A",
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Current = { Value = true }
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rCheckbox.Current.BindValueChanged(_ => updatePreview());
            gCheckbox.Current.BindValueChanged(_ => updatePreview());
            bCheckbox.Current.BindValueChanged(_ => updatePreview());
            aCheckbox.Current.BindValueChanged(_ => updatePreview(), true);
        }

        public void Inspect(Texture texture)
        {
            previewContainer.Size = texture.Size;
            checkerboard.Size = texture.Size;
            preview.Texture = texture;
            preview.Size = texture.Size;
            interactiveContainer.Fit();
        }

        private void updatePreview() => preview.UpdateComponents(rCheckbox.Current.Value, gCheckbox.Current.Value, bCheckbox.Current.Value, aCheckbox.Current.Value);

        protected override void PopIn() => this.ResizeWidthTo(width, 500, Easing.OutQuint);

        protected override void PopOut() => this.ResizeWidthTo(0, 500, Easing.OutQuint);

        private partial class TexturePreview : Sprite
        {
            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "TextureChannels");
            }

            private bool r;
            private bool g;
            private bool b;
            private bool a;

            public void UpdateComponents(bool r, bool g, bool b, bool a)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;

                Invalidate(Invalidation.DrawNode);
            }

            protected override DrawNode CreateDrawNode() => new TexturePreviewDrawNode(this);

            protected class TexturePreviewDrawNode : SpriteDrawNode
            {
                public new TexturePreview Source => (TexturePreview)base.Source;

                public TexturePreviewDrawNode(TexturePreview source)
                    : base(source)
                {
                }

                private bool r;
                private bool g;
                private bool b;
                private bool a;

                public override void ApplyState()
                {
                    base.ApplyState();

                    r = Source.r;
                    g = Source.g;
                    b = Source.b;
                    a = Source.a;
                }

                private IUniformBuffer<TextureChannelParameters>? parametersBuffer;

                protected override void BindUniformResources(IShader shader, IRenderer renderer)
                {
                    base.BindUniformResources(shader, renderer);

                    parametersBuffer ??= renderer.CreateUniformBuffer<TextureChannelParameters>();
                    parametersBuffer.Data = new TextureChannelParameters
                    {
                        R = r,
                        G = g,
                        B = b,
                        A = a
                    };

                    shader.BindUniformBlock("m_TextureChannelParameters", parametersBuffer);
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
                    public UniformBool R;
                    public UniformBool G;
                    public UniformBool B;
                    public UniformBool A;
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

            private float zoom = 1f;

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
                isFit = false;

                return true;
            }

            protected void SetZoom(float newZoom, Vector2? mousePosition = null, double duration = 0)
            {
                ScalableContent.ClearTransforms();

                zoom = newZoom;

                if (mousePosition.HasValue)
                {
                    ScalableContent.OriginPosition = ToSpaceOfOtherDrawable(mousePosition.Value, ScalableContent);
                    ScalableContent.Anchor = Anchor.TopLeft;
                    ScalableContent.Position = mousePosition.Value;
                }

                ScalableContent.ScaleTo(zoom, duration, Easing.OutQuint);
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
