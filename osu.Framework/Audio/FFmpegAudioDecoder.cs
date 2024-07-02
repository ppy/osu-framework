// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Graphics.Video;
using SDL;
using static osu.Framework.Audio.AudioDecoderManager;

namespace osu.Framework.Audio
{
    internal class FFmpegAudioDecoder : AudioDecoder
    {
        private VideoDecoder? ffmpeg;
        private byte[]? decodeData;

        public FFmpegAudioDecoder(int rate, int channels, bool isTrack, SDL_AudioFormat format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass)
            : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass)
        {
        }

        internal override void Free()
        {
            decodeData = null;

            ffmpeg?.Dispose();
            base.Free();
        }

        protected override int LoadFromStreamInternal(out byte[] decoded)
        {
            if (ffmpeg == null)
            {
                ffmpeg = new VideoDecoder(Stream, Rate, Channels, SDL3.SDL_AUDIO_ISFLOAT(Format), SDL3.SDL_AUDIO_BITSIZE(Format), SDL3.SDL_AUDIO_ISSIGNED(Format));

                ffmpeg.PrepareDecoding();
                ffmpeg.RecreateCodecContext();

                Bitrate = (int)ffmpeg.Bitrate;
                Length = ffmpeg.Duration;
                ByteLength = (long)Math.Ceiling(ffmpeg.Duration / 1000.0d * Rate) * Channels * (SDL3.SDL_AUDIO_BITSIZE(Format) / 8); // FIXME

                Loading = true;
            }

            int got = ffmpeg.DecodeNextAudioFrame(32, ref decodeData, !IsTrack);

            if (ffmpeg.State != VideoDecoder.DecoderState.Running)
                Loading = false;

            decoded = decodeData;
            return got;
        }
    }
}
