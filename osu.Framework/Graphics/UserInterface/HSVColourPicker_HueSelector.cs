// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class HSVColourPicker
    {
        public abstract class HueSelector : CompositeDrawable
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
                        Child = new HueSelectorBackground
                        {
                            RelativeSizeAxes = Axes.Both
                        }
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

            private class HueSelectorBackground : Box, ITexturedShaderDrawable
            {
                public new IShader TextureShader { get; private set; }
                public new IShader RoundedTextureShader { get; private set; }

                [BackgroundDependencyLoader]
                private void load(ShaderManager shaders)
                {
                    TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "HueSelectorBackground");
                    RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "HueSelectorBackgroundRounded");
                }
            }
        }
    }
}
