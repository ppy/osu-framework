// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Visualisation.Audio
{
    public class MixerDisplay : CompositeDrawable
    {
        public readonly AudioMixer Mixer;

        private readonly FillFlowContainer<Drawable> mixerChannelsContainer;

        public MixerDisplay(AudioMixer mixer)
        {
            Mixer = mixer;

            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            Container outputChannelContainer;

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.2f)
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Text = mixer.Identifier,
                        Font = FrameworkFont.Condensed.With(size: 14),
                        Colour = FrameworkColour.Yellow,
                        Padding = new MarginPadding
                        {
                            Horizontal = 10,
                            Vertical = 10,
                            Left = 20
                        },
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Direction = FillDirection.Horizontal,
                        Margin = new MarginPadding
                        {
                            Horizontal = 10,
                        },
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            outputChannelContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Padding = new MarginPadding
                                {
                                    Right = 10,
                                    Vertical = 20
                                }
                            },
                            mixerChannelsContainer = new FillFlowContainer<Drawable>
                            {
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5),
                                Padding = new MarginPadding
                                {
                                    Horizontal = 10,
                                    Bottom = 20
                                }
                            }
                        }
                    }
                }
            };

            if (Mixer is AudioMixer audioMixer)
                outputChannelContainer.Add(new AudioChannelDisplay(audioMixer));
        }

        protected override void Update()
        {
            base.Update();

            if (!(Mixer is BassAudioMixer bassMixer))
                return;

            IBassAudioChannel[] channels = bassMixer.ActiveChannels.ToArray();

            if (channels.Length == 0)
            {
                mixerChannelsContainer.Clear();

                return;
            }

            foreach (IBassAudioChannel channel in channels)
            {
                if (mixerChannelsContainer.All(ch =>
                {
                    if (ch is AudioChannelDisplay audioDisplay)
                        return ((IBassAudioChannel)audioDisplay.Channel).Handle != channel.Handle;

                    if (ch is MixerDisplay mixerDisplay)
                        return ((IBassAudioChannel)mixerDisplay.Mixer).Handle != channel.Handle;

                    return true;
                }))
                {
                    if (channel is BassAudioMixer mixer)
                        mixerChannelsContainer.Add(new MixerDisplay(mixer));
                    else
                        mixerChannelsContainer.Add(new AudioChannelDisplay(channel));
                }
            }

            mixerChannelsContainer.RemoveAll(ch =>
            {
                if (ch is AudioChannelDisplay audioDisplay)
                    return channels.All(channel => ((IBassAudioChannel)audioDisplay.Channel).Handle != channel.Handle);

                if (ch is MixerDisplay mixerDisplay)
                    return channels.All(channel => ((IBassAudioChannel)mixerDisplay.Mixer).Handle != channel.Handle);

                return true;
            });
        }
    }
}
