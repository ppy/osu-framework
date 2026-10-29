// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Bindables;
using System.IO;
using System.Threading;
using mysoundlib_cs;
using osu.Framework.Audio.Callbacks;
using osu.Framework.Extensions;

namespace osu.Framework.Audio.Sample
{
    internal class SampleSDL3Factory : SampleFactory, SDL3AudioDecoderQueue.IWorker
    {
        private volatile bool isLoaded;
        public override bool IsLoaded => isLoaded;

        private readonly SDL3AudioMixer mixer;

        private IntPtr handle = IntPtr.Zero;

        private Stream? dataStream;

        public SampleSDL3Factory(Stream data, string name, SDL3AudioMixer mixer, int playbackConcurrency)
            : base(name, playbackConcurrency)
        {
            this.mixer = mixer;
            dataStream = data;

            SDL3AudioDecoderQueue.INSTANCE.Enqueue(this);
        }

        private readonly object lockHandle = new object();

        bool SDL3AudioDecoderQueue.IWorker.DoWork()
        {
            lock (lockHandle)
            {
                if (dataStream == null)
                    return false;

                using SDL3AudioFileCallbacks fileCallbacks = new SDL3AudioFileCallbacks(dataStream);
                IntPtr tempHandle = MySoundLibrary.mslCreateSampleFactory().ThrowIfNull();

                MySoundLibrary.mslRunSampleFactoryDecoder(tempHandle, fileCallbacks.Handle, fileCallbacks.ReadCallback, fileCallbacks.SeekCallback);
                Length = MySoundLibrary.mslSampleFactoryGetLength(tempHandle);
                Interlocked.Exchange(ref handle, tempHandle);
                isLoaded = true;

                dataStream.Dispose();
                dataStream = null;
            }

            return false;
        }

        public IntPtr CreatePlayer()
        {
            if (dataStream == null)
            {
                // decoding has failed or done
                return MySoundLibrary.mslCreateSample(handle).ThrowIfNull();
            }
            else
            {
                // make sure it doesn't get accessed while loading.
                lock (lockHandle)
                {
                    return MySoundLibrary.mslCreateSample(handle).ThrowIfNull();
                }
            }
        }

        public override Sample CreateSample() => new SampleSDL3(this, mixer) { OnPlay = SampleFactoryOnPlay };

        protected override void UpdatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
        }

        ~SampleSDL3Factory()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            lock (lockHandle)
            {
                MySoundLibrary.mslDestroySampleFactory(handle);
                handle = IntPtr.Zero;

                dataStream?.Dispose();
                dataStream = null;
            }

            base.Dispose(disposing);
        }
    }
}
