// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Framework.Testing
{
    public abstract class TestScene : DrawFrameRecordingContainer
    {
        protected TestScene()
        {
            Masking = true;
            RelativeSizeAxes = Axes.Both;
        }
    }
}
