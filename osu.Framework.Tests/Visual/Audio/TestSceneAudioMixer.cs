// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using ManagedBass;
using ManagedBass.Fx;
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
    public partial class TestSceneAudioMixer : FrameworkTestScene
    {
        private readonly DragHandle dragHandle;
        private readonly AudioPlayingDrawable audioDrawable;
        private readonly ContainerWithEffect noEffectContainer;
        private readonly FillFlowContainer<ContainerWithEffect> effectContainers;

        public TestSceneAudioMixer()
        {
            AddRange(new Drawable[]
            {
                noEffectContainer = new ContainerWithEffect("no effect", Color4.Black, null)
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        effectContainers = new FillFlowContainer<ContainerWithEffect>
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(20)
                        },
                        audioDrawable = new AudioPlayingDrawable { Origin = Anchor.Centre }
                    }
                },
                dragHandle = new DragHandle
                {
                    Origin = Anchor.Centre,
                    Position = new Vector2(50)
                }
            });

            for (int i = 0; i < 50; i++)
            {
                float centre = 150 + 50 * i;

                effectContainers.Add(new ContainerWithEffect($"<{centre}Hz", Color4.Blue, new BQFParameters
                {
                    lFilter = BQFType.LowPass,
                    fCenter = centre
                })
                {
                    Size = new Vector2(100),
                });
            }
        }

        protected override void Update()
        {
            base.Update();

            Vector2 pos = dragHandle.ScreenSpaceDrawQuad.Centre;
            Container container = effectContainers.SingleOrDefault(c => c.ScreenSpaceDrawQuad.Contains(pos)) ?? noEffectContainer;

            if (audioDrawable.Parent != container)
            {
                audioDrawable.Parent!.RemoveInternal(audioDrawable, false);
                container.Add(audioDrawable);
            }
        }

        private partial class AudioPlayingDrawable : CompositeDrawable
        {
            [BackgroundDependencyLoader]
            private void load(ISampleStore samples)
            {
                DrawableSample sample;

                AddInternal(new AudioContainer
                {
                    Volume = { Value = 0.5f },
                    Child = sample = new DrawableSample(samples.Get("long.mp3"))
                });

                var channel = sample.GetChannel();
                channel.Looping = true;
                channel.Play();
            }
        }

        private partial class DragHandle : CompositeDrawable
        {
            public DragHandle()
            {
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

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e) => Position = e.MousePosition;
        }

        private partial class ContainerWithEffect : Container
        {
            protected override Container<Drawable> Content => content;

            private readonly Container content;
            private readonly Drawable background;

            public ContainerWithEffect(string name, Color4 colour, IEffectParameter? effect)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                DrawableAudioMixer mixer;
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

                if (effect != null)
                    mixer.AddEffect(effect);
            }

            protected override void Update()
            {
                base.Update();
                background.Alpha = content.Count > 0 ? 1 : 0.2f;
            }
        }
    }
}
