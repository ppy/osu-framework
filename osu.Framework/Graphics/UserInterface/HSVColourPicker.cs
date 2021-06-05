// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;

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

        public Colour4 BackgroundColour
        {
            get => background.Colour;
            set => background.Colour = value;
        }

        /// <summary>
        /// Contains the elements of the colour picker.
        /// </summary>
        protected readonly FillFlowContainer Content;

        private readonly Box background;
        private readonly SaturationValueSelector saturationValueSelector;
        private readonly HueSelector hueSelector;

        protected HSVColourPicker()
        {
            Width = 300;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                background = new Box
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

            saturationValueSelector.Hue.BindTo(hueSelector.Hue);

            saturationValueSelector.Hue.BindValueChanged(_ => updateCurrent());
            saturationValueSelector.Saturation.BindValueChanged(_ => updateCurrent());
            saturationValueSelector.Value.BindValueChanged(_ => updateCurrent(), true);
        }

        private void updateCurrent()
        {
            Current.Value = Colour4.FromHSV(saturationValueSelector.Hue.Value, saturationValueSelector.Saturation.Value, saturationValueSelector.Value.Value);
        }

        public abstract class SaturationValueSelector : CompositeDrawable
        {
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
                    })
                };
            }

            /// <summary>
            /// Creates the nub which will be used for the hue slider.
            /// </summary>
            protected abstract Drawable CreateSliderNub();

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
