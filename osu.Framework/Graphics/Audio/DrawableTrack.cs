// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Track;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A <see cref="Track"/> wrapper to allow insertion in the draw hierarchy to allow transforms, lifetime management etc.
    /// </summary>
    public class DrawableTrack : DrawableAudioWrapper, ITrack
    {
        private readonly Track track;

        /// <summary>
        /// Construct a new drawable track instance.
        /// </summary>
        /// <param name="track">The audio track to wrap.</param>
        /// <param name="disposeTrackOnDisposal">Whether the track should be automatically disposed on drawable disposal/expiry.</param>
        public DrawableTrack(Track track, bool disposeTrackOnDisposal = true)
            : base(track, disposeTrackOnDisposal)
        {
            this.track = track;
        }

        public bool Looping
        {
            get => track.Looping;
            set => track.Looping = value;
        }

        public double RestartPoint
        {
            get => track.RestartPoint;
            set => track.RestartPoint = value;
        }

        public double CurrentTime => track.CurrentTime;

        public double Length
        {
            get => track.Length;
            set => track.Length = value;
        }

        public bool IsRunning => track.IsRunning;

        public void Reset() => track.Reset();

        public void Restart() => track.Restart();

        public void ResetSpeedAdjustments() => track.ResetSpeedAdjustments();

        public bool Seek(double seek) => track.Seek(seek);

        public void Start() => track.Start();

        public void Stop() => track.Stop();
    }
}
