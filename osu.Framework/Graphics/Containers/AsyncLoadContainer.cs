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
        /// <param name="content">The content which should be asynchronously loaded. Note that the <see cref="Drawable.RelativeSizeAxes"/> and <see cref="Container{T}.AutoSizeAxes"/> of this container
        /// will be transferred as the default for this <see cref="AsyncLoadContainer"/>.</param>
        public AsyncLoadContainer(Container content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), $@"{nameof(AsyncLoadContainer)} required non-null {nameof(content)}.");

            this.content = content;

            RelativeSizeAxes = content.RelativeSizeAxes;
            AutoSizeAxes = content.AutoSizeAxes;
        }

        private readonly Container content;

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
            loadTask = LoadComponentAsync(content, AddInternal);
        }

        /// <summary>
        /// True if the load task for our content has been started.
        /// Will remain true even after load is completed.
        /// </summary>
        protected bool LoadTriggered => loadTask != null;
    }
}
