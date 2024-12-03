// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Logging;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which, when a child requests it, will wrap its content in a <see cref="BufferedContainer"/>.
    /// </summary>
    [Cached]
    public partial class RefCountedBackbufferProvider : Container, IBackbufferProvider
    {
        private int refCount;

        private readonly Container content = new Container { RelativeSizeAxes = Axes.Both };

        private BufferedContainer? bufferedContainer;

        protected override Container<Drawable> Content => content;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(content);
        }

        internal void Increment() => refCount++;

        internal void Decrement() => refCount--;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            Debug.Assert(refCount >= 0);

            if (refCount > 0 && bufferedContainer == null)
            {
                Logger.Log($@"{nameof(RefCountedBackbufferProvider)} became active.");

                ClearInternal(false);
                AddInternal(bufferedContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = content
                });
            }
            else if (refCount == 0 && bufferedContainer != null)
            {
                Logger.Log($@"{nameof(RefCountedBackbufferProvider)} became inactive.");

                bufferedContainer?.Clear(false);
                bufferedContainer = null;

                ClearInternal();
                AddInternal(content);
            }
        }
    }
}
