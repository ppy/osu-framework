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
