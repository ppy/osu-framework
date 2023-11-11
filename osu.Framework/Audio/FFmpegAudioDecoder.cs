// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Graphics.Video;
using SDL2;

namespace osu.Framework.Audio
{
    internal class FFmpegAudioDecoder : AudioDecoder
    {
        public FFmpegAudioDecoder(int rate, int channels, ushort format)
            : base(rate, channels, format)
        {
        }

        public class FFmpegAudioDecoderData : AudioDecoderData
        {
            internal VideoDecoder? FFmpeg;

            public FFmpegAudioDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream, PassDataDelegate? pass, object? userData)
                : base(rate, channels, isTrack, format, stream, autoDisposeStream, pass, userData)
            {
            }

            internal override void Dispose()
            {
                FFmpeg?.Dispose();
                base.Dispose();
            }
        }

        protected override void LoadFromStreamInternal(AudioDecoderData decodeData, out byte[] decoded)
        {
            if (decodeData is not FFmpegAudioDecoderData job)
                throw new ArgumentException("Provide proper data");

            if (job.FFmpeg == null)
            {
                job.FFmpeg = new VideoDecoder(job.Stream, job.Rate, job.Channels, SDL.SDL_AUDIO_ISFLOAT(job.Format), SDL.SDL_AUDIO_BITSIZE(job.Format), SDL.SDL_AUDIO_ISSIGNED(job.Format));

                job.FFmpeg.PrepareDecoding();
                job.FFmpeg.RecreateCodecContext();

                job.Bitrate = (int)job.FFmpeg.Bitrate;
                job.Length = job.FFmpeg.Duration;
                job.ByteLength = (long)Math.Ceiling(job.FFmpeg.Duration / 1000.0d * job.Rate) * job.Channels * SDL.SDL_AUDIO_BITSIZE(job.Format); // FIXME

                job.Loading = true;
            }

            job.FFmpeg.DecodeNextAudioFrame(32, out byte[] audioData, !job.IsTrack);

            if (job.FFmpeg.State != VideoDecoder.DecoderState.Running)
                job.Loading = false;

            decoded = audioData;
        }

        public override AudioDecoderData CreateDecoderData(int rate, int channels, bool isTrack, ushort format, Stream stream, bool autoDisposeStream = true, PassDataDelegate? pass = null, object? userData = null)
            => new FFmpegAudioDecoderData(rate, channels, isTrack, format, stream, autoDisposeStream, pass, userData);
    }
}
