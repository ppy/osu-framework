// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneAudioMixer : FrameworkTestScene
    {
        private ContainerWithEffect compressorContainer;
        private ContainerWithEffect limiterContainer;
        private ContainerWithEffect filterContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            FillFlowContainer<ContainerWithEffect> effectContainers;
            Child = effectContainers = new FillFlowContainer<ContainerWithEffect>
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    compressorContainer = new ContainerWithEffect("compressor", Color4.Red)
                    {
                        Effect = new CompressorParameters
                        {
                            fAttack = 5f,
                            fRelease = 100f,
                            fThreshold = -10f,
                            fGain = 0f,
                            fRatio = 8f,
                        }
                    },
                    limiterContainer = new ContainerWithEffect("limiter", Color4.Green)
                    {
                        Effect = new CompressorParameters
                        {
                            fAttack = 0.01f,
                            fRelease = 100f,
                            fThreshold = -10f,
                            fGain = 0f,
                            fRatio = 20f,
                        }
                    },
                    filterContainer = new ContainerWithEffect("filter", Color4.Blue)
                    {
                        Effect = new BQFParameters
                        {
                            lFilter = BQFType.LowPass,
                            fCenter = 150
                        }
                    }
                }
            };

            AudioBox audioBox;
            Add(audioBox = new AudioBox(this, effectContainers));

            Add(audioBox.CreateProxy());
        });

        private class AudioBox : CompositeDrawable
        {
            private readonly Container<Drawable> defaultParent;
            private readonly Container<ContainerWithEffect> effectContainers;

            public AudioBox(Container<Drawable> defaultParent, Container<ContainerWithEffect> effectContainers)
            {
                this.defaultParent = defaultParent;
                this.effectContainers = effectContainers;

                currentContainer = defaultParent;

                Origin = Anchor.Centre;
                Size = new Vector2(50);

                InternalChild = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.HotPink,
                        },
                        new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Icon = FontAwesome.Solid.VolumeUp,
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(ISampleStore samples)
            {
                samples.Volume.Value = 0.5f;

                DrawableSample sample;
                AddInternal(sample = new DrawableSample(samples.Get("long.mp3")));

                var channel = sample.GetChannel();
                channel.Looping = true;
                channel.Play();
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                Position += e.Delta;
            }

            private Container<Drawable> currentContainer;

            protected override void Update()
            {
                base.Update();

                Vector2 centre = ScreenSpaceDrawQuad.Centre;

                Container<Drawable> targetContainer = effectContainers.FirstOrDefault(c => c.Contains(centre)) ?? defaultParent;
                if (targetContainer == currentContainer)
                    return;

                currentContainer.Remove(this);
                targetContainer.Add(this);

                Position = targetContainer.ToLocalSpace(centre);

                currentContainer = targetContainer;
            }
        }

        private class ContainerWithEffect : Container
        {
            protected override Container<Drawable> Content => content;

            private readonly DrawableAudioMixer mixer;
            private readonly Container content;

            private readonly Drawable background;

            public ContainerWithEffect(string name, Color4 colour)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                RelativeSizeAxes = Axes.Both;
                Size = new Vector2(0.5f);

                InternalChild = mixer = new DrawableAudioMixer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colour,
                            Alpha = 0.2f
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = name,
                            Font = FrameworkFont.Regular.With(size: 18)
                        },
                        content = new Container
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                };
            }

            private IEffectParameter effect;

            public IEffectParameter Effect
            {
                get => effect;
                set
                {
                    Debug.Assert(effect == null);
                    effect = value;

                    mixer.AddEffect(value, 0);
                }
            }

            protected override void Update()
            {
                base.Update();

                background.Alpha = content.Count > 0 ? 1 : 0.2f;
            }
        }
    }
}
