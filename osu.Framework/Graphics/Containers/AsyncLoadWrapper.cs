// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which asynchronously loads specified content.
    /// </summary>
    public class AsyncLoadWrapper : Container
    {
        /// <param name="content">The content which should be asynchronously loaded. Note that the <see cref="Drawable.RelativeSizeAxes"/> and <see cref="Container{T}.AutoSizeAxes"/> of this container
        /// will be transferred as the default for this <see cref="AsyncLoadWrapper"/>.</param>
        public AsyncLoadWrapper(Drawable content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), $@"{nameof(AsyncLoadWrapper)} required non-null {nameof(content)}.");

            this.content = content;

            RelativeSizeAxes = content.RelativeSizeAxes;
            AutoSizeAxes = (content as IContainer)?.AutoSizeAxes ?? AutoSizeAxes;
        }

        protected sealed override Container<Drawable> Content => base.Content;

        public override void Add(Drawable drawable)
        {
            throw new InvalidOperationException($@"{nameof(AsyncLoadWrapper)} doesn't support manually adding children. Please specify loadable conetnt in the constructor.");
        }

        private readonly Drawable content;

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
