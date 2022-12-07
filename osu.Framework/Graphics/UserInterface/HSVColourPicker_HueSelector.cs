// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class HSVColourPicker
    {
        public abstract partial class HueSelector : CompositeDrawable
        {
            public Bindable<float> Hue { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            /// <summary>
            /// The body of the hue slider.
            /// </summary>
            protected readonly Container SliderBar;

            private readonly Drawable nub;

            protected HueSelector()
            {
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;

                InternalChildren = new[]
                {
                    SliderBar = new Container
                    {
                        Height = 30,
                        RelativeSizeAxes = Axes.X,
                        Child = new HueSelectorBackground()
                    },
                    nub = CreateSliderNub().With(d =>
                    {
                        d.RelativeSizeAxes = Axes.Y;
                        d.RelativePositionAxes = Axes.X;
                    })
                };
            }

            /// <summary>
            /// Creates the nub which will be used for the hue slider.
            /// </summary>
            protected abstract Drawable CreateSliderNub();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Hue.BindValueChanged(_ => Scheduler.AddOnce(updateNubPosition), true);
            }

            private void updateNubPosition()
            {
                nub.Position = new Vector2(Hue.Value, 0);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                handleMouseInput(e.ScreenSpaceMousePosition);
                return true;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                handleMouseInput(e.ScreenSpaceMousePosition);
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                handleMouseInput(e.ScreenSpaceMousePosition);
            }

            private void handleMouseInput(Vector2 mousePosition)
            {
                var localSpacePosition = ToLocalSpace(mousePosition);
                Hue.Value = localSpacePosition.X / DrawWidth;
            }

            private partial class HueSelectorBackground : Drawable
            {
                public HueSelectorBackground()
                {
                    RelativeSizeAxes = Axes.Both;
                }

                private IShader shader;

                [BackgroundDependencyLoader]
                private void load(ShaderManager shaders)
                {
                    shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "HueSelectorBackground");
                }

                protected override DrawNode CreateDrawNode() => new HueSelectorBackgroundDrawNode(this);

                private class HueSelectorBackgroundDrawNode : DrawNode
                {
                    public new HueSelectorBackground Source => (HueSelectorBackground)base.Source;

                    public HueSelectorBackgroundDrawNode(HueSelectorBackground source)
                        : base(source)
                    {
                    }

                    private IShader shader;
                    private Vector2 drawSize;

                    public override void ApplyState()
                    {
                        base.ApplyState();

                        shader = Source.shader;
                        drawSize = Source.DrawSize;
                    }

                    public override void Draw(IRenderer renderer)
                    {
                        base.Draw(renderer);

                        shader.Bind();

                        var quad = new Quad(
                            Vector2Extensions.Transform(Vector2.Zero, DrawInfo.Matrix),
                            Vector2Extensions.Transform(new Vector2(drawSize.X, 0f), DrawInfo.Matrix),
                            Vector2Extensions.Transform(new Vector2(0f, drawSize.Y), DrawInfo.Matrix),
                            Vector2Extensions.Transform(drawSize, DrawInfo.Matrix)
                        );

                        renderer.DrawQuad(quad, DrawColourInfo.Colour);

                        shader.Unbind();
                    }
                }
            }
        }
    }
}
