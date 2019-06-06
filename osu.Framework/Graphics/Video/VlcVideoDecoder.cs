// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LibVLCSharp.Shared;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Video
{
    // ReSharper disable RedundantAssignment
    public unsafe class VlcVideoDecoder : VideoDecoder
    {
        private static readonly MethodInfo set_callback_method;

        private static void libVlcVideoSetCallbacks(IntPtr mediaPlayer,
                                                    MediaPlayer.LibVLCVideoLockCb lockCallback,
                                                    MediaPlayer.LibVLCVideoUnlockCb unlockCallback,
                                                    MediaPlayer.LibVLCVideoDisplayCb displayCallback,
                                                    IntPtr opaque)
        {
            set_callback_method.Invoke(null, new object[] { mediaPlayer, lockCallback, unlockCallback, displayCallback, opaque });
        }

        static VlcVideoDecoder()
        {
            var mediaPlayerType = typeof(MediaPlayer);
            var assembly = mediaPlayerType.Assembly;
            var nativeType = assembly.DefinedTypes.First(x => x.FullName == "LibVLCSharp.Shared.MediaPlayer+Native");
            set_callback_method = nativeType.DeclaredMethods.First(x => x.Name == "LibVLCVideoSetCallbacks");
        }

        public override double Duration => media.Duration;

        private float lastDecodedFrameTime;
        public override float LastDecodedFrameTime => lastDecodedFrameTime;

        public override double FrameRate => player.Fps;

        /// <summary>
        /// The current decoding state.
        /// </summary>
        public override DecoderState State
        {
            get => state;
            protected set => state = value;
        }

        private volatile DecoderState state;

        private LibVLC libVlc;
        private Media media;
        private MediaPlayer player;

        public int Width { get; private set; }
        public int Height { get; private set; }

        private ObjectHandle<VlcVideoDecoder> objectHandle;

        private IntPtr buffer = IntPtr.Zero;

        public override void Seek(double targetTimestamp) => player.Position = (float)(targetTimestamp / media.Duration);

        private MediaPlayer.LibVLCVideoLockCb videoLockDelegate;
        private MediaPlayer.LibVLCVideoUnlockCb videoUnlockDelegate;
        private MediaPlayer.LibVLCVideoDisplayCb videoDisplayDelegate;
        private MediaPlayer.LibVLCVideoFormatCb videoFormatDelegate;
        private MediaPlayer.LibVLCVideoCleanupCb videoCleanupDelegate;

        public VlcVideoDecoder(Stream videoStream)
            : base(videoStream)
        {
            Core.Initialize(Assembly.GetExecutingAssembly().Location);
            libVlc = new LibVLC();
            media = new Media(libVlc, videoStream);
            player = new MediaPlayer(media);
            player.EndReached += playerEndReached;

            objectHandle = new ObjectHandle<VlcVideoDecoder>(this, GCHandleType.Normal);

            videoLockDelegate = videoLockCallback;
            videoUnlockDelegate = videoUnlockCallback;
            videoDisplayDelegate = videoDisplayCallback;
            videoFormatDelegate = videoFormatCallback;
            videoCleanupDelegate = videoCleanupCallback;

            player.SetVideoFormatCallbacks(videoFormatDelegate, videoCleanupDelegate);
            libVlcVideoSetCallbacks(player.NativeReference, videoLockDelegate, videoUnlockDelegate, videoDisplayDelegate, objectHandle.Handle);
        }

        private void playerEndReached(object sender, EventArgs e)
        {
            if (Looping)
                player.Position = 0;
            else
                State = DecoderState.EndOfStream;
        }

        public override void StartDecoding()
        {
            player.Play();
            State = DecoderState.Running;
        }

        public override void StopDecoding(bool waitForDecoderExit)
        {
            player.Stop();
            State = DecoderState.Ready;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(disposing);

            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;

            State = DecoderState.Stopped;
            if (player?.IsPlaying ?? false)
                player?.Stop();
            player?.Dispose();
            player = null;

            media?.Dispose();
            media = null;

            libVlc?.Dispose();
            libVlc = null;

            objectHandle.Dispose();

            videoCleanupDelegate = null;
            videoDisplayDelegate = null;
            videoFormatDelegate = null;
            videoLockDelegate = null;
            videoUnlockDelegate = null;
        }

        [MonoPInvokeCallback(typeof(MediaPlayer.LibVLCVideoLockCb))]
        private static IntPtr videoLockCallback(IntPtr opaque, IntPtr planes)
        {
            var handle = new ObjectHandle<VlcVideoDecoder>(opaque);
            handle.GetTarget(out VlcVideoDecoder decoder);

            Marshal.WriteIntPtr(planes, decoder.buffer);
            return opaque;
        }

        [MonoPInvokeCallback(typeof(MediaPlayer.LibVLCVideoUnlockCb))]
        private static void videoUnlockCallback(IntPtr opaque, IntPtr picture, IntPtr planes)
        {
        }

        [MonoPInvokeCallback(typeof(MediaPlayer.LibVLCVideoDisplayCb))]
        private static void videoDisplayCallback(IntPtr opaque, IntPtr picture)
        {
            var handle = new ObjectHandle<VlcVideoDecoder>(opaque);
            handle.GetTarget(out VlcVideoDecoder decoder);

            const int max_pending_frames = 3;

            if (decoder.DecodedFrames.Count >= max_pending_frames)
                return;

            if (!decoder.AvailableTextures.TryDequeue(out var tex))
                tex = new Texture(decoder.Width, decoder.Height, true);

            var upload = new ArrayPoolTextureUpload(tex.Width, tex.Height);
            new Span<Rgba32>(decoder.buffer.ToPointer(), decoder.Width * decoder.Height).CopyTo(upload.RawData);
            tex.SetData(upload);

            float time = decoder.player.Position * decoder.media.Duration;

            decoder.DecodedFrames.Enqueue(new DecodedFrame { Time = time, Texture = tex });

            decoder.lastDecodedFrameTime = time;
        }

        private static uint alignDimension(uint dimension, uint mod)
        {
            var remainder = dimension % mod;

            if (remainder == 0)
                return dimension;

            return dimension + mod - remainder;
        }

        [MonoPInvokeCallback(typeof(MediaPlayer.LibVLCVideoFormatCb))]
        private static uint videoFormatCallback(ref IntPtr userData, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
        {
            var handle = new ObjectHandle<VlcVideoDecoder>(userData);
            handle.GetTarget(out VlcVideoDecoder decoder);

            writeFourCcString("RGBA", chroma);

            var mediaTrack = decoder.media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
            var videoTrack = mediaTrack.Data.Video;
            width = videoTrack.Width;
            height = videoTrack.Height;

            if (videoTrack.SarDen != 0)
                width = width * videoTrack.SarNum / videoTrack.SarDen;

            decoder.Width = (int)width;
            decoder.Height = (int)height;

            const int bpp = 32;
            pitches = alignDimension(width * bpp / 8, 32);
            lines = alignDimension(height, 32);

            var size = pitches * lines;

            decoder.buffer = Marshal.AllocHGlobal((int)size);

            return 1;
        }

        [MonoPInvokeCallback(typeof(MediaPlayer.LibVLCVideoCleanupCb))]
        private static void videoCleanupCallback(ref IntPtr opaque)
        {
            var handle = new ObjectHandle<VlcVideoDecoder>(opaque);
            handle.GetTarget(out VlcVideoDecoder decoder);

            if (decoder.buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(decoder.buffer);

            decoder.buffer = IntPtr.Zero;
        }

        private static void writeFourCcString(string fourCcString, IntPtr destination)
        {
            if (fourCcString.Length != 4)
            {
                throw new ArgumentException("4CC codes must be 4 characters long", nameof(fourCcString));
            }

            var bytes = Encoding.ASCII.GetBytes(fourCcString);

            for (var i = 0; i < 4; i++)
            {
                Marshal.WriteByte(destination, i, bytes[i]);
            }
        }
    }
}
