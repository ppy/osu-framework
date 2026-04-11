// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio
{
    public static class AudioOutputDefaults
    {
        public const int DEFAULT_SAMPLE_RATE = 48000;
        public const int SECONDARY_SAMPLE_RATE = 44100;
        public const int DEFAULT_ASIO_BUFFER_SIZE = 128;

        public const float WASAPI_EXCLUSIVE_BUFFER_SECONDS = 0.05f;
        public const float WASAPI_EXCLUSIVE_PERIOD_SECONDS = 0.01f;
    }
}
