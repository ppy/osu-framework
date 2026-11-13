// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Audio.Mixing.SDL3;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleSDL3 : Sample
    {
        public override bool IsLoaded => factory.IsLoaded;

        public override double Length => factory.Length;

        private readonly SampleSDL3Factory factory;
        private readonly SDL3AudioMixer mixer;

        public SampleSDL3(SampleSDL3Factory factory, SDL3AudioMixer mixer)
            : base(factory)
        {
            this.factory = factory;
            this.mixer = mixer;
        }

        protected override SampleChannel CreateChannel()
        {
            var channel = new SampleChannelSDL3(this, mixer.Add);
            return channel;
        }

        internal IntPtr CreatePlayer()
        {
            return IsLoaded ? factory.CreatePlayer() : IntPtr.Zero;
        }

        protected override void ItemRemoved(SampleChannel item)
        {
            mixer.RemoveChannel((ISDL3AudioChannel)item);
            base.ItemRemoved(item);
        }
    }
}
