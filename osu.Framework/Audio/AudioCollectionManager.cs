// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A collection of audio components which need central property control.
    /// </summary>
    public class AudioCollectionManager<T> : AdjustableAudioComponent, IBassAudio
        where T : AudioComponent
    {
        internal List<T> Items = new List<T>();

        public void AddItem(T item)
        {
            EnqueueAction(delegate
            {
                if (Items.Contains(item)) return;

                if (item is IAdjustableAudioComponent adjustable)
                    adjustable.BindAdjustments(this);

                Items.Add(item);
                ItemAdded(item);
            });
        }

        void IBassAudio.UpdateDevice(int deviceIndex) => UpdateDevice(deviceIndex);

        internal virtual void UpdateDevice(int deviceIndex)
        {
            foreach (var item in Items.OfType<IBassAudio>())
                item.UpdateDevice(deviceIndex);
        }

        protected override void UpdateChildren()
        {
            base.UpdateChildren();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                if (!item.IsAlive)
                {
                    Items.RemoveAt(i--);
                    ItemRemoved(item);
                    continue;
                }

                item.Update();
            }
        }

        protected virtual void ItemAdded(T item)
        {
        }

        protected virtual void ItemRemoved(T item)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // make the items queue their disposal, so they get disposed when UpdateChildren updates them.
            foreach (var i in Items)
                i.Dispose();

            base.Dispose(disposing);
        }
    }
}
