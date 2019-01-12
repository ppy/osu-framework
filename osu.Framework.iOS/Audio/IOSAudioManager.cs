// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Audio;
using osu.Framework.Audio.Callbacks;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Audio.Callbacks;

namespace osu.Framework.iOS.Audio
{
    public class IOSAudioManager : AudioManager
    {
        public IOSAudioManager(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore) : base(trackStore, sampleStore)
        {
        }

        protected override CallbackFactory CreateCallbackFactory() => new IOSCallbackFactory();
    }
}
