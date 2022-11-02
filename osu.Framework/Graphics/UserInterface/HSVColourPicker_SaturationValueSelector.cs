// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class HSVColourPicker
    {
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

            private readonly Drawable marker;
            private readonly SaturationBox box;

            protected SaturationValueSelector()
            {
                RelativeSizeAxes = Axes.X;

                InternalChildren = new[]
                {
                    SelectionArea = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = box = new SaturationBox()
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

                // the following handlers aren't fired immediately to avoid mutating Current by accident when ran prematurely.
                // if necessary, they will run when the Current value change callback fires at the end of this method.
                Hue.BindValueChanged(_ => debounce(hueChanged));
                Saturation.BindValueChanged(_ => debounce(saturationChanged));
                Value.BindValueChanged(_ => debounce(valueChanged));

                // Current takes precedence over HSV controls, and as such it must run last after HSV handlers have been set up for correct operation.
                Current.BindValueChanged(_ => currentChanged(), true);
            }

            // As Current and {Hue,Saturation,Value} are mutually bound together,
            // using unprotected value change callbacks can end up causing partial colour updates (e.g. only the hue changing when Current is set),
            // or circular updates (e.g. Hue.Changed -> Current.Changed -> Hue.Changed).
            // To prevent this, this flag is set on every original change on each of the four bindables,
            // and any subsequent value change callbacks are supposed to not mutate any of those bindables further if the flag is set.
            private bool changeInProgress;

            private void debounce(Action updateFunc)
            {
                if (changeInProgress)
                {
                    // if changeInProgress is set, it means that this call is triggered by Current changing.
                    // the update cannot be scheduled, because due to floating-point / HSV-to-RGB conversion foibles it could potentially slightly change Current again in the next frame.
                    // running immediately is fine, however, as updateCurrent() guards against that by checking changeInProgress itself.
                    updateFunc.Invoke();
                }
                else
                {
                    // if changeInProgress is not set, it means that this call is triggered by actual user input on the hue/saturation/value controls.
                    // as such it can be debounced to reduce the amount of performed work.
                    Scheduler.AddOnce(updateFunc);
                }
            }

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
                box.Hue = Hue.Value;
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

            private class SaturationBox : Box, ITexturedShaderDrawable
            {
                public new IShader TextureShader { get; private set; }
                public new IShader RoundedTextureShader { get; private set; }

                private float hue;

                public float Hue
                {
                    get => hue;
                    set
                    {
                        if (hue == value) return;

                        hue = value;
                        Invalidate(Invalidation.DrawNode);
                    }
                }

                public SaturationBox()
                {
                    RelativeSizeAxes = Axes.Both;
                }

                [BackgroundDependencyLoader]
                private void load(ShaderManager shaders)
                {
                    TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "SaturationSelectorBackground");
                    RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "SaturationSelectorBackgroundRounded");
                }

                protected override DrawNode CreateDrawNode() => new SaturationBoxDrawNode(this);

                private class SaturationBoxDrawNode : SpriteDrawNode
                {
                    public new SaturationBox Source => (SaturationBox)base.Source;

                    public SaturationBoxDrawNode(SaturationBox source)
                        : base(source)
                    {
                    }

                    private float hue;

                    public override void ApplyState()
                    {
                        base.ApplyState();
                        hue = Source.hue;
                    }

                    protected override void Blit(IRenderer renderer)
                    {
                        GetAppropriateShader(renderer).GetUniform<float>("hue").UpdateValue(ref hue);
                        base.Blit(renderer);
                    }
                }
            }
        }
    }
}
