// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which asynchronously loads its children.
    /// </summary>
    public class AsyncLoadContainer : Container
    {
        /// <summary>
        /// Called when async loading of children has completed.
        /// </summary>
        public Action<AsyncLoadContainer> FinishedLoading;

        protected override Container<Drawable> Content => content;

        private readonly Container content = new Container { RelativeSizeAxes = Axes.Both };


        protected virtual bool ShouldLoadContent => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            if (ShouldLoadContent)
                loadContentAsync();
        }

        protected override void Update()
        {
            base.Update();

            if (!LoadTriggered && ShouldLoadContent)
                loadContentAsync();
        }

        private Task loadTask;

        private void loadContentAsync()
        {
            loadTask = LoadComponentAsync(content, d =>
            {
                AddInternal(d);
                FinishedLoading?.Invoke(this);
            });
        }

        /// <summary>
        /// True if the load task for our content has been started.
        /// Will remain true even after load is completed.
        /// </summary>
        protected bool LoadTriggered => loadTask != null;
    }
}