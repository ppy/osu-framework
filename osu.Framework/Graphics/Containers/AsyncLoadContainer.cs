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

        private Game game;

        protected virtual bool ShouldLoadContent => true;

        [BackgroundDependencyLoader]
        private void load(Game game)
        {
            this.game = game;
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
            loadTask = content.LoadAsync(game, d =>
            {
                AddInternal(d);
                d.FadeInFromZero(150);
                FinishedLoading?.Invoke(this);
            });
        }

        protected bool LoadTriggered => loadTask != null;
    }
}