// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneTrackAmplitudes : FrameworkTestScene
    {
        private DrawableTrack track;

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks)
        {
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    track = new DrawableTrack(tracks.Get("sample-track.mp3"))
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            track.Looping = true;
            track.Start();
        }
    }
}
