// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Screens
{
    public class Screen : CompositeDrawable, IScreen
    {
        public bool ValidForResume { get; set; } = true;

        public bool ValidForPush { get; set; } = true;

        public override bool RemoveWhenNotAlive => !ValidForPush;

        [Resolved]
        protected Game Game { get; private set; }

        public Screen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        public virtual void OnEntering(IScreen last)
        {
        }

        public virtual bool OnExiting(IScreen next) => false;

        public virtual void OnResuming(IScreen last)
        {
        }

        public virtual void OnSuspending(IScreen next)
        {
        }
    }
}
