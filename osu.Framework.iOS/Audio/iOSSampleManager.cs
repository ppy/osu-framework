// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using osu.Framework.Audio.Sample;
using osu.Framework.IO.Stores;

namespace osu.Framework.iOS.Audio
{
    public class iOSSampleManager : SampleManager
    {
        public iOSSampleManager(IResourceStore<byte[]> store) : base(store)
        {
        }

        protected override SampleBass CreateSampleBass(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency) => new iOSSampleBass(data, customPendingActions, concurrency);
    }
}
