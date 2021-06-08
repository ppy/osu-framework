// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Control that allows for specifying a colour using the hue-saturation-value (HSV) colour model.
    /// </summary>
    public abstract class HSVColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        /// <summary>
        /// The background of the control.
        /// </summary>
        protected Box Background { get; }

        /// <summary>
        /// Contains the elements of the colour picker.
        /// </summary>
        protected FillFlowContainer Content { get; }

        private readonly SaturationValueSelector saturationValueSelector;
        private readonly HueSelector hueSelector;

        protected HSVColourPicker()
        {
            Width = 300;
            AutoSizeAxes = Axes.Y;
            Current.Value = Colour4.White;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                Content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        saturationValueSelector = CreateSaturationValueSelector(),
                        hueSelector = CreateHueSelector()
                    }
                }
            };
        }

        /// <summary>
        /// Creates the control to be used for interactively selecting the hue of the target colour.
        /// </summary>
        protected abstract HueSelector CreateHueSelector();

        /// <summary>
        /// Creates the control to be used for interactively selecting the saturation and value of the target colour.
        /// </summary>
        protected abstract SaturationValueSelector CreateSaturationValueSelector();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            saturationValueSelector.Current.BindTo(current);
            hueSelector.Hue.BindTo(saturationValueSelector.Hue);
        }

        public abstract class SaturationValueSelector : CompositeDrawable
        {
            public readonly Bindable<Colour4> Current = new Bindable<Colour4>();

            public Bindable<float> Hue { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            public Bindable<float> Saturation { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            public Bindable<float> Value { get; } = new BindableFloat
            {
                MinValue = 0,
                MaxValue = 1
            };

            /// <summary>
            /// The gradiented box serving as the selection area.
            /// </summary>
            protected Container SelectionArea { get; }

            private readonly Box hueBox;
            private readonly Drawable marker;

            protected SaturationValueSelector()
            {
                RelativeSizeAxes = Axes.X;

                InternalChildren = new[]
                {
                    SelectionArea = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            hueBox = new Box
                            {
                                Name = "Hue",
                                RelativeSizeAxes = Axes.Both
                            },
                            new Box
                            {
                                Name = "Saturation",
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(Colour4.White, Colour4.White.Opacity(0))
                            },
                            new Box
                            {
                                Name = "Value",
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientVertical(Colour4.Black.Opacity(0), Colour4.Black)
                            },
                        }
                    },
                    marker = CreateMarker().With(d =>
                    {
                        d.Current.BindTo(Current);

                        d.Origin = Anchor.Centre;
                        d.RelativePositionAxes = Axes.Both;
                    })
                };
            }

            /// <summary>
            /// Creates the marker which will be used for selecting the final colour from the gamut.
            /// </summary>
            protected abstract Marker CreateMarker();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(_ => currentChanged(), true);

                Hue.BindValueChanged(_ => hueChanged(), true);
                Saturation.BindValueChanged(_ => saturationChanged(), true);
                Value.BindValueChanged(_ => valueChanged(), true);
            }

            // As Current and {Hue,Saturation,Value} are mutually bound together,
            // using unprotected value change callbacks can end up causing partial colour updates (e.g. only the hue changing when Current is set),
            // or circular updates (e.g. Hue.Changed -> Current.Changed -> Hue.Changed).
            // To prevent this, this flag is set on every original change on each of the four bindables,
            // and any subsequent value change callbacks are supposed to not mutate any of those bindables further if the flag is set.
            private bool changeInProgress;

            private void currentChanged()
            {
                if (changeInProgress)
                    return;

                var asHSV = Current.Value.ToHSV();

                changeInProgress = true;

                Saturation.Value = asHSV.Y;
                Value.Value = asHSV.Z;

                if (shouldUpdateHue(asHSV.X))
                    Hue.Value = asHSV.X;

                changeInProgress = false;
            }

            private bool shouldUpdateHue(float newHue)
            {
                // there are two situations in which a hue value change is possibly unwanted.
                // * if saturation is near-zero, it may not be really possible to accurately measure the hue of the colour,
                //   as hsv(x, 0, y) == hsv(z, 0, y) for any x,y,z.
                // * similarly, the hues of 0 and 1 are functionally equivalent,
                //   as hsv(0, x, y) == hsv(1, x, y) for any x,y.
                // in those cases, just keep the hue as it was, as the colour will still be roughly the same to the point of being imperceptible,
                // and doing this will prevent UX idiosyncrasies (such as the hue slider jumping to 0 for no apparent reason).
                return Precision.DefinitelyBigger(Saturation.Value, 0)
                       && !Precision.AlmostEquals(Hue.Value - newHue, 1);
            }

            private void hueChanged()
            {
                hueBox.Colour = Colour4.FromHSV(Hue.Value, 1, 1);
                updateCurrent();
            }

            private void saturationChanged()
            {
                marker.X = Saturation.Value;
                updateCurrent();
            }

            private void valueChanged()
            {
                marker.Y = 1 - Value.Value;
                updateCurrent();
            }

            private void updateCurrent()
            {
                if (changeInProgress)
                    return;

                changeInProgress = true;
                Current.Value = Colour4.FromHSV(Hue.Value, Saturation.Value, Value.Value);
                changeInProgress = false;
            }

            protected override void Update()
            {
                base.Update();

                // manually preserve aspect ratio.
                // Fill{Mode,AspectRatio} do not work here, because they require RelativeSizeAxes = Both,
                // which in turn causes BypassAutoSizeAxes to be set to Both, and so the parent ignores the child height and assumes 0.
                Height = DrawWidth;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                handleMouseInput(e.ScreenSpaceMousePosition);
                return true;
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                handleMouseInput(e.ScreenSpaceMousePosition);
            }

            private void handleMouseInput(Vector2 mousePosition)
            {
                var localSpacePosition = ToLocalSpace(mousePosition);
                Saturation.Value = localSpacePosition.X / DrawWidth;
                Value.Value = 1 - localSpacePosition.Y / DrawHeight;
            }

            protected abstract class Marker : CompositeDrawable
            {
                public IBindable<Colour4> Current { get; } = new Bindable<Colour4>();
            }
        }

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

                Hue.BindValueChanged(_ => updateNubPosition(), true);
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
